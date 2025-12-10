using System.Windows;
using System.Windows.Controls;

namespace STranslate.Controls;

public static class SnackbarBehavior
{
    public static readonly DependencyProperty EnableSnackbarProperty =
        DependencyProperty.RegisterAttached(
            "EnableSnackbar",
            typeof(bool),
            typeof(SnackbarBehavior),
            new PropertyMetadata(false, OnEnableSnackbarChanged));

    public static bool GetEnableSnackbar(DependencyObject obj)
        => (bool)obj.GetValue(EnableSnackbarProperty);

    public static void SetEnableSnackbar(DependencyObject obj, bool value)
        => obj.SetValue(EnableSnackbarProperty, value);

    private static void OnEnableSnackbarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window) return;

        if ((bool)e.NewValue)
        {
            window.Loaded += Window_Loaded;
        }
        else
        {
            window.Loaded -= Window_Loaded;
        }
    }

    private static void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Window window) return;

        // 确保窗口内容是 Panel 类型
        if (window.Content is Panel panel)
        {
            // 检查是否已经添加了 SnackbarContainer
            var existingSnackbar = panel.Children.OfType<SnackbarContainer>().FirstOrDefault();
            if (existingSnackbar == null)
            {
                var snackbar = new SnackbarContainer();
                panel.Children.Add(snackbar);
                Panel.SetZIndex(snackbar, 9999);
            }
        }
    }
}
