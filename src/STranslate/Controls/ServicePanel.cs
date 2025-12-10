using STranslate.Plugin;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace STranslate.Controls;

public class ServicePanel : ListBox
{
    static ServicePanel()
        => DefaultStyleKeyProperty.OverrideMetadata(typeof(ServicePanel),
            new FrameworkPropertyMetadata(typeof(ServicePanel)));

    public ICommand? ActiveReplaceCommand
    {
        get => (ICommand?)GetValue(ActiveReplaceCommandProperty);
        set => SetValue(ActiveReplaceCommandProperty, value);
    }

    public static readonly DependencyProperty ActiveReplaceCommandProperty =
        DependencyProperty.Register(
            nameof(ActiveReplaceCommand),
            typeof(ICommand),
            typeof(ServicePanel));

    public ICommand? ActiveImTranCommand
    {
        get => (ICommand?)GetValue(ActiveImTranCommandProperty);
        set => SetValue(ActiveImTranCommandProperty, value);
    }

    public static readonly DependencyProperty ActiveImTranCommandProperty =
        DependencyProperty.Register(
            nameof(ActiveImTranCommand),
            typeof(ICommand),
            typeof(ServicePanel));

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(
            nameof(DeleteCommand),
            typeof(ICommand),
            typeof(ServicePanel));

    public ICommand? DuplicateCommand
    {
        get => (ICommand?)GetValue(DuplicateCommandProperty);
        set => SetValue(DuplicateCommandProperty, value);
    }

    public static readonly DependencyProperty DuplicateCommandProperty =
        DependencyProperty.Register(
            nameof(DuplicateCommand),
            typeof(ICommand),
            typeof(ServicePanel));

    public Service? ReplaceService
    {
        get => (Service?)GetValue(ReplaceServiceProperty);
        set => SetValue(ReplaceServiceProperty, value);
    }

    public static readonly DependencyProperty ReplaceServiceProperty =
        DependencyProperty.Register(
            nameof(ReplaceService),
            typeof(Service),
            typeof(ServicePanel));

    public Service? ImTranService
    {
        get => (Service?)GetValue(ImTranServiceProperty);
        set => SetValue(ImTranServiceProperty, value);
    }

    public static readonly DependencyProperty ImTranServiceProperty =
        DependencyProperty.Register(
            nameof(ImTranService),
            typeof(Service),
            typeof(ServicePanel));

}