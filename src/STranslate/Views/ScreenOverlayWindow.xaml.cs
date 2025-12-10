using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using CommunityToolkit.Mvvm.DependencyInjection;
using STranslate.Controls;
using STranslate.ViewModels;

namespace STranslate.Views;

/// <summary>
/// 屏幕覆盖翻译窗口 - 用于在屏幕上直接显示翻译结果
/// 这是一个完全透明的窗口，直接覆盖在截图原位置
/// </summary>
public partial class ScreenOverlayWindow : Window
{
    // Windows API 直接设置窗口位置
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_SHOWWINDOW = 0x0040;

    private Size _containerSize;

    public ScreenOverlayWindow()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<ScreenOverlayWindowViewModel>();
        
        // 订阅 ViewModel 的文本块更新事件
        if (DataContext is ScreenOverlayWindowViewModel vm)
        {
            vm.TextBlocksReady += OnTextBlocksReady;
        }

        SizeChanged += (s, e) => _containerSize = e.NewSize;
    }

    private Rect? _pendingScreenBounds;

    /// <summary>
    /// 设置窗口位置和大小以匹配截图区域
    /// </summary>
    /// <param name="screenBounds">屏幕坐标（像素）</param>
    public void SetScreenBounds(Rect screenBounds)
    {
        if (screenBounds.Width <= 0 || screenBounds.Height <= 0)
            return;

        // 保存坐标，在 Loaded 事件中应用
        _pendingScreenBounds = screenBounds;

        // 如果窗口已加载，立即应用
        if (IsLoaded)
        {
            ApplyScreenBounds();
        }
        else
        {
            Loaded += OnLoadedApplyBounds;
        }
    }

    private void OnLoadedApplyBounds(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedApplyBounds;
        ApplyScreenBounds();
    }

    private void ApplyScreenBounds()
    {
        if (_pendingScreenBounds == null) return;

        var screenBounds = _pendingScreenBounds.Value;
        _pendingScreenBounds = null;

        // 使用 Windows API 直接设置窗口位置（像素坐标，绕过 WPF 逻辑坐标系统）
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            SetWindowPos(
                hwnd,
                HWND_TOPMOST,
                (int)screenBounds.Left,
                (int)screenBounds.Top,
                (int)screenBounds.Width,
                (int)screenBounds.Height,
                SWP_SHOWWINDOW);
        }
        else
        {
            // 回退：使用 WPF 方式
            var source = PresentationSource.FromVisual(this);
            double dpiScaleX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double dpiScaleY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

            Left = screenBounds.Left / dpiScaleX;
            Top = screenBounds.Top / dpiScaleY;
            Width = screenBounds.Width / dpiScaleX;
            Height = screenBounds.Height / dpiScaleY;
        }

        _containerSize = new Size(screenBounds.Width, screenBounds.Height);
    }

    /// <summary>
    /// 当 ViewModel 准备好文本块数据时调用
    /// </summary>
    private void OnTextBlocksReady(object? sender, List<TextBlockData> textBlocks)
    {
        Dispatcher.Invoke(() =>
        {
            TextBlockCanvas.Children.Clear();

            foreach (var data in textBlocks)
            {
                var textBlock = new InteractiveTextBlock
                {
                    Text = data.Text,
                    TextFontSize = data.FontSize,
                    OriginalBounds = data.Bounds,
                    ContainerSize = _containerSize,
                    Width = data.Bounds.Width,
                    MinHeight = data.Bounds.Height
                };

                textBlock.InitializeLayout(data.Bounds.Width, data.Bounds.Height);

                Canvas.SetLeft(textBlock, data.Bounds.Left);
                Canvas.SetTop(textBlock, data.Bounds.Top);

                TextBlockCanvas.Children.Add(textBlock);
            }
        });
    }

    /// <summary>
    /// Canvas 点击处理 - 双击关闭，单击拖动
    /// </summary>
    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 只处理直接点击 Canvas 的事件（不是子元素）
        if (e.OriginalSource != TextBlockCanvas) return;

        // 单击空白区域先退出所有文本块的编辑状态
        foreach (var child in TextBlockCanvas.Children)
        {
            if (child is InteractiveTextBlock block)
            {
                block.ExitEditIfEditing();
            }
        }

        if (e.ClickCount == 2)
        {
            Close();
        }
        else
        {
            DragMove();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is ScreenOverlayWindowViewModel vm)
        {
            vm.TextBlocksReady -= OnTextBlocksReady;
        }
        base.OnClosed(e);
    }
}
