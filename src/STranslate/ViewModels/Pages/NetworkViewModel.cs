using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using STranslate.Core;
using STranslate.Plugin;

namespace STranslate.ViewModels.Pages;

public partial class NetworkViewModel : SearchViewModelBase
{
    private readonly IHttpService _httpService;
    private readonly ILogger<NetworkViewModel> _logger;

    [ObservableProperty]
    public partial string TestResult { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsTesting { get; set; } = false;

    public Settings Settings { get; }
    public DataProvider DataProvider { get; }

    public NetworkViewModel(
        Settings settings,
        DataProvider dataProvider,
        IHttpService httpService,
        Internationalization i18n,
        ILogger<NetworkViewModel> logger) : base(i18n, "Network_")
    {
        Settings = settings;
        DataProvider = dataProvider;
        _httpService = httpService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsTesting) return;

        IsTesting = true;
        TestResult = "正在测试连接...";

        try
        {
            var isConnected = await _httpService.TestProxyAsync();
            if (isConnected)
            {
                var ip = await _httpService.GetCurrentIpAsync();
                TestResult = $"连接成功！当前IP信息：{ip}";
            }
            else
            {
                TestResult = "连接失败，请检查代理设置";
            }
        }
        catch (Exception ex)
        {
            TestResult = $"测试失败：{ex.Message}";
            _logger.LogError(ex, "代理连接测试失败");
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private void ResetSettings()
    {
        Settings.Proxy.IsEnabled = true;
        Settings.Proxy.ProxyType = ProxyType.System;
        Settings.Proxy.ProxyAddress = "127.0.0.1";
        Settings.Proxy.ProxyPort = 8080;
        Settings.Proxy.ProxyUsername = string.Empty;
        Settings.Proxy.ProxyPassword = string.Empty;
        TestResult = string.Empty;
    }
}