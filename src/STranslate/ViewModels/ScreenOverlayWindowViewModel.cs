using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using STranslate.Core;
using STranslate.Helpers;
using STranslate.Services;
using STranslate.Plugin;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bitmap = System.Drawing.Bitmap;

namespace STranslate.ViewModels;

/// <summary>
/// 文本块数据（用于传递给 View 创建可交互控件）
/// </summary>
public class TextBlockData
{
    public string Text { get; set; } = string.Empty;
    public Rect Bounds { get; set; }
    public double FontSize { get; set; }
}

/// <summary>
/// 屏幕覆盖翻译窗口的 ViewModel
/// 提供轻量级的屏幕覆盖翻译功能
/// </summary>
public partial class ScreenOverlayWindowViewModel : ObservableObject, IDisposable
{
    #region 事件

    /// <summary>
    /// 文本块准备就绪事件（通知 View 创建可交互控件）
    /// </summary>
    public event EventHandler<List<TextBlockData>>? TextBlocksReady;

    #endregion

    #region 构造函数和依赖注入

    public ScreenOverlayWindowViewModel(
        ILogger<ScreenOverlayWindowViewModel> logger,
        Settings settings,
        OcrService ocrService,
        TranslateService translateService,
        Internationalization i18n,
        INotification notification)
    {
        _logger = logger;
        _settings = settings;
        _ocrService = ocrService;
        _translateService = translateService;
        _i18n = i18n;
        _notification = notification;
    }

    private readonly ILogger<ScreenOverlayWindowViewModel> _logger;
    private readonly Settings _settings;
    private readonly OcrService _ocrService;
    private readonly TranslateService _translateService;
    private readonly Internationalization _i18n;
    private readonly INotification _notification;

    #endregion

    #region 属性

    /// <summary>
    /// 翻译后的图片（显示在窗口中）
    /// </summary>
    [ObservableProperty]
    public partial BitmapSource? TranslatedImage { get; set; }

    /// <summary>
    /// 原始图片（显示为背景）
    /// </summary>
    [ObservableProperty]
    public partial BitmapSource? SourceImage { get; set; }

    /// <summary>
    /// 是否正在处理
    /// </summary>
    [ObservableProperty]
    public partial bool IsProcessing { get; set; }

    /// <summary>
    /// 处理状态文本
    /// </summary>
    [ObservableProperty]
    public partial string ProcessingText { get; set; } = string.Empty;

    /// <summary>
    /// 是否固定窗口（固定后不会自动关闭）
    /// </summary>
    [ObservableProperty]
    public partial bool IsPinned { get; set; } = false;

    /// <summary>
    /// 是否显示翻译后的图片（否则显示原图）
    /// </summary>
    [ObservableProperty]
    public partial bool IsShowingTranslated { get; set; } = true;

    /// <summary>
    /// 翻译后的图片缓存
    /// </summary>
    private BitmapSource? _translatedImageCache;

    #endregion

    #region 命令

    /// <summary>
    /// 执行翻译
    /// </summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ExecuteAsync(Bitmap bitmap, CancellationToken cancellationToken)
    {
        if (IsProcessing) return;

        IsProcessing = true;

        try
        {
            // 保存原始图片作为背景
            SourceImage = Utilities.ToBitmapImage(bitmap, _settings.GetImageFormat());

            // 获取 OCR 服务
            var ocrSvc = _ocrService.GetActiveSvc<IOcrPlugin>();
            if (ocrSvc == null)
            {
                _notification.Show(_i18n.GetTranslation("Prompt"), _i18n.GetTranslation("NoOcrService"));
                return;
            }

            // 执行 OCR
            var data = Utilities.ToBytes(bitmap, _settings.GetImageFormat());
            var ocrResult = await ocrSvc.RecognizeAsync(new OcrRequest(data, _settings.OcrLanguage), cancellationToken);

            if (!ocrResult.IsSuccess || string.IsNullOrEmpty(ocrResult.Text))
            {
                _notification.Show(_i18n.GetTranslation("Prompt"), _i18n.GetTranslation("OcrFailed"));
                return;
            }

            // 检查是否有位置信息
            if (!Utilities.HasBoxPoints(ocrResult))
            {
                _notification.Show(_i18n.GetTranslation("Prompt"), "OCR 结果无位置信息，无法进行图片内翻译");
                return;
            }

            // 应用版面分析：合并同一段落内的文本块
            ApplyLayoutAnalysis(ocrResult);

            // 获取翻译服务
            if (_translateService.ImageTranslateService?.Plugin is not ITranslatePlugin tranSvc)
            {
                _notification.Show(_i18n.GetTranslation("Prompt"), _i18n.GetTranslation("NoTranslateService"));
                return;
            }

            // 并行翻译所有文本块
            await Parallel.ForEachAsync(ocrResult.OcrContents, cancellationToken, async (content, ct) =>
            {
                var (isSuccess, source, target) = await LanguageDetector.GetLanguageAsync(content.Text, ct).ConfigureAwait(false);
                if (!isSuccess)
                {
                    _logger.LogWarning("语言检测失败: {Text}", content.Text);
                }
                var result = new TranslateResult();
                await tranSvc.TranslateAsync(new TranslateRequest(content.Text, source, target), result, ct);
                content.Text = result.IsSuccess ? result.Text : content.Text;
            });

            // 生成文本块数据并通知 View 创建可交互控件
            // 获取图像 DPI
            double dpiX = SourceImage!.DpiX > 0 ? SourceImage.DpiX : 96;
            double dpiY = SourceImage.DpiY > 0 ? SourceImage.DpiY : 96;
            
            var textBlocks = GenerateTextBlockData(ocrResult, dpiX, dpiY);
            TextBlocksReady?.Invoke(this, textBlocks);
            IsShowingTranslated = true;
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("屏幕覆盖翻译已取消");
        }
        catch (Exception ex)
        {
            _notification.Show(_i18n.GetTranslation("Prompt"), $"{_i18n.GetTranslation("ImtransFailed")}\n{ex.Message}");
            _logger.LogError(ex, "屏幕覆盖翻译执行失败");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    [RelayCommand]
    private void Close(Window window)
    {
        ExecuteCancelCommand.Execute(null);
        window.Close();
    }

    /// <summary>
    /// 复制图片到剪贴板
    /// </summary>
    [RelayCommand]
    private void CopyImage()
    {
        if (TranslatedImage == null) return;
        Clipboard.SetImage(TranslatedImage);
    }

    /// <summary>
    /// 保存图片
    /// </summary>
    [RelayCommand]
    private void SaveImage()
    {
        if (TranslatedImage == null) return;

        var saveFileDialog = new SaveFileDialog
        {
            Title = _i18n.GetTranslation("SaveAs"),
            Filter = "PNG Files (*.png)|*.png|JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|All Files (*.*)|*.*",
            FileName = $"ScreenTranslate_{DateTime.Now:yyyyMMddHHmmssfff}",
            DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            AddToRecent = true
        };

        if (saveFileDialog.ShowDialog() != true)
            return;

        try
        {
            BitmapEncoder encoder = saveFileDialog.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? new PngBitmapEncoder()
                : new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(TranslatedImage));

            using var fs = new FileStream(saveFileDialog.FileName, FileMode.Create);
            encoder.Save(fs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存图片失败");
        }
    }

    /// <summary>
    /// 切换显示原图/翻译图
    /// </summary>
    [RelayCommand]
    private void ToggleImage()
    {
        if (SourceImage == null) return;

        IsShowingTranslated = !IsShowingTranslated;
        // 切换原图/翻译文本块显示（由 View 处理）
    }

    /// <summary>
    /// 切换固定状态
    /// </summary>
    [RelayCommand]
    private void TogglePin()
    {
        IsPinned = !IsPinned;
    }

    #endregion

    #region 私有方法 - 文本块数据生成

    /// <summary>
    /// 根据 OCR 结果生成文本块数据
    /// </summary>
    private List<TextBlockData> GenerateTextBlockData(OcrResult ocrResult, double dpiX, double dpiY)
    {
        var textBlocks = new List<TextBlockData>();

        if (ocrResult?.OcrContents == null)
            return textBlocks;

        double scaleX = 96.0 / dpiX;
        double scaleY = 96.0 / dpiY;

        foreach (var content in ocrResult.OcrContents)
        {
            if (content.BoxPoints == null || content.BoxPoints.Count == 0 || string.IsNullOrEmpty(content.Text))
                continue;

            // 获取原始像素边界
            var pixelBounds = CalculateBoundingRect(content.BoxPoints);
            
            // 转换为 WPF 逻辑坐标
            var logicBounds = new Rect(
                pixelBounds.Left * scaleX,
                pixelBounds.Top * scaleY,
                pixelBounds.Width * scaleX,
                pixelBounds.Height * scaleY
            );

            // 计算适合的字体大小（基于逻辑高度）
            double fontSize = Math.Max(10, logicBounds.Height * 0.75);
            fontSize = Math.Min(fontSize, 48);

            textBlocks.Add(new TextBlockData
            {
                Text = content.Text,
                Bounds = logicBounds,
                FontSize = fontSize
            });
        }

        return textBlocks;
    }

    #endregion

    #region 私有方法 - 图片翻译渲染（保留用于兼容）

    /// <summary>
    /// 生成带有翻译文本覆盖的图像（保留用于兼容）
    /// </summary>
    private BitmapSource GenerateTranslatedImage(OcrResult ocrResult, BitmapSource? image)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (ocrResult?.OcrContents == null ||
            ocrResult.OcrContents.All(x => x.BoxPoints?.Count == 0))
        {
            return image;
        }

        double dpiX = image.DpiX > 0 ? image.DpiX : 96;
        double dpiY = image.DpiY > 0 ? image.DpiY : 96;
        double pixelsPerDip = dpiX / 96.0;

        // 超采样缩放
        double scaleFactor = 1.0;
        double minDimension = Math.Min(image.PixelWidth, image.PixelHeight);
        if (minDimension < 1000)
        {
            scaleFactor = Math.Min(4.0, 1000.0 / minDimension);
            scaleFactor = Math.Max(scaleFactor, 2.0);
        }

        int renderWidth = (int)(image.PixelWidth * scaleFactor);
        int renderHeight = (int)(image.PixelHeight * scaleFactor);

        var drawingVisual = new DrawingVisual();

        using (var drawingContext = drawingVisual.RenderOpen())
        {
            double totalScale = scaleFactor / pixelsPerDip;
            drawingContext.PushTransform(new ScaleTransform(totalScale, totalScale));
            drawingContext.DrawImage(image, new Rect(0, 0, image.PixelWidth, image.PixelHeight));

            foreach (var item in ocrResult.OcrContents)
            {
                if (item.BoxPoints == null || item.BoxPoints.Count == 0 || string.IsNullOrEmpty(item.Text))
                    continue;

                DrawTranslatedTextOverlay(drawingContext, item, pixelsPerDip);
            }

            drawingContext.Pop();
        }

        var renderBitmap = new RenderTargetBitmap(
            renderWidth,
            renderHeight,
            dpiX,
            dpiY,
            PixelFormats.Pbgra32
        );

        renderBitmap.Render(drawingVisual);
        renderBitmap.Freeze();

        return renderBitmap;
    }

    /// <summary>
    /// 在指定区域绘制翻译文本覆盖层
    /// 每个文本块独立渲染，保持原始位置
    /// </summary>
    private void DrawTranslatedTextOverlay(DrawingContext drawingContext, OcrContent content, double pixelsPerDip)
    {
        var boundingRect = CalculateBoundingRect(content.BoxPoints!);

        // 绘制纯白背景完全覆盖原文
        var backgroundBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        backgroundBrush.Freeze();

        // 背景稍微扩展确保完全覆盖
        var expandedRect = new Rect(
            boundingRect.Left,
            boundingRect.Top,
            boundingRect.Width,
            boundingRect.Height);
        drawingContext.DrawRectangle(backgroundBrush, null, expandedRect);

        // 使用醒目的深色字体
        var textBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        textBrush.Freeze();

        // 使用粗体提高可读性
        var typeface = new Typeface(
            new FontFamily("Microsoft YaHei, Arial, SimSun"),
            FontStyles.Normal,
            FontWeights.Bold,  // 粗体
            FontStretches.Normal);

        // 字体大小：尽可能填满区域高度
        double fontSize = Math.Max(10, boundingRect.Height * 0.85);
        fontSize = Math.Min(fontSize, 96); // 提高最大字体限制

        var formattedText = new FormattedText(
            content.Text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            textBrush,
            pixelsPerDip);

        // 如果文字太宽，按比例缩小
        if (formattedText.Width > boundingRect.Width && boundingRect.Width > 5)
        {
            double ratio = boundingRect.Width / formattedText.Width;
            fontSize = Math.Max(8, fontSize * ratio * 0.98);
            
            formattedText = new FormattedText(
                content.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                textBrush,
                pixelsPerDip);
        }

        // 居中绘制
        var textPosition = new Point(
            boundingRect.Left + (boundingRect.Width - formattedText.Width) / 2,
            boundingRect.Top + (boundingRect.Height - formattedText.Height) / 2
        );

        drawingContext.DrawText(formattedText, textPosition);
    }

    /// <summary>
    /// 计算边界矩形
    /// </summary>
    private static Rect CalculateBoundingRect(List<BoxPoint> points)
    {
        double minX = points.Min(p => p.X);
        double maxX = points.Max(p => p.X);
        double minY = points.Min(p => p.Y);
        double maxY = points.Max(p => p.Y);
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// 创建适应区域的最优文本
    /// </summary>
    private FormattedText CreateOptimalText(string text, Rect boundingRect, Brush textBrush, double pixelsPerDip)
    {
        const double minFontSize = 6;
        const double maxFontSize = 48;
        const double padding = 6;

        var availableWidth = Math.Max(20, boundingRect.Width - padding);
        var availableHeight = Math.Max(15, boundingRect.Height - padding);

        var estimatedFontSize = Math.Min(maxFontSize,
            Math.Max(minFontSize, Math.Min(availableHeight * 0.7, availableWidth * 0.12)));

        var optimalSize = FindOptimalFontSize(text, estimatedFontSize, minFontSize, maxFontSize,
            availableWidth, availableHeight, textBrush, pixelsPerDip);

        var formattedText = CreateFormattedText(text, optimalSize, textBrush, availableWidth, pixelsPerDip);

        if (formattedText.Height > availableHeight)
        {
            var truncatedText = TruncateTextToFit(text, optimalSize, availableWidth, availableHeight, pixelsPerDip);
            formattedText = CreateFormattedText(truncatedText, optimalSize, textBrush, availableWidth, pixelsPerDip);
        }

        return formattedText;
    }

    /// <summary>
    /// 使用二分查找确定最优字体大小
    /// </summary>
    private double FindOptimalFontSize(string text, double initialSize, double minSize, double maxSize,
        double availableWidth, double availableHeight, Brush textBrush, double pixelsPerDip)
    {
        double bestSize = minSize;
        double low = minSize;
        double high = Math.Min(maxSize, initialSize);

        while (high - low > 0.5)
        {
            var mid = (low + high) / 2;
            var testText = CreateFormattedText(text, mid, textBrush, availableWidth, pixelsPerDip);

            if (testText.Height <= availableHeight && testText.Width <= availableWidth)
            {
                bestSize = mid;
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return bestSize;
    }

    /// <summary>
    /// 截断文本以适应区域
    /// </summary>
    private string TruncateTextToFit(string text, double fontSize, double availableWidth, double availableHeight, double pixelsPerDip)
    {
        var estimatedCharsPerLine = Math.Max(1, (int)(availableWidth / (fontSize * 0.6)));
        var estimatedLines = Math.Max(1, (int)(availableHeight / (fontSize * 1.2)));
        var maxChars = estimatedCharsPerLine * estimatedLines;

        if (text.Length <= maxChars) return text;

        var truncated = maxChars > 3 ? text.Substring(0, maxChars - 3) + "..." : text.Substring(0, Math.Min(text.Length, maxChars));

        while (truncated.Length > 4)
        {
            var testText = CreateFormattedText(truncated, fontSize, new SolidColorBrush(Colors.Black), availableWidth, pixelsPerDip);
            if (testText.Height <= availableHeight) break;

            truncated = truncated.Length > 6
                ? truncated.Substring(0, truncated.Length - 4) + "..."
                : truncated.Substring(0, Math.Max(1, truncated.Length - 1));
        }

        return truncated;
    }

    /// <summary>
    /// 创建格式化文本对象
    /// </summary>
    private static FormattedText CreateFormattedText(string text, double fontSize, Brush textBrush, double maxWidth, double pixelsPerDip)
    {
        var formattedText = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Microsoft YaHei, Arial, SimSun"),
            fontSize,
            textBrush,
            pixelsPerDip);

        formattedText.MaxTextWidth = maxWidth;
        return formattedText;
    }

    #endregion

    #region 私有方法 - 版面分析

    /// <summary>
    /// 应用版面分析
    /// </summary>
    private void ApplyLayoutAnalysis(OcrResult ocrResult)
    {
        if (!Utilities.HasBoxPoints(ocrResult))
            return;

        var contentWithRects = ocrResult.OcrContents
            .Where(content => !string.IsNullOrWhiteSpace(content.Text) && content.BoxPoints?.Count > 0)
            .Select((content, index) => new ContentWithRect
            {
                Content = content,
                Rect = CalculateBoundingRect(content.BoxPoints!),
                Index = index
            })
            .OrderBy(x => x.Rect.Top)
            .ThenBy(x => x.Rect.Left)
            .ToList();

        if (contentWithRects.Count == 0)
            return;

        var mergedContents = GroupAndMergeContents(contentWithRects);
        ocrResult.OcrContents.Clear();
        ocrResult.OcrContents.AddRange(mergedContents);
    }

    private class ContentWithRect
    {
        public OcrContent Content { get; set; } = null!;
        public Rect Rect { get; set; }
        public int Index { get; set; }
    }

    /// <summary>
    /// 将相邻的 OCR 内容分组并合并
    /// </summary>
    private List<OcrContent> GroupAndMergeContents(List<ContentWithRect> contentWithRects)
    {
        var mergedContents = new List<OcrContent>();
        var used = new HashSet<int>();

        for (int i = 0; i < contentWithRects.Count; i++)
        {
            if (used.Contains(i)) continue;

            var current = contentWithRects[i];
            var group = new List<ContentWithRect> { current };
            used.Add(i);

            // 查找可以合并的相邻文本块
            for (int j = i + 1; j < contentWithRects.Count; j++)
            {
                if (used.Contains(j)) continue;

                var candidate = contentWithRects[j];
                if (AreAdjacent(current.Rect, candidate.Rect))
                {
                    group.Add(candidate);
                    used.Add(j);
                    // 更新当前边界以包含新合并的块
                    current = new ContentWithRect
                    {
                        Content = current.Content,
                        Rect = Rect.Union(current.Rect, candidate.Rect),
                        Index = current.Index
                    };
                }
            }

            // 创建合并后的 OcrContent
            if (group.Count == 1)
            {
                mergedContents.Add(group[0].Content);
            }
            else
            {
                var mergedText = string.Join(" ", group.Select(g => g.Content.Text));
                var allPoints = group.SelectMany(g => g.Content.BoxPoints ?? []).ToList();
                var mergedRect = group.Aggregate(group[0].Rect, (acc, g) => Rect.Union(acc, g.Rect));

                var mergedContent = new OcrContent
                {
                    Text = mergedText,
                    BoxPoints =
                    [
                        new BoxPoint((int)mergedRect.Left, (int)mergedRect.Top),
                        new BoxPoint((int)mergedRect.Right, (int)mergedRect.Top),
                        new BoxPoint((int)mergedRect.Right, (int)mergedRect.Bottom),
                        new BoxPoint((int)mergedRect.Left, (int)mergedRect.Bottom)
                    ]
                };
                mergedContents.Add(mergedContent);
            }
        }

        return mergedContents;
    }

    /// <summary>
    /// 判断两个矩形是否相邻（可以合并）
    /// 严格模式：只合并同一行内水平相邻的文本块，不进行垂直合并
    /// </summary>
    private bool AreAdjacent(Rect rect1, Rect rect2)
    {
        // 水平相邻检测的阈值（允许小间距）
        var horizontalThreshold = Math.Max(rect1.Height, rect2.Height) * 0.5;

        // 计算垂直重叠比例（判断是否在同一行）
        double overlapTop = Math.Max(rect1.Top, rect2.Top);
        double overlapBottom = Math.Min(rect1.Bottom, rect2.Bottom);
        double overlapHeight = Math.Max(0, overlapBottom - overlapTop);
        double minHeight = Math.Min(rect1.Height, rect2.Height);
        double verticalOverlapRatio = minHeight > 0 ? overlapHeight / minHeight : 0;

        // 必须在同一行（垂直重叠 >= 50%）
        if (verticalOverlapRatio < 0.5)
            return false;

        // 检查水平相邻（左右紧邻）
        double horizontalGap = Math.Min(
            Math.Abs(rect1.Right - rect2.Left),
            Math.Abs(rect2.Right - rect1.Left));

        return horizontalGap <= horizontalThreshold;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        TranslatedImage = null;
        SourceImage = null;
        _translatedImageCache = null;
        GC.SuppressFinalize(this);
    }

    #endregion
}
