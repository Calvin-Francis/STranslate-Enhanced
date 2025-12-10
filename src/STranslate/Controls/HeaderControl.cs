using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace STranslate.Controls;

public class HeaderControl : Control
{
    static HeaderControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HeaderControl),
            new FrameworkPropertyMetadata(typeof(HeaderControl)));
    }

    public bool IsTopmost
    {
        get => (bool)GetValue(IsTopmostProperty);
        set => SetValue(IsTopmostProperty, value);
    }

    public static readonly DependencyProperty IsTopmostProperty =
        DependencyProperty.Register(
            nameof(IsTopmost),
            typeof(bool),
            typeof(HeaderControl),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    #region Setting

    public bool IsSettingVisible
    {
        get => (bool)GetValue(IsSettingVisibleProperty);
        set => SetValue(IsSettingVisibleProperty, value);
    }

    public static readonly DependencyProperty IsSettingVisibleProperty =
        DependencyProperty.Register(
            nameof(IsSettingVisible),
            typeof(bool),
            typeof(HeaderControl),
            new FrameworkPropertyMetadata(
                true,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ICommand? SettingCommand
    {
        get => (ICommand?)GetValue(SettingCommandProperty);
        set => SetValue(SettingCommandProperty, value);
    }

    public static readonly DependencyProperty SettingCommandProperty =
        DependencyProperty.Register(
            nameof(SettingCommand),
            typeof(ICommand),
            typeof(HeaderControl));

    #endregion

    #region HideInput

    public bool IsHideInput
    {
        get => (bool)GetValue(IsHideInputProperty);
        set => SetValue(IsHideInputProperty, value);
    }

    public static readonly DependencyProperty IsHideInputProperty =
        DependencyProperty.Register(
            nameof(IsHideInput),
            typeof(bool),
            typeof(HeaderControl),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsHideInputVisible
    {
        get => (bool)GetValue(IsHideInputVisibleProperty);
        set => SetValue(IsHideInputVisibleProperty, value);
    }

    public static readonly DependencyProperty IsHideInputVisibleProperty =
        DependencyProperty.Register(
            nameof(IsHideInputVisible),
            typeof(bool),
            typeof(HeaderControl),
            new FrameworkPropertyMetadata(
                true,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    #endregion

    #region ScreenshotTranslateInImage

    public bool ScreenshotTranslateInImage
    {
        get => (bool)GetValue(ScreenshotTranslateInImageProperty);
        set => SetValue(ScreenshotTranslateInImageProperty, value);
    }

    public static readonly DependencyProperty ScreenshotTranslateInImageProperty =
        DependencyProperty.Register(
            nameof(ScreenshotTranslateInImage),
            typeof(bool),
            typeof(HeaderControl),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsScreenshotTranslateInImageVisible
    {
        get => (bool)GetValue(IsScreenshotTranslateInImageVisibleProperty);
        set => SetValue(IsScreenshotTranslateInImageVisibleProperty, value);
    }

    public static readonly DependencyProperty IsScreenshotTranslateInImageVisibleProperty =
        DependencyProperty.Register(
            nameof(IsScreenshotTranslateInImageVisible),
            typeof(bool),
            typeof(HeaderControl),
            new FrameworkPropertyMetadata(
                true,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    #endregion

    #region ColorScheme

    public bool IsColorSchemeVisible
    {
        get => (bool)GetValue(IsColorSchemeVisibleProperty);
        set => SetValue(IsColorSchemeVisibleProperty, value);
    }

    public static readonly DependencyProperty IsColorSchemeVisibleProperty =
        DependencyProperty.Register(
            nameof(IsColorSchemeVisible),
            typeof(bool),
            typeof(HeaderControl),
            new FrameworkPropertyMetadata(
                true,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ICommand? ColorSchemeCommand
    {
        get => (ICommand?)GetValue(ColorSchemeCommandProperty);
        set => SetValue(ColorSchemeCommandProperty, value);
    }

    public static readonly DependencyProperty ColorSchemeCommandProperty =
        DependencyProperty.Register(
            nameof(ColorSchemeCommand),
            typeof(ICommand),
            typeof(HeaderControl));

    #endregion

    #region MouseHook

    public bool IsMouseHook
    {
        get => (bool)GetValue(IsMouseHookProperty);
        set => SetValue(IsMouseHookProperty, value);
    }

    public static readonly DependencyProperty IsMouseHookProperty =
        DependencyProperty.Register(
            nameof(IsMouseHook),
            typeof(bool),
            typeof(HeaderControl),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsMouseHookVisible
    {
        get => (bool)GetValue(IsMouseHookVisibleProperty);
        set => SetValue(IsMouseHookVisibleProperty, value);
    }

    public static readonly DependencyProperty IsMouseHookVisibleProperty =
        DependencyProperty.Register(
            nameof(IsMouseHookVisible),
            typeof(bool),
            typeof(HeaderControl),
            new FrameworkPropertyMetadata(
                true,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    #endregion

    #region History

    public bool IsHistoryNavigationVisible
    {
        get => (bool)GetValue(IsHistoryNavigationVisibleProperty);
        set => SetValue(IsHistoryNavigationVisibleProperty, value);
    }

    public static readonly DependencyProperty IsHistoryNavigationVisibleProperty =
        DependencyProperty.Register(
            nameof(IsHistoryNavigationVisible),
            typeof(bool),
            typeof(HeaderControl),
            new FrameworkPropertyMetadata(
                true,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ICommand? HistoryPreviousCommand
    {
        get => (ICommand?)GetValue(HistoryPreviousCommandProperty);
        set => SetValue(HistoryPreviousCommandProperty, value);
    }

    public static readonly DependencyProperty HistoryPreviousCommandProperty =
        DependencyProperty.Register(
            nameof(HistoryPreviousCommand),
            typeof(ICommand),
            typeof(HeaderControl));

    public ICommand? HistoryNextCommand
    {
        get => (ICommand?)GetValue(HistoryNextCommandProperty);
        set => SetValue(HistoryNextCommandProperty, value);
    }

    public static readonly DependencyProperty HistoryNextCommandProperty =
        DependencyProperty.Register(
            nameof(HistoryNextCommand),
            typeof(ICommand),
            typeof(HeaderControl));

    #endregion

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PART_Border") is Border border)
        {
            border.MouseLeftButtonDown += (s, e) =>
            {
                Window.GetWindow(this)?.DragMove();
            };
        }
    }
}
