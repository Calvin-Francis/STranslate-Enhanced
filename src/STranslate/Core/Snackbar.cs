using STranslate.Controls;
using STranslate.Plugin;
using System.Windows;

namespace STranslate.Core;

public class Snackbar : ISnackbar
{
    private readonly Dictionary<Window, SnackbarContainer> _snackbars = [];

    public void Show(
        string message,
        Severity severity = Severity.Informational,
        int durationMs = 3000,
        string? actionText = null,
        Action? actionCallback = null)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var activeWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

            if (activeWindow == null) return;

            var snackbar = GetOrCreateSnackbar(activeWindow);
            snackbar.Show(message, severity, durationMs, actionText, actionCallback);
        });
    }

    public void ShowSuccess(string message, int durationMs = 3000)
        => Show(message, Severity.Success, durationMs);

    public void ShowError(string message, int durationMs = 4000)
        => Show(message, Severity.Error, durationMs);

    public void ShowWarning(string message, int durationMs = 3000)
        => Show(message, Severity.Warning, durationMs);

    public void ShowInfo(string message, int durationMs = 3000)
        => Show(message, Severity.Informational, durationMs);

    private SnackbarContainer GetOrCreateSnackbar(Window window)
    {
        if (_snackbars.TryGetValue(window, out var existingSnackbar))
            return existingSnackbar;

        var snackbar = new SnackbarContainer();

        // 查找窗口的主容器（通常是 Grid）
        if (window.Content is System.Windows.Controls.Panel panel)
        {
            // 添加到最顶层
            panel.Children.Add(snackbar);
            System.Windows.Controls.Panel.SetZIndex(snackbar, 9999);
        }

        _snackbars[window] = snackbar;

        // 窗口关闭时清理
        window.Closed += (s, e) => _snackbars.Remove(window);

        return snackbar;
    }
}