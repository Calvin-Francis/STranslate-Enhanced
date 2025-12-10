using ChefKeys;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using NHotkey.Wpf;
using STranslate.Core;
using System.Windows;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace STranslate.Helpers;

public class HotkeyMapper
{
    private static readonly ILogger<HotkeyMapper> _logger;
    private static readonly Internationalization _i18n;
    private const string LWin = "LWin";
    private const string RWin = "RWin";

    static HotkeyMapper()
    {
        _logger = Ioc.Default.GetRequiredService<ILogger<HotkeyMapper>>();
        _i18n = Ioc.Default.GetRequiredService<Internationalization>();
    }

    internal static bool SetHotkey(string hotkeyStr, Action action)
    {
        var hotkey = new HotkeyModel(hotkeyStr);
        return SetHotkey(hotkey, action);
    }

    internal static bool SetHotkey(HotkeyModel hotkey, Action action)
    {
        string hotkeyStr = hotkey.ToString();
        try
        {
            if (hotkeyStr == LWin || hotkeyStr == RWin)
            {
                return SetWithChefKeys(hotkeyStr, action);
            }

            HotkeyManager.Current.AddOrReplace(hotkeyStr, hotkey.CharKey, hotkey.ModifierKeys, (_, _) => action.Invoke());

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error registering hotkey: {HotkeyStr}", hotkeyStr);

            ShowDialog(string.Format(_i18n.GetTranslation("RegisterHotkeyFailed"), hotkeyStr));

            return false;
        }
    }

    internal static bool RemoveHotkey(string hotkeyStr)
    {
        try
        {
            if (hotkeyStr == LWin || hotkeyStr == RWin)
            {
                return RemoveWithChefKeys(hotkeyStr);
            }

            if (!string.IsNullOrEmpty(hotkeyStr))
                HotkeyManager.Current.Remove(hotkeyStr);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error removing hotkey: {HotkeyStr}", hotkeyStr);

            ShowDialog(string.Format(_i18n.GetTranslation("UnregisterHotkeyFailed"), hotkeyStr));

            return false;
        }
    }

    internal static bool CheckAvailability(HotkeyModel currentHotkey)
    {
        try
        {
            HotkeyManager.Current.AddOrReplace("HotkeyAvailabilityTest", currentHotkey.CharKey, currentHotkey.ModifierKeys, (sender, e) => { });
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            HotkeyManager.Current.Remove("HotkeyAvailabilityTest");
        }
    }

    public static SpecialKeyState CheckModifiers()
    {
        SpecialKeyState state = new SpecialKeyState();
        if ((PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_SHIFT) & 0x8000) != 0)
        {
            //SHIFT is pressed
            state.ShiftPressed = true;
        }
        if ((PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_CONTROL) & 0x8000) != 0)
        {
            //CONTROL is pressed
            state.CtrlPressed = true;
        }
        if ((PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_MENU) & 0x8000) != 0)
        {
            //ALT is pressed
            state.AltPressed = true;
        }
        if ((PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_LWIN) & 0x8000) != 0 ||
            (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_RWIN) & 0x8000) != 0)
        {
            //WIN is pressed
            state.WinPressed = true;
        }

        return state;
    }

    #region Private Methods

    private static bool SetWithChefKeys(string hotkeyStr, Action action)
    {
        try
        {
            ChefKeysManager.RegisterHotkey(hotkeyStr, hotkeyStr, action);
            ChefKeysManager.Start();

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error registering hotkey with ChefKeys: {HotkeyStr}", hotkeyStr);

            ShowDialog(string.Format(_i18n.GetTranslation("RegisterHotkeyFailed"), hotkeyStr));

            return false;
        }
    }

    private static bool RemoveWithChefKeys(string hotkeyStr)
    {
        try
        {
            ChefKeysManager.UnregisterHotkey(hotkeyStr);
            ChefKeysManager.Stop();

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error removing hotkey: {HotkeyStr}", hotkeyStr);

            ShowDialog(string.Format(_i18n.GetTranslation("UnregisterHotkeyFailed"), hotkeyStr));

            return false;
        }
    }

    private static void ShowDialog(string message)
    {
        try
        {
            // 使用 Dispatcher.BeginInvoke 确保在 UI 线程上延迟执行
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                        message,
                        Constant.AppName,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        MessageBoxResult.OK);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to show message box in dispatcher");
                }
            }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to show message box");
        }
    }

    #endregion
}