using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObservableCollections;
using STranslate.Core;
using STranslate.Plugin;

namespace STranslate.ViewModels.Pages;

public partial class HistoryViewModel : ObservableObject
{
    private const int PageSize = 20;
    private const int searchDelayMilliseconds = 500;

    private readonly SqlService _sqlService;
    private readonly ISnackbar _snackbar;
    private readonly Internationalization _i18n;
    private readonly Timer _searchTimer;

    private CancellationTokenSource? _searchCts;
    private DateTime _lastCursorTime = DateTime.Now;
    private bool _isLoading = false;

    private bool CanLoadMore =>
        !_isLoading &&
        string.IsNullOrEmpty(SearchText) &&
        (TotalCount == 0 || _items.Count != TotalCount);

    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;

    /// <summary>
    /// <see href="https://blog.coldwind.top/posts/more-observable-collections/"/>
    /// </summary>
    private readonly ObservableList<HistoryModel> _items = [];

    public INotifyCollectionChangedSynchronizedViewList<HistoryModel> HistoryItems { get; }

    [ObservableProperty] public partial HistoryModel? SelectedItem { get; set; }

    [ObservableProperty] public partial long TotalCount { get; set; }

    public HistoryViewModel(
        SqlService sqlService,
        ISnackbar snackbar,
        Internationalization i18n)
    {
        _sqlService = sqlService;
        _snackbar = snackbar;
        _i18n = i18n;
        _searchTimer = new Timer(async _ => await SearchAsync(), null, Timeout.Infinite, Timeout.Infinite);

        HistoryItems = _items.ToNotifyCollectionChanged();

        _ = RefreshAsync();
    }

    // 搜索文本变化时修改定时器
    partial void OnSearchTextChanged(string value) => _searchTimer.Change(searchDelayMilliseconds, Timeout.Infinite);

    private async Task SearchAsync()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        if (string.IsNullOrEmpty(SearchText))
        {
            await RefreshAsync();
            return;
        }

        var historyItems = await _sqlService.GetDataAsync(SearchText, _searchCts.Token);

        App.Current.Dispatcher.Invoke(() =>
        {
            _items.Clear();
            if (historyItems == null) return;

            _items.AddRange(historyItems);
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        TotalCount = await _sqlService.GetCountAsync();

        App.Current.Dispatcher.Invoke(() => _items.Clear());
        _lastCursorTime = DateTime.Now;

        await LoadMoreAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(HistoryModel historyModel)
    {
        var success = await _sqlService.DeleteDataAsync(historyModel);
        if (success)
        {
            App.Current.Dispatcher.Invoke(() => _items.Remove(historyModel));
            TotalCount--;
        }
        else
            _snackbar.ShowError(_i18n.GetTranslation("OperationFailed"));
    }

    [RelayCommand]
    private void Copy(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Utilities.SetText(text);
        _snackbar.ShowSuccess(_i18n.GetTranslation("CopySuccess"));
    }

    [RelayCommand(CanExecute = nameof(CanLoadMore))]
    private async Task LoadMoreAsync()
    {
        try
        {
            _isLoading = true;

            var historyData = await _sqlService.GetDataCursorPagedAsync(PageSize, _lastCursorTime);
            if (!historyData.Any()) return;

            App.Current.Dispatcher.Invoke(() =>
            {
                // 更新游标
                _lastCursorTime = historyData.Last().Time;
                var uniqueHistoryItems = historyData.Where(h => !_items.Any(existing => existing.Id == h.Id));
                _items.AddRange(uniqueHistoryItems);
            });
        }
        finally
        {
            _isLoading = false;
            LoadMoreCommand.NotifyCanExecuteChanged();
        }
    }
}