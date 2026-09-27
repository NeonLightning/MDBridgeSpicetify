using SuchByte.MacroDeck.ActionButton;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class TogglePlayAction : SpiceActionBase
{
    public override string Name => "Spotify: Toggle Play/Pause";
    public override string Description => "Toggle play/pause";

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        _ = Task.Run(() => Main.SendCommandAsync(new { type = "togglePlay" }));
    }
}
