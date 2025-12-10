using CommunityToolkit.Mvvm.DependencyInjection;
using STranslate.Core;
using STranslate.Helpers;
using STranslate.ViewModels;
using System.Windows;

namespace STranslate.Views;

public partial class MainWindow : IDisposable
{
    private readonly MainWindowViewModel _viewModel;
    private readonly Settings _settings;
    // IDisposable
    private bool _disposed = false;

    public MainWindow()
    {
        _viewModel = Ioc.Default.GetRequiredService<MainWindowViewModel>();
        _settings = Ioc.Default.GetRequiredService<Settings>();

        DataContext = _viewModel;

        InitializeComponent();

        //Notification.Show("STranslate", "Welcome to STranslate!");
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.UpdatePosition(_settings.HideOnStartup);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        if (_settings.HideOnStartup)
        {
            _viewModel.Hide();
        }
        else
        {
            _viewModel.Show();
            Win32Helper.SetForegroundWindow(this);
        }

        base.OnContentRendered(e);
    }

    protected override void OnDeactivated(EventArgs e)
    {
        if (_viewModel.IsTopmost) return;

        // win32 api和wpf层面修改窗口显隐时表现有所不同，直接使用Hide可能会导致出现在Alt-Tab栏
        // https://github.com/ZGGSONG/STranslate/issues/165
        if (_settings.HideWhenDeactivated)
            _viewModel.Hide();

        base.OnDeactivated(e);
    }

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                PART_NotifyIcon.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}