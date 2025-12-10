using iNKORE.UI.WPF.Modern.Controls;
using STranslate.Plugin;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace STranslate.Controls;

public partial class SnackbarContainer : UserControl
{
    private DispatcherTimer? _autoHideTimer;
    private Action? _actionCallback;
    private Storyboard? _currentShowStoryboard;
    private Storyboard? _currentHideStoryboard;

    public SnackbarContainer()
    {
        InitializeComponent();
        Visibility = Visibility.Collapsed;
    }

    public void Show(
        string message,
        Severity severity = Severity.Informational,
        int durationMs = 3000,
        string? actionText = null,
        Action? actionCallback = null)
    {
        // 立即停止所有正在进行的动画
        CancelCurrentAnimations();

        InfoBarControl.Title = string.Empty;
        InfoBarControl.Message = message;
        InfoBarControl.Severity = ConvertSeverity(severity);
        _actionCallback = actionCallback;

        // 设置动作按钮
        if (!string.IsNullOrEmpty(actionText) && actionCallback != null)
        {
            var actionButton = new Button
            {
                Content = actionText,
                Margin = new Thickness(0, 0, 8, 0)
            };
            actionButton.Click += ActionButton_Click;
            InfoBarControl.ActionButton = actionButton;
        }
        else
        {
            InfoBarControl.ActionButton = null;
        }

        // 显示
        Visibility = Visibility.Visible;
        InfoBarHost.IsHitTestVisible = true;
        InfoBarControl.IsOpen = true;

        // 播放显示动画
        _currentShowStoryboard = (Storyboard)FindResource("ShowStoryboard");
        _currentShowStoryboard.Begin();

        // 自动隐藏
        if (durationMs > 0)
        {
            _autoHideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(durationMs)
            };
            _autoHideTimer.Tick += (s, e) =>
            {
                _autoHideTimer.Stop();
                Hide();
            };
            _autoHideTimer.Start();
        }
    }

    public void Hide()
    {
        // 立即停止所有正在进行的动画
        CancelCurrentAnimations();

        _currentHideStoryboard = (Storyboard)FindResource("HideStoryboard");

        // 使用命名的事件处理程序，确保可以正确移除
        EventHandler? completedHandler = null;
        completedHandler = (s, e) =>
        {
            // 移除事件处理程序，避免重复触发
            if (_currentHideStoryboard != null)
            {
                _currentHideStoryboard.Completed -= completedHandler;
            }

            InfoBarControl.IsOpen = false;
            Visibility = Visibility.Collapsed;
            InfoBarHost.IsHitTestVisible = false;
            _currentHideStoryboard = null;
        };

        _currentHideStoryboard.Completed += completedHandler;
        _currentHideStoryboard.Begin();
    }

    /// <summary>
    /// 取消所有正在进行的动画和定时器
    /// </summary>
    private void CancelCurrentAnimations()
    {
        // 停止定时器
        _autoHideTimer?.Stop();
        _autoHideTimer = null;

        // 停止显示动画
        if (_currentShowStoryboard != null)
        {
            _currentShowStoryboard.Stop();
            _currentShowStoryboard = null;
        }

        // 停止隐藏动画
        if (_currentHideStoryboard != null)
        {
            _currentHideStoryboard.Stop();
            _currentHideStoryboard = null;
        }
    }

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        _actionCallback?.Invoke();
        Hide();
    }

    private void InfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        Hide();
    }

    private static InfoBarSeverity ConvertSeverity(Severity severity)
    {
        return severity switch
        {
            Severity.Success => InfoBarSeverity.Success,
            Severity.Warning => InfoBarSeverity.Warning,
            Severity.Error => InfoBarSeverity.Error,
            _ => InfoBarSeverity.Informational
        };
    }
}