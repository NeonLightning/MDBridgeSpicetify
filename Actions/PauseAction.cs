using SuchByte.MacroDeck.ActionButton;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class PauseAction : SpiceActionBase
{
    public override string Name => "Spotify: Pause";
    public override string Description => "Pause playback";

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        _ = Task.Run(() => Main.SendCommandAsync(new { type = "pause" }));
    }
}
