using STranslate.Plugin.Translate.MicrosoftBuiltIn.View;
using STranslate.Plugin.Translate.MicrosoftBuiltIn.ViewModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Windows.Controls;

namespace STranslate.Plugin.Translate.MicrosoftBuiltIn;

public class Main : TranslatePluginBase
{
    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;

    private const string Url = "https://st-ms.deno.dev/translate";
    private const string ApiEndpoint = "api.cognitive.microsofttranslator.com";
    private const string ApiVersion = "3.0";
    private const int MaxTextLength = 1000;

    public override Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel();
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    /// <summary>
    ///     https://learn.microsoft.com/zh-cn/azure/ai-services/translator/language-support
    /// </summary>
    /// <param name="lang"></param>
    /// <returns></returns>
    public override string? GetSourceLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.Auto => "auto",
        LangEnum.ChineseSimplified => "zh-Hans",
        LangEnum.ChineseTraditional => "zh-Hant",
        LangEnum.Cantonese => null,
        LangEnum.English => "en",
        LangEnum.Japanese => "ja",
        LangEnum.Korean => "ko",
        LangEnum.French => "fr",
        LangEnum.Spanish => "es",
        LangEnum.Russian => "ru",
        LangEnum.German => "de",
        LangEnum.Italian => "it",
        LangEnum.Turkish => "tr",
        LangEnum.PortuguesePortugal => "pt-pt",
        LangEnum.PortugueseBrazil => "pt",
        LangEnum.Vietnamese => "vi",
        LangEnum.Indonesian => "id",
        LangEnum.Thai => "th",
        LangEnum.Malay => "ms",
        LangEnum.Arabic => "ar",
        LangEnum.Hindi => null,
        LangEnum.MongolianCyrillic => "mn-Cyrl",
        LangEnum.MongolianTraditional => "mn-Mong",
        LangEnum.Khmer => "km",
        LangEnum.NorwegianBokmal => "nb",
        LangEnum.NorwegianNynorsk => "nb",
        LangEnum.Persian => "fa",
        LangEnum.Swedish => "sv",
        LangEnum.Polish => "pl",
        LangEnum.Dutch => "nl",
        LangEnum.Ukrainian => "uk",
        _ => "auto"
    };

    public override string? GetTargetLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.Auto => "auto",
        LangEnum.ChineseSimplified => "zh-Hans",
        LangEnum.ChineseTraditional => "zh-Hant",
        LangEnum.Cantonese => null,
        LangEnum.English => "en",
        LangEnum.Japanese => "ja",
        LangEnum.Korean => "ko",
        LangEnum.French => "fr",
        LangEnum.Spanish => "es",
        LangEnum.Russian => "ru",
        LangEnum.German => "de",
        LangEnum.Italian => "it",
        LangEnum.Turkish => "tr",
        LangEnum.PortuguesePortugal => "pt-pt",
        LangEnum.PortugueseBrazil => "pt",
        LangEnum.Vietnamese => "vi",
        LangEnum.Indonesian => "id",
        LangEnum.Thai => "th",
        LangEnum.Malay => "ms",
        LangEnum.Arabic => "ar",
        LangEnum.Hindi => null,
        LangEnum.MongolianCyrillic => "mn-Cyrl",
        LangEnum.MongolianTraditional => "mn-Mong",
        LangEnum.Khmer => "km",
        LangEnum.NorwegianBokmal => "nb",
        LangEnum.NorwegianNynorsk => "nb",
        LangEnum.Persian => "fa",
        LangEnum.Swedish => "sv",
        LangEnum.Polish => "pl",
        LangEnum.Dutch => "nl",
        LangEnum.Ukrainian => "uk",
        _ => "auto"
    };

    public override void Init(IPluginContext context)
    {
        Context = context;
        Settings = context.LoadSettingStorage<Settings>();
    }

    public override void Dispose() { }

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


        string url = $"{ApiEndpoint}/translate?api-version={ApiVersion}&to={targetStr}";
        if (!string.IsNullOrEmpty(sourceStr))
        {
            url += $"&from={sourceStr}";
        }

        var content = new[] { new { request.Text } };
        var options = new Options
        {
            Headers = new Dictionary<string, string>
            {
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36" },
                { "X-MT-Signature", GetSignature(url) }
            }
        };

        var response = await Context.HttpService.PostAsync($"https://{url}", content, options, cancellationToken);
        var rootNode = JsonNode.Parse(response);
        if (rootNode is JsonArray arr && arr.Count > 0)
        {
            var translations = arr[0]?["translations"] as JsonArray;
            var data = translations?[0]?["text"]?.ToString() ?? throw new Exception($"No result.\nRaw: {response}");
            result.Success(data);
        }
        else
        {
            throw new Exception($"No result.\nRaw: {response}");
        }
    }

    /// <summary>
    ///     https://github.com/d4n3436/GTranslate/blob/master/src/GTranslate/Translators/MicrosoftTranslator.cs
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static string GetSignature(string url)
    {
        string guid = Guid.NewGuid().ToString("N");
        string escapedUrl = Uri.EscapeDataString(url);
        string dateTime = DateTimeOffset.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ssG\\MT", CultureInfo.InvariantCulture);

        byte[] bytes = Encoding.UTF8.GetBytes($"MSTranslatorAndroidApp{escapedUrl}{dateTime}{guid}".ToLowerInvariant());

        using var hmac = new HMACSHA256(PrivateKey);
        byte[] hash = hmac.ComputeHash(bytes);

        return $"MSTranslatorAndroidApp::{Convert.ToBase64String(hash)}::{dateTime}::{guid}";
    }

    private static readonly byte[] PrivateKey =
    [
        0xa2, 0x29, 0x3a, 0x3d, 0xd0, 0xdd, 0x32, 0x73,
        0x97, 0x7a, 0x64, 0xdb, 0xc2, 0xf3, 0x27, 0xf5,
        0xd7, 0xbf, 0x87, 0xd9, 0x45, 0x9d, 0xf0, 0x5a,
        0x09, 0x66, 0xc6, 0x30, 0xc6, 0x6a, 0xaa, 0x84,
        0x9a, 0x41, 0xaa, 0x94, 0x3a, 0xa8, 0xd5, 0x1a,
        0x6e, 0x4d, 0xaa, 0xc9, 0xa3, 0x70, 0x12, 0x35,
        0xc7, 0xeb, 0x12, 0xf6, 0xe8, 0x23, 0x07, 0x9e,
        0x47, 0x10, 0x95, 0x91, 0x88, 0x55, 0xd8, 0x17
    ];
}