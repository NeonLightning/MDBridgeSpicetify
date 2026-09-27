using SuchByte.MacroDeck.Plugins;

namespace Niyah.SpicetifyBridge.Actions;

public abstract class SpiceActionBase : PluginAction
{
    protected Main Main => PluginInstance.Main ?? throw new InvalidOperationException("Plugin main instance not set");
}
