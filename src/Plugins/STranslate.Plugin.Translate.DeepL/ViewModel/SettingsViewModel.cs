using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace STranslate.Plugin.Translate.DeepL.ViewModel;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IPluginContext Context;
    private readonly Settings Settings;

    public SettingsViewModel(IPluginContext context, Settings settings)
    {
        Context = context;
        Settings = settings;

        ApiKey = Settings.ApiKey;
        UseProApi = Settings.UseProApi;
        UsageStr = Settings.UsageStr;
        Usage = Settings.Usage;

        PropertyChanged += PropertyChangedHandler;
    }

    private void PropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ApiKey):
                Settings.ApiKey = ApiKey;
                break;
            case nameof(UseProApi):
                Settings.UseProApi = UseProApi;
                break;
            case nameof(UsageStr):
                Settings.UsageStr = UsageStr;
                break;
            case nameof(Usage):
                Settings.Usage = Usage;
                break;
            default:
                break;
        }
        Context.SaveSettingStorage<Settings>();
    }

    public void Dispose() => PropertyChanged -= PropertyChangedHandler;

    [ObservableProperty] public partial string ApiKey { get; set; }
    [ObservableProperty] public partial bool UseProApi { get; set; }
    [ObservableProperty] public partial string UsageStr { get; set; }
    [ObservableProperty] public partial double Usage { get; set; }

    [RelayCommand]
    private async Task QueryUsageAsync()
    {
        const string path = "/v2/usage";
        var url = Settings.UseProApi ? Constant.ProUrl : Constant.FreeUrl;
        var uriBuilder = new UriBuilder(url)
        {
            Path = path
        };
        var option = string.IsNullOrEmpty(Settings.ApiKey)
            ? default :
            new Options
            {
                Headers = new Dictionary<string, string>
                {
                    { "Authorization", "DeepL-Auth-Key " + Settings.ApiKey }
                }
            };
        try
        {
            var response = await Context.HttpService.GetAsync(uriBuilder.Uri.ToString(), option);
            var parseData = JsonNode.Parse(response);
            var count = parseData?["character_count"]?.ToString() ?? throw new Exception(Context.GetTranslation("STranslate_Plugin_Translate_DeepL_Query_Error1"));
            var limit = parseData?["character_limit"]?.ToString() ?? throw new Exception(Context.GetTranslation("STranslate_Plugin_Translate_DeepL_Query_Error2"));
            UsageStr = $"{count}/{limit}";
            Usage = double.Parse(count) / double.Parse(limit) * 100;
            Context.Snackbar.ShowSuccess(Context.GetTranslation("STranslate_Plugin_Translate_DeepL_Query_Success"));
        }
        catch (OperationCanceledException)
        {
            // ignored
            Context.Snackbar.Show(Context.GetTranslation("STranslate_Plugin_Translate_DeepL_Query_Cancel"));
        }
        catch (Exception ex)
        {
            if (ex.InnerException is { } innEx) ex = innEx;
            Context.Snackbar.ShowError(Context.GetTranslation("STranslate_Plugin_Translate_DeepL_Query_Fail"));
            Context.Logger.LogError($"DeepL query usage error: {ex.Message}");
        }
    }
}