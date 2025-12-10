using STranslate.Plugin.Translate.ICibaBuiltIn.View;
using STranslate.Plugin.Translate.ICibaBuiltIn.ViewModel;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Controls;

namespace STranslate.Plugin.Translate.ICibaBuiltIn;

public class Main : DictionaryPluginBase
{
    private const string URL = "http://dict-co.iciba.com/api/dictionary.php";

    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;

    public override Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel();
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    public override void Init(IPluginContext context)
    {
        Context = context;
        Settings = context.LoadSettingStorage<Settings>();
    }

    public override void Dispose() { }

    public override async Task TranslateAsync(string content, DictionaryResult result, CancellationToken cancellationToken = default)
    {
        var option = new Options
        {
            QueryParams = new Dictionary<string, string>
            {
                { "type", "json" },
                { "w", content.ToLower() },
                { "key", "54A9DE969E911BC5294B70DA8ED5C9C4" }
            }
        };

        var response = await Context.HttpService.GetAsync(URL, option, cancellationToken);

        var jsonDoc = JsonDocument.Parse(response);
        var root = jsonDoc.RootElement;

        // 检查 word_name 字段是否存在且不为空
        if (
            !root.TryGetProperty("word_name", out var wordName) ||
            wordName.GetString() is not string word ||
            string.IsNullOrEmpty(word))
        {
            result.ResultType = DictionaryResultType.NoResult;
            return;
        }

        result.Text = word;
        result.ResultType = DictionaryResultType.Success;

        // 检查 symbols 数组是否存在且不为空
        if (!root.TryGetProperty("symbols", out var symbols) || symbols.GetArrayLength() == 0)
            return;

        var firstSymbol = symbols[0];
        bool isChinese = Util.IsChinese(content);

        if (isChinese)
        {
            // 处理中文内容
            ProcessChineseContent(firstSymbol, result);
        }
        else
        {
            // 处理英文内容
            ProcessEnglishContent(firstSymbol, result);
            // 只有英文才处理词汇变形
            ProcessWordExchange(root, result);
        }
    }

    /// <summary>
    /// 处理中文内容的音标和释义
    /// </summary>
    /// <param name="firstSymbol">符号元素</param>
    /// <param name="result">字典结果</param>
    private void ProcessChineseContent(JsonElement firstSymbol, DictionaryResult result)
    {
        // 中文拼音
        if (firstSymbol.TryGetProperty("word_symbol", out var phZh))
        {
            var symbolZh = new Symbol
            {
                Label = "zh",
                Phonetic = phZh.GetString() ?? string.Empty,
                AudioUrl = firstSymbol.TryGetProperty("symbol_mp3", out var phZhMp3)
                    ? phZhMp3.GetString() ?? string.Empty
                    : string.Empty
            };
            if (!string.IsNullOrWhiteSpace(symbolZh.Phonetic))
                result.Symbols.Add(symbolZh);
        }

        // 处理中文释义（结构与英文不同）
        if (!firstSymbol.TryGetProperty("parts", out var parts))
            return;
        foreach (var part in parts.EnumerateArray())
        {
            if (!part.TryGetProperty("means", out var means))
                continue;

            // 分别收集有效释义和无效释义
            var validMeans = new List<string>();
            var invalidMeans = new List<string>();

            foreach (var mean in means.EnumerateArray())
            {
                if (!mean.TryGetProperty("word_mean", out var wordMean))
                    continue;

                var meaning = wordMean.GetString();
                if (string.IsNullOrEmpty(meaning))
                    continue;

                // 根据 has_mean 字段判断分类
                if (mean.TryGetProperty("has_mean", out var hasMean))
                {
                    var isValid = GetHasMeanValue(hasMean);
                    if (isValid.HasValue)
                    {
                        if (isValid.Value)
                        {
                            validMeans.Add(meaning);
                        }
                        else
                        {
                            invalidMeans.Add(meaning);
                        }
                    }
                    // 如果 has_mean 存在但值无效，跳过该条目
                }
                else
                {
                    // 如果没有 has_mean 字段，默认归为有效释义
                    validMeans.Add(meaning);
                }
            }

            // 添加有效释义
            if (validMeans.Count > 0)
            {
                var validDictMean = new DictMean
                {
                    PartOfSpeech = Context.GetTranslation("Paraphrase"),
                    Means = new ObservableCollection<string>(validMeans)
                };
                result.DictMeans.Add(validDictMean);
            }

            // 添加扩展释义（如电影名等）
            if (invalidMeans.Count > 0)
            {
                var invalidDictMean = new DictMean
                {
                    PartOfSpeech = Context.GetTranslation("Expand"),
                    Means = new ObservableCollection<string>(invalidMeans)
                };
                result.DictMeans.Add(invalidDictMean);
            }
        }
    }

    /// <summary>
    /// 处理英文内容的音标和释义
    /// </summary>
    /// <param name="firstSymbol">符号元素</param>
    /// <param name="result">字典结果</param>
    private void ProcessEnglishContent(JsonElement firstSymbol, DictionaryResult result)
    {
        // 英式发音
        if (firstSymbol.TryGetProperty("ph_en", out var phEn) &&
            !string.IsNullOrEmpty(phEn.GetString()))
        {
            var symbolEn = new Symbol
            {
                Label = "uk",
                Phonetic = phEn.GetString() ?? string.Empty,
                AudioUrl = firstSymbol.TryGetProperty("ph_en_mp3", out var phEnMp3)
                    ? phEnMp3.GetString() ?? string.Empty
                    : string.Empty
            };
            result.Symbols.Add(symbolEn);
        }

        // 美式发音
        if (firstSymbol.TryGetProperty("ph_am", out var phAm) &&
            !string.IsNullOrEmpty(phAm.GetString()))
        {
            var symbolAm = new Symbol
            {
                Label = "us",
                Phonetic = phAm.GetString() ?? string.Empty,
                AudioUrl = firstSymbol.TryGetProperty("ph_am_mp3", out var phAmMp3)
                    ? phAmMp3.GetString() ?? string.Empty
                    : string.Empty
            };
            result.Symbols.Add(symbolAm);
        }

        // 英文词性和释义
        if (firstSymbol.TryGetProperty("parts", out var parts))
        {
            foreach (var part in parts.EnumerateArray())
            {
                if (!part.TryGetProperty("part", out var partOfSpeech) ||
                    !part.TryGetProperty("means", out var means))
                    continue;

                var dictMean = new DictMean
                {
                    PartOfSpeech = partOfSpeech.GetString() ?? string.Empty,
                    Means = new ObservableCollection<string>(
                        means.EnumerateArray()
                            .Select(m => m.GetString())
                            .Where(m => !string.IsNullOrEmpty(m))
                            .Cast<string>()
                    )
                };

                result.DictMeans.Add(dictMean);
            }
        }
    }

    /// <summary>
    /// 处理词汇变形信息
    /// </summary>
    /// <param name="root">JSON根元素</param>
    /// <param name="result">字典结果</param>
    private void ProcessWordExchange(JsonElement root, DictionaryResult result)
    {
        if (!root.TryGetProperty("exchange", out var exchange))
            return;

        // 复数形式
        if (exchange.TryGetProperty("word_pl", out var wordPl))
        {
            AddWordForms(wordPl, result.Plurals);
        }

        // 过去式
        if (exchange.TryGetProperty("word_past", out var wordPast))
        {
            AddWordForms(wordPast, result.PastTense);
        }

        // 过去分词
        if (exchange.TryGetProperty("word_done", out var wordDone))
        {
            AddWordForms(wordDone, result.PastParticiple);
        }

        // 现在分词/动名词
        if (exchange.TryGetProperty("word_ing", out var wordIng))
        {
            AddWordForms(wordIng, result.PresentParticiple);
        }

        // 第三人称单数
        if (exchange.TryGetProperty("word_third", out var wordThird))
        {
            AddWordForms(wordThird, result.ThirdPersonSingular);
        }

        // 比较级
        if (exchange.TryGetProperty("word_er", out var wordEr))
        {
            AddWordForms(wordEr, result.Comparative);
        }

        // 最高级
        if (exchange.TryGetProperty("word_est", out var wordEst))
        {
            AddWordForms(wordEst, result.Superlative);
        }
    }

    /// <summary>
    /// 添加词汇变形到指定集合中
    /// </summary>
    /// <param name="jsonElement">JSON元素</param>
    /// <param name="collection">目标集合</param>
    private static void AddWordForms(JsonElement jsonElement, ObservableCollection<string> collection)
    {
        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in jsonElement.EnumerateArray())
                {
                    var word = item.GetString();
                    if (!string.IsNullOrEmpty(word))
                    {
                        collection.Add(word);
                    }
                }
                break;
            case JsonValueKind.String:
                var singleWord = jsonElement.GetString();
                if (!string.IsNullOrEmpty(singleWord))
                {
                    collection.Add(singleWord);
                }
                break;
        }
    }

    /// <summary>
    /// 解析 has_mean 字段的值
    /// </summary>
    /// <param name="hasMean">has_mean 的 JsonElement</param>
    /// <returns>如果是有效值返回 bool，如果是无效值返回 null</returns>
    private static bool? GetHasMeanValue(JsonElement hasMean)
    {
        return hasMean.ValueKind switch
        {
            JsonValueKind.String => hasMean.GetString() switch
            {
                "1" => true,
                "0" => false,
                _ => null
            },
            JsonValueKind.Number => hasMean.GetInt32() switch
            {
                1 => true,
                0 => false,
                _ => null
            },
            _ => null
        };
    }
}