using System.Windows;

namespace STranslate.Core;

public class OcrWord
{
    public string Text { get; set; } = string.Empty;
    
    public Rect BoundingBox { get; set; }

    /// <summary>
    /// 该单词在全文中的起始索引
    /// </summary>
    public int StartIndexInFullText { get; set; }
    
    public int EndIndexInFullText => StartIndexInFullText + Text.Length;
}
