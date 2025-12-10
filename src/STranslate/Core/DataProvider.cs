using iNKORE.UI.WPF.Modern;
using Serilog.Events;
using STranslate.Plugin;
using STranslate.ViewModels.Pages;

namespace STranslate.Core;

/// <summary>
/// 枚举数据提供者，提供下拉选项的数据绑定支持
/// </summary>
public class DataProvider
{
    public DataProvider(Internationalization i18n)
    {
        i18n.OnLanguageChanged += UpdateLanguage;
        UpdateLanguage();
    }

    /// <summary>
    /// 更新语言标签
    /// </summary>
    public void UpdateLanguage()
    {
        DropdownDataGeneric<LangEnum>.UpdateLabels(LangEnums);
        DropdownDataGeneric<ProxyType>.UpdateLabels(ProxyTypes);
        DropdownDataGeneric<LanguageDetectorType>.UpdateLabels(LanguageDetectors);
        DropdownDataGeneric<ElementTheme>.UpdateLabels(ColorSchemes);
        DropdownDataGeneric<LineBreakHandleType>.UpdateLabels(LineBreakHandleTypes);
        DropdownDataGeneric<PluginType>.UpdateLabels(PluginTypes);
        DropdownDataGeneric<LayoutAnalysisMode>.UpdateLabels(LayoutAnalysisModes);
        DropdownDataGeneric<WindowScreenType>.UpdateLabels(WindowScreenTypes);
        DropdownDataGeneric<WindowAlignType>.UpdateLabels(WindowAlignTypes);
        DropdownDataGeneric<StartMode>.UpdateLabels(StartModes);
        DropdownDataGeneric<LogEventLevel>.UpdateLabels(LogEventLevels);
        DropdownDataGeneric<OcrResultShowingType>.UpdateLabels(OcrResultShowingTypes);
        DropdownDataGeneric<HistoryLimit>.UpdateLabels(HistoryLimits);
        DropdownDataGeneric<CopyAfterTranslation>.UpdateLabels(CopyAfterTranslations);
        DropdownDataGeneric<BackupType>.UpdateLabels(BackupTypes);
        DropdownDataGeneric<ImageQuality>.UpdateLabels(ImageQualities);
    }

    #region LangEnums

    public class LangEnumData : DropdownDataGeneric<LangEnum> { }

    public List<LangEnumData> LangEnums { get; } =
        DropdownDataGeneric<LangEnum>.GetValues<LangEnumData>("LangEnum");

    #endregion

    #region ProxyTypes

    public class ProxyTypeData : DropdownDataGeneric<ProxyType> { }
    public List<ProxyTypeData> ProxyTypes { get; } =
        DropdownDataGeneric<ProxyType>.GetValues<ProxyTypeData>("ProxyType");

    #endregion

    #region LanguageDetectors

    public class LanguageDetectorData : DropdownDataGeneric<LanguageDetectorType> { }
    public List<LanguageDetectorData> LanguageDetectors { get; } =
        DropdownDataGeneric<LanguageDetectorType>.GetValues<LanguageDetectorData>("LanguageDetectorType");

    #endregion

    #region ColorSchemes

    public class ColorSchemeData : DropdownDataGeneric<ElementTheme> { }
    public List<ColorSchemeData> ColorSchemes { get; } =
        DropdownDataGeneric<ElementTheme>.GetValues<ColorSchemeData>("ColorScheme");

    #endregion

    #region LineBreakHandleTypes

    public class LineBreakHandleData : DropdownDataGeneric<LineBreakHandleType> { }
    public List<LineBreakHandleData> LineBreakHandleTypes { get; } =
        DropdownDataGeneric<LineBreakHandleType>.GetValues<LineBreakHandleData>("LineBreakHandleType");

    #endregion

    #region PluginTypes

    public class PluginTypeData : DropdownDataGeneric<PluginType> { }
    public List<PluginTypeData> PluginTypes { get; } =
        DropdownDataGeneric<PluginType>.GetValues<PluginTypeData>("PluginType");

    #endregion

    #region LayoutAnalysisMode

    public class LayoutAnalysisModeData : DropdownDataGeneric<LayoutAnalysisMode> { }
    public List<LayoutAnalysisModeData> LayoutAnalysisModes { get; } =
        DropdownDataGeneric<LayoutAnalysisMode>.GetValues<LayoutAnalysisModeData>("LayoutAnalysisMode");

    #endregion

    #region WindowScreenTypes

    public class WindowScreenTypeData : DropdownDataGeneric<WindowScreenType> { }
    public List<WindowScreenTypeData> WindowScreenTypes { get; } =
        DropdownDataGeneric<WindowScreenType>.GetValues<WindowScreenTypeData>("WindowScreenType");

    #endregion

    #region WindowAlignTypes

    public class WindowAlignTypeData : DropdownDataGeneric<WindowAlignType> { }
    public List<WindowAlignTypeData> WindowAlignTypes { get; } =
        DropdownDataGeneric<WindowAlignType>.GetValues<WindowAlignTypeData>("WindowAlignType");

    #endregion

    #region StartModes

    public class StartModeData : DropdownDataGeneric<StartMode> { }
    public List<StartModeData> StartModes { get; } =
        DropdownDataGeneric<StartMode>.GetValues<StartModeData>("StartMode");

    #endregion

    #region LogEventLevels

    public class LogEventLevelData : DropdownDataGeneric<LogEventLevel> { }
    public List<LogEventLevelData> LogEventLevels { get; } =
        DropdownDataGeneric<LogEventLevel>.GetValues<LogEventLevelData>("LogEventLevel");

    #endregion

    #region OcrResultShowingTypes

    public class OcrResultShowingTypeData : DropdownDataGeneric<OcrResultShowingType> { }
    public List<OcrResultShowingTypeData> OcrResultShowingTypes { get; } =
        DropdownDataGeneric<OcrResultShowingType>.GetValues<OcrResultShowingTypeData>("OcrResultShowingType");

    #endregion

    #region HistoryLimits

    public class HistoryLimitData : DropdownDataGeneric<HistoryLimit> { }
    public List<HistoryLimitData> HistoryLimits { get; } =
        DropdownDataGeneric<HistoryLimit>.GetValues<HistoryLimitData>("HistoryLimit");

    #endregion

    #region CopyAfterTranslations

    public class CopyAfterTranslationData : DropdownDataGeneric<CopyAfterTranslation> { }
    public List<CopyAfterTranslationData> CopyAfterTranslations { get; } =
        DropdownDataGeneric<CopyAfterTranslation>.GetValues<CopyAfterTranslationData>("CopyAfterTranslation");

    #endregion

    #region BackupTypes

    public class BackupTypeData : DropdownDataGeneric<BackupType> { }
    public List<BackupTypeData> BackupTypes { get; } =
        DropdownDataGeneric<BackupType>.GetValues<BackupTypeData>("BackupType");

    #endregion

    #region ImageQualities

    public class ImageQualityData : DropdownDataGeneric<ImageQuality> { }
    public List<ImageQualityData> ImageQualities { get; } =
        DropdownDataGeneric<ImageQuality>.GetValues<ImageQualityData>("ImageQuality");

    #endregion
}