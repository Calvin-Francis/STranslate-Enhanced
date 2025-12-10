using STranslate.Plugin.Translate.DeepL.View;
using STranslate.Plugin.Translate.DeepL.ViewModel;
using System.Text.Json.Nodes;
using System.Windows.Controls;

namespace STranslate.Plugin.Translate.DeepL;

public class Main : TranslatePluginBase
{
    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;

    public override Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel(Context, Settings);
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    /// <summary>
    /// <see href="https://developers.deepl.com/docs/getting-started/supported-languages"/>
    /// * May be have some differences with STranslate internal LangEnum
    /// </summary>
    /// <param name="langEnum"></param>
    /// <returns></returns>
    public override string? GetSourceLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.Auto => "auto",
        LangEnum.ChineseSimplified => "ZH",
        LangEnum.ChineseTraditional => "ZH",
        LangEnum.Cantonese => "ZH",
        LangEnum.English => "EN",
        LangEnum.Japanese => "JA",
        LangEnum.Korean => "KO",
        LangEnum.French => "FR",
        LangEnum.Spanish => "ES",
        LangEnum.Russian => "RU",
        LangEnum.German => "DE",
        LangEnum.Italian => "IT",
        LangEnum.Turkish => "TR",
        LangEnum.PortuguesePortugal => "PT-PT",
        LangEnum.PortugueseBrazil => "PT-BR",
        LangEnum.Vietnamese => null,
        LangEnum.Indonesian => "ID",
        LangEnum.Thai => null,
        LangEnum.Malay => null,
        LangEnum.Arabic => "AR",
        LangEnum.Hindi => null,
        LangEnum.MongolianCyrillic => null,
        LangEnum.MongolianTraditional => null,
        LangEnum.Khmer => null,
        LangEnum.NorwegianBokmal => "NB",
        LangEnum.NorwegianNynorsk => "NB",
        LangEnum.Persian => null,
        LangEnum.Swedish => "SV",
        LangEnum.Polish => "PL",
        LangEnum.Dutch => "NL",
        LangEnum.Ukrainian => null,
        _ => "auto"
    };

    /// <summary>
    /// <see href="https://developers.deepl.com/docs/getting-started/supported-languages"/>
    /// * May be have some differences with STranslate internal LangEnum
    /// </summary>
    /// <param name="langEnum"></param>
    /// <returns></returns>
    public override string? GetTargetLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.Auto => "auto",
        LangEnum.ChineseSimplified => "ZH-HANS",
        LangEnum.ChineseTraditional => "ZH-HANT",
        LangEnum.Cantonese => "ZH",
        LangEnum.English => "EN",
        LangEnum.Japanese => "JA",
        LangEnum.Korean => "KO",
        LangEnum.French => "FR",
        LangEnum.Spanish => "ES",
        LangEnum.Russian => "RU",
        LangEnum.German => "DE",
        LangEnum.Italian => "IT",
        LangEnum.Turkish => "TR",
        LangEnum.PortuguesePortugal => "PT-PT",
        LangEnum.PortugueseBrazil => "PT-BR",
        LangEnum.Vietnamese => null,
        LangEnum.Indonesian => "ID",
        LangEnum.Thai => null,
        LangEnum.Malay => null,
        LangEnum.Arabic => "AR",
        LangEnum.Hindi => null,
        LangEnum.MongolianCyrillic => null,
        LangEnum.MongolianTraditional => null,
        LangEnum.Khmer => null,
        LangEnum.NorwegianBokmal => "NB",
        LangEnum.NorwegianNynorsk => "NB",
        LangEnum.Persian => null,
        LangEnum.Swedish => "SV",
        LangEnum.Polish => "PL",
        LangEnum.Dutch => "NL",
        LangEnum.Ukrainian => null,
        _ => "auto"
    };

    public override void Init(IPluginContext context)
    {
        Context = context;
        Settings = context.LoadSettingStorage<Settings>();
    }

    public override void Dispose() => _viewModel?.Dispose();

    public override async Task TranslateAsync(TranslateRequest request, TranslateResult result, CancellationToken cancellationToken = default)
    {
        if (GetSourceLanguage(request.SourceLang) is not string sourceStr)
        {
            result.Fail(Context.GetTranslation("UnsupportedSourceLang"));
            return;
        }
        if (GetTargetLanguage(request.TargetLang) is not string targetStr)
        {
            result.Fail(Context.GetTranslation("UnsupportedTargetLang"));
            return;
        }

        var content = new
        {
            text = new[] { request.Text },
            source_lang = sourceStr,
            target_lang = targetStr,
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

        var url = Settings.UseProApi ? Constant.ProUrl : Constant.FreeUrl;
        var response = await Context.HttpService.PostAsync(url, content, option, cancellationToken);

        var jsonNode = JsonNode.Parse(response);
        var translatedText = jsonNode?["translations"]?[0]?["text"]?.ToString() ?? throw new Exception(response);

        result.Success(translatedText);
    }
}