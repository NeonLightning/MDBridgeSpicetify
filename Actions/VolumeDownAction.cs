using Niyah.SpicetifyBridge.Models;
using Niyah.SpicetifyBridge.Views;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class VolumeDownAction : SpiceActionBase
{
    public override string Name => "Spotify: Volume Down";
    public override string Description => "Decrease volume by a step";

    public override bool CanConfigure => true;

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        var model = VolumeDeltaActionConfigModel.FromJson(Configuration);
        _ = Task.Run(() => Main.SendCommandAsync(new { type = "volumeDelta", delta = -Math.Abs(model.Delta) }));
    }

    public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
    {
        return new VolumeDeltaActionConfigView(this);
    }
}
