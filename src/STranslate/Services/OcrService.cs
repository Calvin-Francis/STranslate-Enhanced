using STranslate.Core;
using STranslate.Plugin;

namespace STranslate.Services;

public partial class OcrService : BaseService
{
    protected override ServiceType ServiceType => ServiceType.OCR;

    public OcrService(
        PluginManager pluginManager,
        ServiceManager serviceManager,
        PluginService PluginService,
        ServiceSettings serviceSettings,
        Internationalization i18n
    ) : base(pluginManager, serviceManager, PluginService, serviceSettings, i18n)
    {
        LoadPlugins<IOcrPlugin>();
        LoadServices<IOcrPlugin>();
    }
}
