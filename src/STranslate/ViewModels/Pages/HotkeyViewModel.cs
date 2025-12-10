using STranslate.Core;

namespace STranslate.ViewModels.Pages;

public partial class HotkeyViewModel(HotkeySettings settings, Internationalization i18n) : SearchViewModelBase(i18n, "Hotkey_")
{
    public HotkeySettings HotkeySettings { get; } = settings;
}