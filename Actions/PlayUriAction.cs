using Niyah.SpicetifyBridge.Models;
using Niyah.SpicetifyBridge.Views;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class PlayUriAction : SpiceActionBase
{
    public override string Name => "Spotify: Play URI";
    public override string Description => "Play a specific Spotify URI (track/album/playlist/etc.)";

    public override bool CanConfigure => true;

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        var model = PlayUriActionConfigModel.FromJson(Configuration);
        if (string.IsNullOrWhiteSpace(model.Uri)) return;

        _ = Task.Run(() => Main.SendCommandAsync(new { type = "playUri", uri = model.Uri }));
    }

    public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
    {
        return new PlayUriActionConfigView(this);
    }
}
