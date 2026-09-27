using SuchByte.MacroDeck.ActionButton;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class ToggleShuffleAction : SpiceActionBase
{
    public override string Name => "Spotify: Toggle Shuffle";
    public override string Description => "Toggle shuffle";

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        _ = Task.Run(() => Main.SendCommandAsync(new { type = "toggleShuffle" }));
    }
}
