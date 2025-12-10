using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STranslate.Services;
using STranslate.Plugin;
using System.Windows.Controls;

namespace STranslate.ViewModels.Pages;

/// <summary>
/// 服务 ViewModel 基类，提供通用的服务管理功能
/// </summary>
/// <typeparam name="T">服务实例类型</typeparam>
public abstract partial class ServiceViewModelBase<T>(T service) : ObservableObject where T : BaseService
{
    public T Service { get; } = service;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    public partial Service? SelectedItem { get; set; }

    [ObservableProperty] 
    public partial Control? SettingUI { get; set; }

    private bool CanRemoveService() => SelectedItem != null;

    /// <summary>
    /// 选择插件时显示配置UI
    /// </summary>
    /// <param name="value"></param>
    partial void OnSelectedItemChanged(Service? value)
    {
        if (value != null)
        {
            SettingUI = value.Plugin.GetSettingUI();
        }
        else
        {
            SettingUI = null;
        }
    }

    [RelayCommand]
    private async Task AddServiceAsync()
    {
        var result = await Service.AddAsync();
        if (result == null)
            return;

        SelectedItem = result;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveService))]
    private async Task DeleteAsync(Service service)
    {
        var result = await Service.DeleteAsync(service);
        if (!result)
            return;

        SelectedItem = null;
    }

    [RelayCommand]
    private void Duplicate(Service svc)
    {
        var service = Service.Duplicate(svc);
        SelectedItem = service;
    }
}