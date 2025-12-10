using CommunityToolkit.Mvvm.Input;
using STranslate.Core;
using System.Windows.Controls;

namespace STranslate.ViewModels.Pages;

public partial class StandaloneViewModel : SearchViewModelBase
{
    public StandaloneViewModel(Settings settings, DataProvider dataProvider, Internationalization i18n) : base(i18n, "Standalone_")
    {
        Settings = settings;
        DataProvider = dataProvider;

        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Settings.LayoutAnalysisMode))
            {
                switch (Settings.LayoutAnalysisMode)
                {
                    case LayoutAnalysisMode.MergeAll:
                        UpdateRatio(6.0, 6.0, 0.3, 0.2);
                        break;
                    case LayoutAnalysisMode.StrictParagraph:
                        UpdateRatio(0.3, 0.2, 0.2, 0.1);
                        break;
                    case LayoutAnalysisMode.StandardDocument:
                        UpdateRatio(0.8, 0.5, 0.3, 0.2);
                        break;
                    case LayoutAnalysisMode.ColumnDocument:
                        UpdateRatio(0.6, 2.0, 0.3, 0.2);
                        break;
                    case LayoutAnalysisMode.NoMerge:
                        UpdateRatio(0.0, 0.0, 0.3, 0.2);
                        break;
                    case LayoutAnalysisMode.UserDefined:
                        // 用户自定义模式，不做任何更改
                        break;
                    default:
                        break;
                }
            }
        };
    }

    [RelayCommand]
    private void SliderValueChanged(Slider slider)
    {
        // 1. IsMouseCaptured: 用户是否正按住鼠标拖动滑块
        // 2. IsKeyboardFocused: 用户是否已通过 Tab 键或点击选中了滑块（准备用键盘方向键操作）
        // 3. IsMouseCaptureWithin: 鼠标是否在控件内部被捕获（更通用的鼠标操作检查）
        // 4. 当前版面分析模式不为自定义时，避免重复设置
        if ((slider.IsMouseCaptured ||
            slider.IsKeyboardFocused ||
            slider.IsMouseCaptureWithin) &&
            Settings.LayoutAnalysisMode != LayoutAnalysisMode.UserDefined)
        {
            Settings.LayoutAnalysisMode = LayoutAnalysisMode.UserDefined;
        }
    }

    /* 版面分析参数配置
     *
     * 1.全部合并
     * private const double VerticalThresholdRatio = 6.0;
     * private const double HorizontalThresholdRatio = 6.0;
     * private const double LineSpacingThresholdRatio = 0.3;
     * private const double WordSpacingThresholdRatio = 0.2;
     *
     * 2.严格段落分析（保持原始结构）
     * private const double VerticalThresholdRatio = 0.3;
     * private const double HorizontalThresholdRatio = 0.2;
     * private const double LineSpacingThresholdRatio = 0.2;
     * private const double WordSpacingThresholdRatio = 0.1;
     *
     * 3.标准文档分析（推荐默认值）
     * private const double VerticalThresholdRatio = 0.8;
     * private const double HorizontalThresholdRatio = 0.5;
     * private const double LineSpacingThresholdRatio = 0.3;
     * private const double WordSpacingThresholdRatio = 0.2;
     *
     * 4.分栏文档分析（合并左右分栏）
     * private const double VerticalThresholdRatio = 0.6;
     * private const double HorizontalThresholdRatio = 2.0;     // 增大以跨越分栏间距
     * private const double LineSpacingThresholdRatio = 0.3;
     * private const double WordSpacingThresholdRatio = 0.2;
     *
     * 5.不合并任何文本块
     * private const double VerticalThresholdRatio = 0.0;       // 设置为0，不允许垂直合并
     * private const double HorizontalThresholdRatio = 0.0;     // 设置为0，不允许水平合并
     * private const double LineSpacingThresholdRatio = 0.3;    // 这两个参数在不合并时不会被使用
     * private const double WordSpacingThresholdRatio = 0.2;    // 这两个参数在不合并时不会被使用
     */
    private void UpdateRatio(double verticalThresholdRatio,
        double horizontalThresholdRatio,
        double lineSpacingThresholdRatio,
        double wordSpacingThresholdRatio)
    {
        Settings.VerticalThresholdRatio = verticalThresholdRatio;
        Settings.HorizontalThresholdRatio = horizontalThresholdRatio;
        Settings.LineSpacingThresholdRatio = lineSpacingThresholdRatio;
        Settings.WordSpacingThresholdRatio = wordSpacingThresholdRatio;
    }

    public Settings Settings { get; }
    public DataProvider DataProvider { get; }
}
