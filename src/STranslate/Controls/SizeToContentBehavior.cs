using System.Windows;

namespace STranslate.Controls;

public static class SizeToContentBehavior
{
    public static readonly DependencyProperty PersistentSizeToContentProperty =
        DependencyProperty.RegisterAttached(
            "PersistentSizeToContent",
            typeof(SizeToContent),
            typeof(SizeToContentBehavior),
            new PropertyMetadata(SizeToContent.Manual, OnPersistentSizeToContentChanged));

    public static void SetPersistentSizeToContent(Window window, SizeToContent value)
    {
        window.SetValue(PersistentSizeToContentProperty, value);
    }

    public static SizeToContent GetPersistentSizeToContent(Window window)
    {
        return (SizeToContent)window.GetValue(PersistentSizeToContentProperty);
    }

    private static void OnPersistentSizeToContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window)
        {
            window.SizeToContent = (SizeToContent)e.NewValue;
            window.SizeChanged += (s, args) =>
            {
                var sizeToContent = GetPersistentSizeToContent(window);
                if (window.SizeToContent == SizeToContent.Manual && sizeToContent != SizeToContent.Manual)
                {
                    window.SizeToContent = sizeToContent;
                }
            };
        }
    }
}