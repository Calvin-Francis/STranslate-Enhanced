using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace STranslate.Plugin.Translate.TransmartBuiltIn.ViewModel;

public partial class SettingsViewModel(IPluginContext context, Main main) : ObservableObject
{
    public Main Main { get; } = main;

    [ObservableProperty] public partial string ValidateResult { get; set; } = string.Empty;

    [RelayCommand]
    public async Task ValidateAsync()
    {
        try
        {
            var option = new Options
            {
                Headers = new Dictionary<string, string>
                {
                    { "User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36" },
                    { "Referer", "https://yi.qq.com/zh-CN/index" }
                }
            };

            var content = new
            {
                header = new
                {
                    fn = "auto_translation_block",
                    client_key = "browser-chrome-110.0.0-Mac OS-df4bd4c5-a65d-44b2-a40f-42f34f3535f2-1677486696487"
                },
                type = "plain",
                model_category = "normal",
                source = new
                {
                    lang = "en",
                    text_block = "Hello world"
                },
                target = new
                {
                    lang = "zh"
                }
            };

            var response = await context.HttpService.PostAsync("https://transmart.qq.com/api/imt", content, option);

            // 解析Google翻译返回的JSON
            var jsonDoc = JsonDocument.Parse(response);
            var translatedText = jsonDoc.RootElement.GetProperty("auto_translation").GetString() ?? throw new Exception(response);

            ValidateResult = context.GetTranslation("ValidationSuccess");
        }
        catch (Exception ex)
        {
            ValidateResult = context.GetTranslation("ValidationFailure");
            context.Logger.LogError(ex, context.GetTranslation("ValidationFailure"));
        }
    }
}