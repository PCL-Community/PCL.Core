using System.Collections.Generic;

namespace PCL.Core.App.Configuration;

public class ConfigEventRegister(
    IEnumerable<IConfigScope> scope,
    ConfigEventHandler handler,
    ConfigEvent trigger = ConfigEvent.Changed,
    bool isPreview = false)
{
    public IEnumerable<IConfigScope> Scopes => scope;
    public ConfigEvent Trigger => trigger;
    public ConfigEventHandler Handler => handler;
    public bool IsPreview => isPreview;

    public ConfigEventRegister(
        IConfigScope scope,
        ConfigEventHandler handler,
        ConfigEvent trigger = ConfigEvent.Changed,
        bool isPreview = false
    ) : this([scope], handler, trigger, isPreview) { }

    public ConfigObserver ToObserver() => new(trigger, handler, isPreview);
}
