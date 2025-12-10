using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace STranslate.Controls;

/// <summary>
/// 可交互的翻译文本块控件
/// 支持：拖拽、Ctrl+滚轮缩放、双击编辑
/// </summary>
public class InteractiveTextBlock : Border
{
    private readonly TextBlock _textBlock;
    private readonly TextBox _textBox;
    private bool _isEditing;
    private bool _isDragging;
    private Point _dragStartPoint;
    private Point _elementStartPosition;
    private double _fontSize;
    private const double MinFontSize = 8;
    private const double MaxFontSize = 72;
    private const double PaddingX = 8;
    private const double PaddingY = 4;

    // 原始边界尺寸（用于填充整个识别区域）
    private double _originalWidth;
    private double _originalHeight;
    private double _scaleFactor = 1.0;

    // 保证同一时间只有一个文本块处于编辑状态
    private static InteractiveTextBlock? _currentEditing;

    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text
    {
        get => _textBlock.Text;
        set
        {
            _textBlock.Text = value;
            _textBox.Text = value;
        }
    }

    /// <summary>
    /// 字体大小
    /// </summary>
    public double TextFontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = Math.Clamp(value, MinFontSize, MaxFontSize);
            _textBlock.FontSize = _fontSize;
            _textBox.FontSize = _fontSize;
        }
    }

    /// <summary>
    /// 原始边界矩形（用于限制拖拽范围）
    /// </summary>
    public Rect OriginalBounds { get; set; }

    /// <summary>
    /// 父容器尺寸（用于限制拖拽范围）
    /// </summary>
    public Size ContainerSize { get; set; }

    public InteractiveTextBlock()
    {
        // 设置边框样式
        Background = new SolidColorBrush(Colors.White);
        BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); // 默认无边框
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(2);
        Cursor = Cursors.Hand;

        // 创建文本显示控件
        _textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.NoWrap,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = new SolidColorBrush(Colors.Black),
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Microsoft YaHei, Arial, SimSun")
        };

        // 创建文本编辑控件（使用极简模板，彻底移除清除按钮）
        _textBox = new TextBox
        {
            TextWrapping = TextWrapping.NoWrap,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Foreground = new SolidColorBrush(Colors.Black),
            Background = new SolidColorBrush(Colors.White),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Microsoft YaHei, Arial, SimSun"),
            AcceptsReturn = false,
            IsUndoEnabled = false,
            Visibility = Visibility.Collapsed
        };
        // 使用极简模板，彻底移除 Windows 10/11 的清除按钮
        _textBox.Template = CreateMinimalTextBoxTemplate();
        SpellCheck.SetIsEnabled(_textBox, false);

        // 使用 Grid 容纳两个控件
        var grid = new Grid();
        grid.Children.Add(_textBlock);
        grid.Children.Add(_textBox);
        Child = grid;

        // 绑定事件
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseMove += OnMouseMove;
        MouseWheel += OnMouseWheel;
        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;

        _textBox.LostFocus += OnTextBoxLostFocus;
        _textBox.KeyDown += OnTextBoxKeyDown;

        _fontSize = 14;
        // 尺寸将在 InitializeLayout 中设置
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        // 鼠标悬停时显示边框
        BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 215));
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (!_isEditing)
        {
            // 鼠标离开时隐藏边框
            BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isEditing) return;

        if (e.ClickCount == 2)
        {
            // 双击进入编辑模式
            EnterEditMode();
            e.Handled = true;
        }
        else
        {
            // 单击开始拖拽
            _isDragging = true;
            _dragStartPoint = e.GetPosition(Parent as UIElement);
            _elementStartPosition = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
            CaptureMouse();
            e.Handled = true;
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _isEditing) return;

        var currentPoint = e.GetPosition(Parent as UIElement);
        var offset = new Point(
            currentPoint.X - _dragStartPoint.X,
            currentPoint.Y - _dragStartPoint.Y);

        var newLeft = _elementStartPosition.X + offset.X;
        var newTop = _elementStartPosition.Y + offset.Y;

        // 限制在容器内
        newLeft = Math.Clamp(newLeft, 0, ContainerSize.Width - ActualWidth);
        newTop = Math.Clamp(newTop, 0, ContainerSize.Height - ActualHeight);

        Canvas.SetLeft(this, newLeft);
        Canvas.SetTop(this, newTop);
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Ctrl+滚轮缩放
        if (Keyboard.Modifiers != ModifierKeys.Control) return;

        // 记录旧尺寸和中心点
        var oldWidth = ActualWidth;
        var oldHeight = ActualHeight;
        var centerX = Canvas.GetLeft(this) + oldWidth / 2;
        var centerY = Canvas.GetTop(this) + oldHeight / 2;

        // 调整缩放因子和字体大小
        var scaleDelta = e.Delta > 0 ? 0.1 : -0.1;
        var newScaleFactor = Math.Clamp(_scaleFactor + scaleDelta, 0.5, 3.0);
        
        if (Math.Abs(newScaleFactor - _scaleFactor) < 0.01)
        {
            e.Handled = true;
            return;
        }

        _scaleFactor = newScaleFactor;
        
        // 同步调整字体大小
        var fontDelta = e.Delta > 0 ? 2 : -2;
        TextFontSize = _fontSize + fontDelta;

        // 更新尺寸
        UpdateSize();

        // 以文本块中心为锚点调整位置
        {
            UpdateLayout();
            var newLeft = centerX - ActualWidth / 2;
            var newTop = centerY - ActualHeight / 2;

            // 限制在容器内
            newLeft = Math.Clamp(newLeft, 0, Math.Max(0, ContainerSize.Width - ActualWidth));
            newTop = Math.Clamp(newTop, 0, Math.Max(0, ContainerSize.Height - ActualHeight));

            Canvas.SetLeft(this, newLeft);
            Canvas.SetTop(this, newTop);
        }

        e.Handled = true;
    }

    /// <summary>
    /// 进入编辑模式
    /// </summary>
    private void EnterEditMode()
    {
        // 先让其他文本块退出编辑
        _currentEditing?.ExitEditIfEditing();
        _currentEditing = this;

        _isEditing = true;
        _textBox.Text = _textBlock.Text;
        _textBlock.Visibility = Visibility.Collapsed;
        _textBox.Visibility = Visibility.Visible;

        // 锁定编辑框尺寸，避免切换时尺寸重算导致“漂移”
        _textBox.Width = ActualWidth;
        _textBox.Height = ActualHeight;

        _textBox.Focus();
        _textBox.SelectAll();
        Cursor = Cursors.IBeam;
        BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 215));
    }

    /// <summary>
    /// 退出编辑模式
    /// </summary>
    private void ExitEditMode()
    {
        _isEditing = false;
        _textBlock.Text = _textBox.Text;
        _textBox.Visibility = Visibility.Collapsed;
        _textBlock.Visibility = Visibility.Visible;
        _textBox.Width = double.NaN;
        _textBox.Height = double.NaN;
        Cursor = Cursors.Hand;
        BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        if (_currentEditing == this)
        {
            _currentEditing = null;
        }
    }

    /// <summary>
    /// 外部调用：如果当前在编辑则退出
    /// </summary>
    public void ExitEditIfEditing()
    {
        if (_isEditing)
        {
            ExitEditMode();
        }
    }

    private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (_isEditing)
        {
            ExitEditMode();
        }
    }

    private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Esc 取消编辑，恢复原文本
            _textBox.Text = _textBlock.Text;
            ExitEditMode();
            e.Handled = true;
        }
    }

    /// <summary>
    /// 初始化布局（使用原始识别区域尺寸）
    /// </summary>
    public void InitializeLayout(double boundsWidth, double boundsHeight)
    {
        _originalWidth = boundsWidth;
        _originalHeight = boundsHeight;
        _scaleFactor = 1.0;
        UpdateSize();
    }

    /// <summary>
    /// 更新尺寸（填充整个识别区域，缩放时按比例扩大）
    /// </summary>
    private void UpdateSize()
    {
        // 使用原始边界尺寸乘以缩放因子
        double desiredWidth = _originalWidth * _scaleFactor;
        double desiredHeight = _originalHeight * _scaleFactor;

        // 确保最小尺寸
        desiredWidth = Math.Max(desiredWidth, 20);
        desiredHeight = Math.Max(desiredHeight, 16);

        Width = desiredWidth;
        Height = desiredHeight;

        // 同步编辑框尺寸
        _textBox.Width = desiredWidth;
        _textBox.Height = desiredHeight;
    }

    /// <summary>
    /// 创建极简 TextBox 模板（只包含内容呈现器，无清除按钮）
    /// </summary>
    private static ControlTemplate CreateMinimalTextBoxTemplate()
    {
        var template = new ControlTemplate(typeof(TextBox));

        // 创建外层 Border
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
        borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));

        // 创建 ScrollViewer（TextBox 必需）
        var scrollViewerFactory = new FrameworkElementFactory(typeof(ScrollViewer));
        scrollViewerFactory.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
        scrollViewerFactory.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
        scrollViewerFactory.SetValue(ScrollViewer.PaddingProperty, new TemplateBindingExtension(PaddingProperty));
        scrollViewerFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        scrollViewerFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        scrollViewerFactory.Name = "PART_ContentHost";

        borderFactory.AppendChild(scrollViewerFactory);
        template.VisualTree = borderFactory;

        return template;
    }
}
