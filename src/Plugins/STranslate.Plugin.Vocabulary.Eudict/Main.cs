using Microsoft.Extensions.Logging;
using STranslate.Plugin.Vocabulary.Eudict.View;
using STranslate.Plugin.Vocabulary.Eudict.ViewModel;
using System.Text.Json;
using System.Windows.Controls;

namespace STranslate.Plugin.Vocabulary.Eudict;

public class Main : IVocabularyPlugin
{
    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;

    public Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel(Context, Settings);
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    public void Init(IPluginContext context)
    {
        Context = context;
        Settings = context.LoadSettingStorage<Settings>();
    }

    public void Dispose() => _viewModel?.Dispose();

    public async Task<VocabularyResult> SaveAsync(string text, CancellationToken cancellationToken)
    {
        var result = new VocabularyResult();
        const string url = "https://api.frdic.com/api/open/v1/studylist/words";
        var startTime = DateTime.Now;
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Settings.BookID, "BookId不可为空");
            var content = new
            {
                id = Settings.BookID,
                language = "en",
                words = new[] { text }
            };
            var option = new Options
            {
                Headers = new Dictionary<string, string>
                {
                    { "Authorization", Settings.Token }
                }
            };
            var resp = await Context.HttpService.PostAsync(url, content, option, cancellationToken);

            result.IsSuccess = GetResult(resp);
            if (!result.IsSuccess)
            {
                result.ErrorMessage = resp;
            }
        }
        catch (Exception ex)
        {
            result.Fail($"{Settings.BookName}保存至生词本失败: {ex.Message}");
            Context.Logger.LogError(ex, $"{Settings.BookName}保存至生词本{Settings.BookName}({Settings.BookID})失败, 请检查配置保存后重试");
        }
        finally
        {
            result.Duration = DateTime.Now - startTime;
        }

        return result;
    }

    private static bool GetResult(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.TryGetProperty("status", out var status) &&
            !string.IsNullOrEmpty(status.GetString()))
        {
            throw new Exception($"接口回复: {json}");
        }

        return true;
    }
}