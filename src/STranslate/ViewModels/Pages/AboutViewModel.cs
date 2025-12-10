using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STranslate.Core;
using STranslate.Plugin;
using STranslate.Services;
using System.Diagnostics;
using System.IO;

namespace STranslate.ViewModels.Pages;

public partial class AboutViewModel(
    Settings settings,
    DataProvider dataProvider,
    ISnackbar snackbar,
    Internationalization i18n,
    UpdaterService updaterService,
    BackupService backupService) : ObservableObject
{
    public Settings Settings { get; } = settings;
    public DataProvider DataProvider { get; } = dataProvider;
    public string Version => Constant.Version switch
    {
        "1.0.0" => Constant.Dev,
        _ => Constant.Version
    };

    #region ICommand

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        if (Version == Constant.Dev)
        {
            snackbar.ShowWarning(i18n.GetTranslation("NoCheckUpdataInDev"));
            return;
        }
        await updaterService.UpdateAppAsync(silentUpdate: false);
    }

    [RelayCommand]
    private void LocateUserData() => Locate(Path.GetDirectoryName(Path.Combine(DataLocation.SettingsDirectory)));

    [RelayCommand]
    private void LocateSettings() => Locate(DataLocation.SettingsDirectory);

    [RelayCommand]
    private void LocateLog() => Locate(Path.Combine(DataLocation.LogDirectory, Constant.Version));

    [RelayCommand]
    private void LocateCache() => Locate(DataLocation.CacheDirectory);

    private void Locate(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return;

        Process.Start("explorer.exe", folder);
    }

    [RelayCommand]
    private async Task BackupAsync()
    {
        if (Settings.Backup.Type == BackupType.Local)
            await backupService.LocalBackupAsync();
        else
            await backupService.PreWebDavBackupAsync();
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (Settings.Backup.Type == BackupType.Local)
            await backupService.LocalRestoreAsync();
        else
            await backupService.WebDavRestoreAsync();
    }

    #endregion
}
