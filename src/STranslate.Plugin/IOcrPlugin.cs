namespace STranslate.Plugin;

/// <summary>
/// OCR 插件接口，定义了支持的语言和识别方法。
/// </summary>
public interface IOcrPlugin : IPlugin
{
    /// <summary>
    /// 获取插件支持的语言列表。
    /// </summary>
    IEnumerable<LangEnum> SupportedLanguages { get; }

    /// <summary>
    /// 异步识别图片中的文本。
    /// </summary>
    /// <param name="request">OCR 请求参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>识别结果。</returns>
    Task<OcrResult> RecognizeAsync(OcrRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// OCR 请求参数，包含待识别图片数据和目标语言。
/// </summary>
/// <param name="ImageData">图片数据</param>
/// <param name="Language">语言</param>
public record OcrRequest(byte[] ImageData, LangEnum Language);

/// <summary>
/// OCR 识别结果，包含识别出的文本、内容列表、语言、耗时、成功标志及错误信息。
/// </summary>
public class OcrResult
{
    /// <summary>
    /// 纯文本结果
    /// </summary>
    public string Text => string.Join(Environment.NewLine, OcrContents.Select(x => x.Text).ToArray()).Trim();
    /// <summary>
    /// 识别出的内容列表。
    /// </summary>
    public List<OcrContent> OcrContents { get; set; } = [];
    /// <summary>
    /// 识别出的语言。
    /// </summary>
    public string Language { get; set; } = string.Empty;
    /// <summary>
    /// 识别耗时。
    /// </summary>
    public TimeSpan Duration { get; set; }
    /// <summary>
    /// 是否识别成功。
    /// </summary>
    public bool IsSuccess { get; set; } = true;
    /// <summary>
    /// 错误信息（如有）。
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 失败
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public OcrResult Fail(string msg)
    {
        IsSuccess = false;
        ErrorMessage = msg;
        return this;
    }
}

/// <summary>
/// OCR 内容，包含识别出的文本及其对应的包围盒坐标点。
/// </summary>
public class OcrContent
{
    /// <summary>
    /// 识别出的文本内容。
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 文本对应的包围盒坐标点集合。
    /// </summary>
    public List<BoxPoint> BoxPoints { get; set; } = [];
}

/// <summary>
/// 表示一个二维坐标点，用于描述 OCR 识别内容的包围盒顶点。
/// </summary>
public class BoxPoint(float x, float y)
{
    /// <summary>
    /// X 坐标值。
    /// </summary>
    public float X { get; set; } = x;

    /// <summary>
    /// Y 坐标值。
    /// </summary>
    public float Y { get; set; } = y;
}