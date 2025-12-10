using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace STranslate.Controls;

public class IconButton : Control
{
    public enum IconButtonType
    {
        /// <summary>
        /// 一次性按钮
        /// </summary>
        Once,
        /// <summary>
        /// 切换按钮
        /// </summary>
        Toggle
    }

    static IconButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(IconButton),
            new FrameworkPropertyMetadata(typeof(IconButton)));
    }

    public IconButtonType Type
    {
        get => (IconButtonType)GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public static readonly DependencyProperty TypeProperty =
        DependencyProperty.Register(
            nameof(Type),
            typeof(IconButtonType),
            typeof(IconButton),
            new PropertyMetadata(IconButtonType.Once));

    public object Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(IconButton),
            new FrameworkPropertyMetadata(
                default, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(
            nameof(IconSize),
            typeof(double),
            typeof(IconButton),
            new PropertyMetadata(16.0));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(IconButton));

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public static readonly DependencyProperty IsOnProperty =
        DependencyProperty.Register(
            nameof(IsOn),
            typeof(bool),
            typeof(IconButton),
            new FrameworkPropertyMetadata(
                false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(IconButton));
}
