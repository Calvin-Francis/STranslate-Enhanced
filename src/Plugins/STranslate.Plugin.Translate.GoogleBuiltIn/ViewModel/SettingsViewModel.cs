using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace STranslate.Plugin.Translate.GoogleBuiltIn.ViewModel;

public partial class SettingsViewModel(IPluginContext context, Main main) : ObservableObject
{
    public Main Main { get; } = main;
    [ObservableProperty] public partial string ValidateResult { get; set; } = string.Empty;

    [RelayCommand]
    public async Task ValidateAsync()
    {
        try
        {
            var content = new
            {
                text = "Hello world!",
                source_lang = "auto",
                target_lang = "zh-CN"
            };
            var response = await context.HttpService.PostAsync("https://googlet.deno.dev/translate", content);

            // 解析Google翻译返回的JSON
            var jsonDoc = JsonDocument.Parse(response);
            var translatedText = jsonDoc.RootElement.GetProperty("data").GetString() ?? throw new Exception(response);

            ValidateResult = context.GetTranslation("ValidationSuccess");
        }
        catch (Exception ex)
        {
            ValidateResult = context.GetTranslation("ValidationFailure");
            context.Logger.LogError(ex, context.GetTranslation("ValidationFailure"));
        }
    }
}