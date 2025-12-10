using CommunityToolkit.Mvvm.DependencyInjection;
using STranslate.Core;
using STranslate.Plugin;
using System.Windows;
using System.Windows.Controls;

namespace STranslate.Controls;

public class HistoryControlSelector : DataTemplateSelector
{
    public DataTemplate? DictionaryTemplate { get; set; }

    public DataTemplate? TranslateTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not HistoryData data)
            return base.SelectTemplate(item, container);

        var svcMgr = Ioc.Default.GetRequiredService<ServiceManager>();

        //TODO: 考虑显示“插件未安装”之类的信息
        if (svcMgr.AllServices.FirstOrDefault(x => x.MetaData.PluginID == data.PluginID && x.ServiceID == data.ServiceID)
                is not Service service)
            return base.SelectTemplate(item, container);

        return service.Plugin switch
        {
            IDictionaryPlugin => DictionaryTemplate,
            ITranslatePlugin => TranslateTemplate,
            _ => base.SelectTemplate(item, container),
        };
    }
}