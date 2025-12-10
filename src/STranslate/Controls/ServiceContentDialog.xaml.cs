using iNKORE.UI.WPF.Modern.Controls;
using STranslate.Plugin;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace STranslate.Controls;

public partial class ServiceContentDialog : INotifyPropertyChanged
{
    public ServiceContentDialog(string title, ObservableCollection<PluginMetaData> itemsSource)
    {
        ServiceTitle = title;

        InitializeComponent();

        DataContext = this;

        _collectionViewSource = new() { Source = itemsSource };
        _collectionViewSource.Filter += OnFilter;
    }

    private void OnFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not PluginMetaData plugin)
        {
            e.Accepted = false;
            return;
        }

        // 文本筛选
        var textMatch = string.IsNullOrEmpty(FilterText) || plugin.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
        e.Accepted = textMatch;
    }

    private readonly CollectionViewSource _collectionViewSource;
    public ICollectionView CollectionView => _collectionViewSource.View;

    private string _filterText = string.Empty;
    public string FilterText
    {
        get => _filterText;
        set
        {
            if (_filterText != value)
            {
                _filterText = value;
                _collectionViewSource.View?.Refresh();
                OnPropertyChanged();
            }
        }
    }

    public string ServiceTitle
    {
        get => (string)GetValue(ServiceTitleProperty);
        set => SetValue(ServiceTitleProperty, value);
    }

    public static readonly DependencyProperty ServiceTitleProperty =
        DependencyProperty.Register(
            nameof(ServiceTitle),
            typeof(string),
            typeof(ServiceContentDialog),
            new PropertyMetadata(string.Empty));

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(ServiceContentDialog), null);

    private void OnClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
        => _collectionViewSource.Filter -= OnFilter;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.F || Keyboard.Modifiers is not ModifierKeys.Control) return;

        PART_FilterTextBox.Focus();
        PART_FilterTextBox.SelectAll();
    }
}