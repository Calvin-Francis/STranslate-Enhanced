namespace STranslate.Plugin.Translate.DeepL;

public class Settings
{
    public string ApiKey { get; set; } = string.Empty;

    public bool UseProApi { get; set; } = false;

    public string UsageStr { get; set; } = string.Empty;
    public double Usage { get; set; }
}