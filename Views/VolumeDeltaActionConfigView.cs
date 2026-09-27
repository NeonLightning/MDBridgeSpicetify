using Niyah.SpicetifyBridge.Models;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;

namespace Niyah.SpicetifyBridge.Views;

public sealed class VolumeDeltaActionConfigView : ActionConfigControl
{
    private readonly PluginAction _action;
    private readonly NumericUpDown _percent;

    public VolumeDeltaActionConfigView(PluginAction action)
    {
        _action = action;

        var model = VolumeDeltaActionConfigModel.FromJson(action.Configuration);

        Dock = DockStyle.Fill;

        var label = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = "Step (percent)",
        };

        _percent = new NumericUpDown
        {
            Dock = DockStyle.Top,
            Minimum = 1,
            Maximum = 100,
            Value = (decimal)Math.Clamp(model.Delta * 100.0, 1, 100),
        };

        Controls.Add(_percent);
        Controls.Add(label);
    }

    public override bool OnActionSave()
    {
        var model = new VolumeDeltaActionConfigModel
        {
            Delta = (double)_percent.Value / 100.0,
        };

        _action.Configuration = model.ToJson();
        return true;
    }
}
