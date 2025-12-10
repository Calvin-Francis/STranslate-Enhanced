using ChefKeys;
using CommunityToolkit.Mvvm.DependencyInjection;
using iNKORE.UI.WPF.Modern.Controls;
using STranslate.Core;
using STranslate.Helpers;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace STranslate.Controls;

public partial class HotkeyControlDialog : ContentDialog
{
    public enum HkReturnType
    {
        Save,
        Delete,
        Cancel
    }

    private readonly HotkeyType _type;
    private readonly Internationalization _i18n;
    private readonly HotkeySettings _hotkeySettings;
    private readonly HotkeyModel _cacheHotkey;
    private Action? _overwriteOtherHotkey;

    private string DefaultHotkey { get; }
    public string WindowTitle { get; }
    public ObservableCollection<string> KeysToDisplay { get; } = [];
    public HkReturnType ReturnType { get; private set; } = HkReturnType.Cancel;
    public string ResultValue { get; private set; } = string.Empty;
    public string EmptyHotkey => _i18n.GetTranslation("None");

    public HotkeyControlDialog(HotkeyType type, string hotkey, string defaultHotkey, string windowTitle = "")
    {
        _type = type;
        _i18n = Ioc.Default.GetRequiredService<Internationalization>();
        _hotkeySettings = Ioc.Default.GetRequiredService<HotkeySettings>();
        WindowTitle = windowTitle switch
        {
            "" or null => _i18n.GetTranslation("BindHotkey"),
            _ => windowTitle
        };
        DefaultHotkey = defaultHotkey;
        _cacheHotkey = new HotkeyModel(hotkey);
        SetKeysToDisplay(_cacheHotkey);

        InitializeComponent();

        ChefKeysManager.StartMenuEnableBlocking = true;
        ChefKeysManager.Start();
    }

    private void OnOverwriteClick(object sender, RoutedEventArgs e)
    {
        _overwriteOtherHotkey?.Invoke();
        OnSaveClick(sender, e);
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        ChefKeysManager.StartMenuEnableBlocking = false;
        ChefKeysManager.Stop();

        // 空热键状态，重定向到删除结果
        if (KeysToDisplay.Count == 1 && KeysToDisplay[0] == EmptyHotkey)
        {
            ReturnType = HkReturnType.Delete;
            Hide();
            return;
        }
        ReturnType = HkReturnType.Save;
        ResultValue = string.Join("+", KeysToDisplay);
        Hide();
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
        => SetKeysToDisplay(new HotkeyModel(DefaultHotkey));

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        KeysToDisplay.Clear();
        KeysToDisplay.Add(EmptyHotkey);
        ResetUI();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        ChefKeysManager.StartMenuEnableBlocking = false;
        ChefKeysManager.Stop();

        ReturnType = HkReturnType.Cancel;
        Hide();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        //when alt is pressed, the real key should be e.SystemKey
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (ChefKeysManager.StartMenuBlocked && key.ToString() == ChefKeysManager.StartMenuSimulatedKey)
            return;

        SpecialKeyState specialKeyState = HotkeyMapper.CheckModifiers();

        var hotkeyModel = new HotkeyModel(
            specialKeyState.AltPressed,
            specialKeyState.ShiftPressed,
            specialKeyState.WinPressed,
            specialKeyState.CtrlPressed,
            key);

        SetKeysToDisplay(hotkeyModel);
    }

    private void SetKeysToDisplay(HotkeyModel? hotkey)
    {
        _overwriteOtherHotkey = null;
        KeysToDisplay.Clear();

        if (hotkey == null || hotkey == default(HotkeyModel) || hotkey.Value.ToString() == Constant.EmptyHotkey)
        {
            KeysToDisplay.Add(EmptyHotkey);
            return;
        }

        foreach (var key in hotkey.Value.EnumerateDisplayKeys()!)
        {
            KeysToDisplay.Add(key);
        }

        if (PART_InfoBar == null)
            return;

        UpdateUI(hotkey.Value);
    }

    private void UpdateUI(HotkeyModel hotkey)
    {
        ResetUI();

        var registeredHotkey = _hotkeySettings.RegisteredHotkeys
            .Where(x => x.Type.HasFlag(_type) || _type.HasFlag(x.Type))
            .Where(x => x.Hotkey != _cacheHotkey.ToString())
            .FirstOrDefault(x => x.Hotkey == hotkey.ToString());
        if (registeredHotkey != null)
        {
            PART_InfoBar.Visibility = Visibility.Visible;
            if (registeredHotkey.OnRemovedHotkey != null)
            {
                PART_InfoBar.Message = string.Format(_i18n.GetTranslation("HotkeyUnavailableEditable"), _i18n.GetTranslation(registeredHotkey.ResourceKey));
                SaveBtn.IsEnabled = false;
                SaveBtn.Visibility = Visibility.Collapsed;
                OverwriteBtn.Visibility = Visibility.Visible;
                _overwriteOtherHotkey = registeredHotkey.OnRemovedHotkey;
            }
            else
            {
                PART_InfoBar.Message = string.Format(_i18n.GetTranslation("HotkeyUnavailableUneditable"), _i18n.GetTranslation(registeredHotkey.ResourceKey));
                SaveBtn.IsEnabled = false;
                SaveBtn.Visibility = Visibility.Visible;
                OverwriteBtn.Visibility = Visibility.Collapsed;
            }
        }
        else if (!CheckHotkeyAvailability(hotkey, true))
        {
            PART_InfoBar.Message = _i18n.GetTranslation("HotkeyUnavailable");
            PART_InfoBar.Visibility = Visibility.Visible;
            SaveBtn.IsEnabled = false;
        }
    }

    private void ResetUI()
    {
        PART_InfoBar.Visibility = Visibility.Collapsed;
        SaveBtn.IsEnabled = true;
        SaveBtn.Visibility = Visibility.Visible;
        OverwriteBtn.Visibility = Visibility.Collapsed;
    }

    private bool CheckHotkeyAvailability(HotkeyModel hotkey, bool validateKeyGesture)
        => hotkey.ToString() is "LWin" or "RWin" || (hotkey.Validate(validateKeyGesture) && HotkeyMapper.CheckAvailability(hotkey));
}