using ScreenGrab;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfScreenHelper;

namespace STranslate.Core;

public class Screenshot : IScreenshot
{
    public Bitmap? GetScreenshot()
    {
        if (ScreenGrabber.IsCapturing)
            return default;
        var bitmap = ScreenGrabber.CaptureDialog(isAuxiliary: true);
        if (bitmap == null)
            return default;
        return bitmap;
    }

    public async Task<Bitmap?> GetScreenshotAsync()
    {
        if (ScreenGrabber.IsCapturing)
            return default;
        var bitmap = await ScreenGrabber.CaptureAsync(isAuxiliary: true);
        if (bitmap == null)
            return default;
        return bitmap;
    }

    /// <summary>
    /// 最后一次截图的屏幕区域（静态存储）
    /// </summary>
    private static Rectangle _lastCaptureRect;

    public async Task<ScreenshotResult> GetScreenshotWithBoundsAsync()
    {
        var result = new ScreenshotResult();
        
        // 使用自定义的截图窗口获取截图和坐标
        var captureResult = await CaptureWithBoundsAsync();
        
        result.Bitmap = captureResult.bitmap;
        if (captureResult.bounds.Width > 0 && captureResult.bounds.Height > 0)
        {
            result.ScreenBounds = new System.Windows.Rect(
                captureResult.bounds.X, 
                captureResult.bounds.Y, 
                captureResult.bounds.Width, 
                captureResult.bounds.Height);
        }

        return result;
    }

    /// <summary>
    /// 使用自定义截图窗口获取截图和坐标
    /// </summary>
    private static Task<(Bitmap? bitmap, Rectangle bounds)> CaptureWithBoundsAsync()
    {
        var tcs = new TaskCompletionSource<(Bitmap?, Rectangle)>();

        Application.Current.Dispatcher.Invoke(() =>
        {
            var allScreens = Screen.AllScreens.ToList();
            var captureWindows = new List<ScreenCaptureOverlay>();

            foreach (var screen in allScreens)
            {
                var window = new ScreenCaptureOverlay();
                window.OnCaptured = (bitmap, bounds) =>
                {
                    // 关闭所有截图窗口
                    foreach (var w in captureWindows)
                    {
                        w.Close();
                    }
                    tcs.TrySetResult((bitmap, bounds));
                };
                window.OnCancelled = () =>
                {
                    foreach (var w in captureWindows)
                    {
                        w.Close();
                    }
                    tcs.TrySetResult((null, Rectangle.Empty));
                };

                // 设置窗口位置和大小（使用 WPF 逻辑坐标）
                var wpfBounds = screen.WpfBounds;
                window.Left = wpfBounds.Left;
                window.Top = wpfBounds.Top;
                window.Width = wpfBounds.Width;
                window.Height = wpfBounds.Height;

                captureWindows.Add(window);
                window.Show();
                window.Activate();
            }
        });

        return tcs.Task;
    }
}

/// <summary>
/// 简化版截图覆盖窗口 - 获取截图和屏幕坐标
/// </summary>
internal class ScreenCaptureOverlay : Window
{
    // Windows API 获取窗口实际像素位置
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public Action<Bitmap, Rectangle>? OnCaptured { get; set; }
    public Action? OnCancelled { get; set; }

    private System.Windows.Point _startPoint;
    private bool _isSelecting;
    private readonly Border _selectionBorder;
    private readonly Canvas _canvas;
    private DpiScale? _dpiScale;
    private IntPtr _hwnd;

    public ScreenCaptureOverlay()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(1, 0, 0, 0)); // 几乎透明
        Topmost = true;
        ShowInTaskbar = false;
        Cursor = Cursors.Cross;
        WindowState = WindowState.Normal;

        _canvas = new Canvas { Background = System.Windows.Media.Brushes.Transparent };
        Content = _canvas;

        _selectionBorder = new Border
        {
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 0, 120, 215))
        };

        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
        KeyDown += OnKeyDown;

        Loaded += (s, e) =>
        {
            _dpiScale = VisualTreeHelper.GetDpi(this);
            _hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            WindowState = WindowState.Maximized;
        };
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            OnCancelled?.Invoke();
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.RightButton == MouseButtonState.Pressed)
        {
            OnCancelled?.Invoke();
            return;
        }

        _isSelecting = true;
        _startPoint = e.GetPosition(this);
        _canvas.Children.Add(_selectionBorder);
        Canvas.SetLeft(_selectionBorder, _startPoint.X);
        Canvas.SetTop(_selectionBorder, _startPoint.Y);
        _selectionBorder.Width = 0;
        _selectionBorder.Height = 0;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting) return;

        var currentPoint = e.GetPosition(this);
        var left = Math.Min(_startPoint.X, currentPoint.X);
        var top = Math.Min(_startPoint.Y, currentPoint.Y);
        var width = Math.Abs(currentPoint.X - _startPoint.X);
        var height = Math.Abs(currentPoint.Y - _startPoint.Y);

        Canvas.SetLeft(_selectionBorder, left);
        Canvas.SetTop(_selectionBorder, top);
        _selectionBorder.Width = width;
        _selectionBorder.Height = height;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting) return;

        _isSelecting = false;
        ReleaseMouseCapture();

        // 获取选区位置和尺寸（边框的外边界就是用户选择的区域）
        var selLeft = Canvas.GetLeft(_selectionBorder);
        var selTop = Canvas.GetTop(_selectionBorder);
        var selWidth = _selectionBorder.Width;
        var selHeight = _selectionBorder.Height;

        if (selWidth < 5 || selHeight < 5)
        {
            OnCancelled?.Invoke();
            return;
        }

        // 使用 Windows API 获取窗口实际像素位置（最精确）
        RECT windowRect;
        if (!GetWindowRect(_hwnd, out windowRect))
        {
            OnCancelled?.Invoke();
            return;
        }

        // DPI 缩放因子
        var dpiScaleX = _dpiScale?.DpiScaleX ?? 1.0;
        var dpiScaleY = _dpiScale?.DpiScaleY ?? 1.0;

        // 计算选区在屏幕上的像素坐标
        // 窗口像素位置 + 选区逻辑坐标 * DPI缩放
        var screenX = windowRect.Left + (int)Math.Round(selLeft * dpiScaleX);
        var screenY = windowRect.Top + (int)Math.Round(selTop * dpiScaleY);
        var screenWidth = (int)Math.Round(selWidth * dpiScaleX);
        var screenHeight = (int)Math.Round(selHeight * dpiScaleY);

        // 确保尺寸有效
        screenWidth = Math.Max(1, screenWidth);
        screenHeight = Math.Max(1, screenHeight);

        var bounds = new Rectangle(screenX, screenY, screenWidth, screenHeight);

        // 截图
        try
        {
            var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);
            }
            OnCaptured?.Invoke(bitmap, bounds);
        }
        catch
        {
            OnCancelled?.Invoke();
        }
    }
}
