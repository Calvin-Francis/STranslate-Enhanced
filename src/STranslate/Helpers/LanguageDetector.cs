using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using STranslate.Core;
using STranslate.Plugin;
using System.Text.Json;

namespace STranslate.Helpers;

/// <summary>
///     <see href="https://github.com/pot-app/pot-desktop/blob/master/src/utils/lang_detect.js" />
/// </summary>
public class LanguageDetector
{
    private static readonly ILogger<Utilities> _logger = Ioc.Default.GetRequiredService<ILogger<Utilities>>();
    private static readonly IHttpService _httpService = Ioc.Default.GetRequiredService<IHttpService>();
    private static readonly Internationalization _i18n = Ioc.Default.GetRequiredService<Internationalization>();
    private static readonly Settings _settings = Ioc.Default.GetRequiredService<Settings>();

    public static async Task<(bool isSuccess, LangEnum source, LangEnum target)> GetLanguageAsync(
        string content,
        CancellationToken cancellationToken = default,
        Action? onStarted = default,
        Action<bool, LangEnum>? onCompleted = default,
        Action? onFinished = default)
    {
        bool isSuccess = true;
        var source = _settings.SourceLang;
        if (_settings.SourceLang == LangEnum.Auto)
        {
            onStarted?.Invoke();
            try
            {
                var detected = await DetectAsync(content, _settings.LanguageDetector, token: cancellationToken).ConfigureAwait(false);
                isSuccess = detected != LangEnum.Auto;
                source = isSuccess ? detected : _settings.SourceLangIfAuto;

                onCompleted?.Invoke(isSuccess, source);
            }
            finally
            {
                onFinished?.Invoke();
            }
        }
        return (isSuccess, source, GetTargetLanguage(source));
    }

    private static LangEnum GetTargetLanguage(LangEnum source)
    {
        if (_settings.TargetLang != LangEnum.Auto)
            return _settings.TargetLang;

        return (source == _settings.FirstLanguage || source == LangEnum.ChineseSimplified || source == LangEnum.ChineseTraditional)
            ? _settings.SecondLanguage
            : _settings.FirstLanguage;
    }

    /// <summary>
    ///     识别语种
    /// </summary>
    /// <param name="text"></param>
    /// <param name="type"></param>
    /// <param name="rate"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private static async Task<LangEnum> DetectAsync(string text, LanguageDetectorType type = LanguageDetectorType.Local,
        double rate = 0.8, CancellationToken token = default)
    {
        return type switch
        {
            LanguageDetectorType.Local => LocalLangDetect(text, rate),
            LanguageDetectorType.Baidu => await BaiduLangDetectAsync(text, token).ConfigureAwait(false),
            LanguageDetectorType.Tencent => await TencentLangDetectAsync(text, token).ConfigureAwait(false),
            LanguageDetectorType.Niutrans => await NiutransLangDetectAsync(text, token).ConfigureAwait(false),
            LanguageDetectorType.Bing => await BingLangDetectAsync(text, token).ConfigureAwait(false),
            LanguageDetectorType.Yandex => await YandexLangDetectAsync(text, token).ConfigureAwait(false),
            LanguageDetectorType.Google => await GoogleLangDetectAsync(text, token).ConfigureAwait(false),
            LanguageDetectorType.Microsoft => await MicrosoftLangDetectAsync(text, token).ConfigureAwait(false),
            _ => LangEnum.Auto
        };
    }

    /// <summary>
    ///     本地识别
    ///     仅能识别 <see cref="LangEnum.ChineseSimplified"/>、<see cref="LangEnum.English"/>
    /// </summary>
    /// <param name="text"></param>
    /// <param name="rate"></param>
    /// <returns></returns>
    private static LangEnum LocalLangDetect(string text, double rate) => Utilities.AutomaticLanguageRecognition(text, rate).SourceLang;

    /// <summary>
    ///     百度识别
    /// </summary>
    /// <param name="text"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private static async Task<LangEnum> BaiduLangDetectAsync(string text, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return LangEnum.Auto;

        try
        {
            const string url = "https://fanyi.baidu.com/langdetect";
            var formData = new Dictionary<string, string> { { "query", text } };
            var resp = await _httpService.PostFormAsync(url, formData, cancellationToken: token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(resp))
                return LangEnum.Auto;

            using var doc = JsonDocument.Parse(resp);
            var lan = doc.RootElement.TryGetProperty("lan", out var langElem)
                ? langElem.GetString() ?? ""
                : "";

            return lan switch
            {
                "zh" => LangEnum.ChineseSimplified,
                "cht" => LangEnum.ChineseTraditional,
                "en" => LangEnum.English,
                "jp" => LangEnum.Japanese,
                "kor" => LangEnum.Korean,
                "fra" => LangEnum.French,
                "spa" => LangEnum.Spanish,
                "ru" => LangEnum.Russian,
                "de" => LangEnum.German,
                "it" => LangEnum.Italian,
                "tr" => LangEnum.Turkish,
                "pt" => LangEnum.PortuguesePortugal,
                "vie" => LangEnum.Vietnamese,
                "id" => LangEnum.Indonesian,
                "th" => LangEnum.Thai,
                "may" => LangEnum.Malay,
                "ar" => LangEnum.Arabic,
                "hi" => LangEnum.Hindi,
                "nob" => LangEnum.NorwegianBokmal,
                "nno" => LangEnum.NorwegianNynorsk,
                "per" => LangEnum.Persian,
                "ukr" => LangEnum.Ukrainian,
                _ => LangEnum.Auto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _i18n.GetTranslation("BaiduDetectError"));
            return LangEnum.Auto;
        }
    }

    /// <summary>
    ///     腾讯识别
    /// </summary>
    /// <param name="text"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private static async Task<LangEnum> TencentLangDetectAsync(string text, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return LangEnum.Auto;

        try
        {
            const string url = "https://transmart.qq.com/api/imt";
            var headers = new Dictionary<string, string>
            {
                { "User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36" },
                { "Referer", "https://yi.qq.com/zh-CN/index" }
            };

            var reqData = new
            {
                header = new
                {
                    fn = "lang_detect",
                    client_key = "browser-chrome-110.0.0-Mac OS-df4bd4c5-a65d-44b2-a40f-42f34f3535f2-1677486696487"
                },
                text = text,
            };
            var option = new Options { Headers = headers };
            var resp = await _httpService.PostAsync(url, reqData, option, token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(resp))
                return LangEnum.Auto;

            using var doc = JsonDocument.Parse(resp);
            var lan = doc.RootElement.TryGetProperty("language", out var langElem)
                ? langElem.GetString() ?? ""
                : "";

            return lan switch
            {
                "chinese" => LangEnum.ChineseSimplified,
                "cantonese" => LangEnum.Cantonese,
                "english" => LangEnum.English,
                "japanese" => LangEnum.Japanese,
                "korean" => LangEnum.Korean,
                "french" => LangEnum.French,
                "spanish" => LangEnum.Spanish,
                "russian" => LangEnum.Russian,
                "german" => LangEnum.German,
                "italian" => LangEnum.Italian,
                "turkish" => LangEnum.Turkish,
                "portuguese" => LangEnum.PortuguesePortugal,
                "vietnamese" => LangEnum.Vietnamese,
                "thai" => LangEnum.Thai,
                "arabic" => LangEnum.Arabic,
                _ => LangEnum.Auto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _i18n.GetTranslation("TencentDetectError"));
            return LangEnum.Auto;
        }
    }

    /// <summary>
    ///     小牛识别
    /// </summary>
    /// <param name="text"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private static async Task<LangEnum> NiutransLangDetectAsync(string text, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return LangEnum.Auto;

        try
        {
            const string url = "https://test.niutrans.com/NiuTransServer/language";
            var option = new Options
            {
                QueryParams = new Dictionary<string, string>
                {
                    { "src_text", text },
                    { "source", "text" },
                    { "time", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() }
                }
            };
            var resp = await _httpService.GetAsync(url, option, token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(resp))
                return LangEnum.Auto;

            using var doc = JsonDocument.Parse(resp);
            var lan = doc.RootElement.TryGetProperty("language", out var langElem)
                ? langElem.GetString() ?? ""
                : "";

            return lan switch
            {
                "zh" => LangEnum.ChineseSimplified,
                "cht" => LangEnum.ChineseTraditional,
                "en" => LangEnum.English,
                "ja" => LangEnum.Japanese,
                "ko" => LangEnum.Korean,
                "fr" => LangEnum.French,
                "es" => LangEnum.Spanish,
                "ru" => LangEnum.Russian,
                "de" => LangEnum.German,
                "it" => LangEnum.Italian,
                "tr" => LangEnum.Turkish,
                "pt" => LangEnum.PortuguesePortugal,
                "vi" => LangEnum.Vietnamese,
                "id" => LangEnum.Indonesian,
                "th" => LangEnum.Thai,
                "ms" => LangEnum.Malay,
                "ar" => LangEnum.Arabic,
                "hi" => LangEnum.Hindi,
                "mn" => LangEnum.MongolianCyrillic,
                "mo" => LangEnum.MongolianTraditional,
                "km" => LangEnum.Khmer,
                "nb" => LangEnum.NorwegianBokmal,
                "nn" => LangEnum.NorwegianNynorsk,
                "fa" => LangEnum.Persian,
                "uk" => LangEnum.Ukrainian,
                _ => LangEnum.Auto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _i18n.GetTranslation("NiutransDetectError"));
            return LangEnum.Auto;
        }
    }

    /// <summary>
    ///     必应识别
    /// </summary>
    /// <param name="text"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private static async Task<LangEnum> BingLangDetectAsync(string text, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return LangEnum.Auto;

        try
        {
            const string tokenUrl = "https://edge.microsoft.com/translate/auth";
            var getToken = await _httpService.GetAsync(url: tokenUrl, cancellationToken: token).ConfigureAwait(false);

            const string url = "https://api-edge.cognitive.microsofttranslator.com/detect";
            var headers = new Dictionary<string, string>
            {
                { "accept", "*/*" },
                { "accept-language", "zh-TW,zh;q=0.9,ja;q=0.8,zh-CN;q=0.7,en-US;q=0.6,en;q=0.5" },
                { "authorization", "Bearer " + getToken },
                { "cache-control", "no-cache" },
                { "pragma", "no-cache" },
                { "sec-ch-ua", "\"Microsoft Edge\";v=\"113\", \"Chromium\";v=\"113\", \"Not-A.Brand\";v=\"24\"" },
                { "sec-ch-ua-mobile", "?0" },
                { "sec-ch-ua-platform", "\"Windows\"" },
                { "sec-fetch-dest", "empty" },
                { "sec-fetch-mode", "cors" },
                { "sec-fetch-site", "cross-site" },
                { "Referer", "https://appsumo.com/" },
                { "Referrer-Policy", "strict-origin-when-cross-origin" },
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36 Edg/113.0.1774.42"
                }
            };

            var queryParams = new Dictionary<string, string> { { "api-version", "3.0" } };
            var content = new[] { new { Text = text } };
            var option = new Options
            {
                QueryParams = queryParams,
                Headers = headers,
            };

            var resp = await _httpService.PostAsync(url, content, option, token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(resp))
                return LangEnum.Auto;

            using var doc = JsonDocument.Parse(resp);
            string lan = "";
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                var first = doc.RootElement[0];
                if (first.TryGetProperty("language", out var langElem))
                {
                    lan = langElem.GetString() ?? "";
                }
            }

            return lan switch
            {
                "zh-Hans" => LangEnum.ChineseSimplified,
                "zh-Hant" => LangEnum.ChineseTraditional,
                "en" => LangEnum.English,
                "ja" => LangEnum.Japanese,
                "ko" => LangEnum.Korean,
                "fr" => LangEnum.French,
                "es" => LangEnum.Spanish,
                "ru" => LangEnum.Russian,
                "de" => LangEnum.German,
                "it" => LangEnum.Italian,
                "tr" => LangEnum.Turkish,
                "pt-pt" => LangEnum.PortuguesePortugal,
                "pt" => LangEnum.PortugueseBrazil,
                "vi" => LangEnum.Vietnamese,
                "id" => LangEnum.Indonesian,
                "th" => LangEnum.Thai,
                "ms" => LangEnum.Malay,
                "ar" => LangEnum.Arabic,
                "hi" => LangEnum.Hindi,
                "mn-Cyrl" => LangEnum.MongolianCyrillic,
                "mn-Mong" => LangEnum.MongolianTraditional,
                "km" => LangEnum.Khmer,
                "nb" => LangEnum.NorwegianBokmal,
                "fa" => LangEnum.Persian,
                "uk" => LangEnum.Ukrainian,
                _ => LangEnum.Auto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _i18n.GetTranslation("BingDetectError"));
            return LangEnum.Auto;
        }
    }

    /// <summary>
    ///     Yandex识别
    /// </summary>
    /// <param name="text"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private static async Task<LangEnum> YandexLangDetectAsync(string text, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return LangEnum.Auto;

        try
        {
            const string url = "https://translate.yandex.net/api/v1/tr.json/detect";
            var option = new Options
            {
                QueryParams = new Dictionary<string, string>
                {
                    { "id", Guid.NewGuid().ToString("N") + "-0-0" },
                    { "srv", "android" },
                    { "text", text }
                }
            };
            var resp = await _httpService.GetAsync(url, option, token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(resp))
                return LangEnum.Auto;

            using var doc = JsonDocument.Parse(resp);
            var lan = doc.RootElement.TryGetProperty("lang", out var langElem)
                ? langElem.GetString() ?? ""
                : "";

            return lan switch
            {
                "zh" => LangEnum.ChineseSimplified,
                "en" => LangEnum.English,
                "ja" => LangEnum.Japanese,
                "ko" => LangEnum.Korean,
                "fr" => LangEnum.French,
                "es" => LangEnum.Spanish,
                "ru" => LangEnum.Russian,
                "de" => LangEnum.German,
                "it" => LangEnum.Italian,
                "tr" => LangEnum.Turkish,
                "pt" => LangEnum.PortuguesePortugal,
                "vi" => LangEnum.Vietnamese,
                "id" => LangEnum.Indonesian,
                "th" => LangEnum.Thai,
                "ms" => LangEnum.Malay,
                "ar" => LangEnum.Arabic,
                "hi" => LangEnum.Hindi,
                "no" => LangEnum.NorwegianBokmal,
                "fa" => LangEnum.Persian,
                "uk" => LangEnum.Ukrainian,
                _ => LangEnum.Auto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _i18n.GetTranslation("YandexDetectError"));
            return LangEnum.Auto;
        }
    }

    /// <summary>
    ///     谷歌识别
    /// </summary>
    /// <param name="text"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private static async Task<LangEnum> GoogleLangDetectAsync(string text, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return LangEnum.Auto;

        try
        {
            const string url =
                "https://translate.google.com/translate_a/single?dt=at&dt=bd&dt=ex&dt=ld&dt=md&dt=qca&dt=rw&dt=rm&dt=ss&dt=t";

            var option = new Options
            {
                QueryParams = new Dictionary<string, string>
                {
                    { "client", "gtx" },
                    { "sl", "auto" },
                    { "tl", "zh-CN" },
                    { "hl", "zh-CN" },
                    { "ie", "UTF-8" },
                    { "oe", "UTF-8" },
                    { "otf", "1" },
                    { "ssel", "0" },
                    { "tsel", "0" },
                    { "kc", "7" },
                    { "q", text }
                }
            };

            var resp = await _httpService.GetAsync(url, option, token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(resp))
                return LangEnum.Auto;

            using var doc = JsonDocument.Parse(resp);
            var root = doc.RootElement;
            string lan = root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 2
                ? root[2].ToString()
                : "";

            return lan switch
            {
                "zh-CN" => LangEnum.ChineseSimplified,
                "zh-TW" => LangEnum.ChineseTraditional,
                "ja" => LangEnum.Japanese,
                "en" => LangEnum.English,
                "ko" => LangEnum.Korean,
                "fr" => LangEnum.French,
                "es" => LangEnum.Spanish,
                "ru" => LangEnum.Russian,
                "de" => LangEnum.German,
                "it" => LangEnum.Italian,
                "tr" => LangEnum.Turkish,
                "pt" => LangEnum.PortuguesePortugal,
                "vi" => LangEnum.Vietnamese,
                "id" => LangEnum.Indonesian,
                "th" => LangEnum.Thai,
                "ms" => LangEnum.Malay,
                "ar" => LangEnum.Arabic,
                "hi" => LangEnum.Hindi,
                "mn" => LangEnum.MongolianCyrillic,
                "km" => LangEnum.Khmer,
                "fa" => LangEnum.Persian,
                "no" => LangEnum.NorwegianBokmal,
                "uk" => LangEnum.Ukrainian,
                _ => LangEnum.Auto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _i18n.GetTranslation("GoogleDetectError"));
            return LangEnum.Auto;
        }
    }

    /// <summary>
    ///     微软识别
    ///     <see href="https://learn.microsoft.com/zh-cn/azure/ai-services/translator/reference/v3-0-detect"/>
    /// </summary>
    /// <param name="text"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private static async Task<LangEnum> MicrosoftLangDetectAsync(string text, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return LangEnum.Auto;

        try
        {
            const string endpoint = "api.cognitive.microsofttranslator.com/detect?api-version=3.0";
            const string url = $"https://{endpoint}";
            var content = new[] { new { Text = text } };
            var option = new Options
            {
                Headers = new Dictionary<string, string>
                {
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36" },
                    { "X-MT-Signature", Utilities.GetSignature(endpoint) }
                }
            };

            var resp = await _httpService.PostAsync(url, content, option, token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(resp))
                throw new Exception("请求结果为空");

            using var doc = JsonDocument.Parse(resp);
            string lan = doc.RootElement.ValueKind == JsonValueKind.Array
                         && doc.RootElement.GetArrayLength() > 0
                         && doc.RootElement[0].TryGetProperty("language", out var langElem)
                ? langElem.GetString() ?? ""
                : "";

            return lan switch
            {
                "zh-Hans" => LangEnum.ChineseSimplified,
                "zh-Hant" => LangEnum.ChineseTraditional,
                "en" => LangEnum.English,
                "ja" => LangEnum.Japanese,
                "ko" => LangEnum.Korean,
                "fr" => LangEnum.French,
                "es" => LangEnum.Spanish,
                "ru" => LangEnum.Russian,
                "de" => LangEnum.German,
                "it" => LangEnum.Italian,
                "tr" => LangEnum.Turkish,
                "pt" => LangEnum.PortugueseBrazil,
                "vi" => LangEnum.Vietnamese,
                "id" => LangEnum.Indonesian,
                "th" => LangEnum.Thai,
                "ms" => LangEnum.Malay,
                "ar" => LangEnum.Arabic,
                "mn-Cyrl" => LangEnum.MongolianCyrillic,
                "km" => LangEnum.Khmer,
                "nb" => LangEnum.NorwegianBokmal,
                "fa" => LangEnum.Persian,
                "sv" => LangEnum.Swedish,
                "pl" => LangEnum.Polish,
                "nl" => LangEnum.Dutch,
                "uk" => LangEnum.Ukrainian,
                _ => LangEnum.Auto
            };
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (ex.InnerException is Exception innEx)
            {
                try
                {
                    using var doc = JsonDocument.Parse(innEx.Message);
                    msg += $" {doc.RootElement.GetProperty("error").GetString()}";
                }
                catch
                {
                    // 忽略JSON解析异常
                }
            }

            msg = msg.Trim();
            _logger.LogError(ex, _i18n.GetTranslation("MicrosoftDetectError") + ": {Message}", msg);
            return LangEnum.Auto;
        }
    }
}