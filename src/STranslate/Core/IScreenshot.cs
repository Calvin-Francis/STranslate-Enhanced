using System.Drawing;

namespace STranslate.Core;

/// <summary>
/// 截图结果，包含截图和屏幕区域信息
/// </summary>
public class ScreenshotResult
{
    /// <summary>
    /// 截图位图
    /// </summary>
    public Bitmap? Bitmap { get; set; }

    /// <summary>
    /// 截图区域在屏幕上的位置（像素坐标）
    /// </summary>
    public System.Windows.Rect ScreenBounds { get; set; }

    /// <summary>
    /// 是否成功获取到屏幕坐标
    /// </summary>
    public bool HasScreenBounds => ScreenBounds.Width > 0 && ScreenBounds.Height > 0;
}

/// <summary>
/// 截图接口，可以不要定义接口，懒得删了
/// </summary>
public interface IScreenshot
{
    /// <summary>
    /// 获取截图
    /// </summary>
    /// <returns></returns>
    Bitmap? GetScreenshot();
    
    /// <summary>
    /// 异步获取截图
    /// </summary>
    /// <returns></returns>
    Task<Bitmap?> GetScreenshotAsync();

    /// <summary>
    /// 异步获取截图及其屏幕坐标（用于屏幕覆盖模式）
    /// </summary>
    /// <returns>包含截图和屏幕区域信息的结果</returns>
    Task<ScreenshotResult> GetScreenshotWithBoundsAsync();
}
