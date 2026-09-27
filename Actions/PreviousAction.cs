using SuchByte.MacroDeck.ActionButton;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class PreviousAction : SpiceActionBase
{
    public override string Name => "Spotify: Previous";
    public override string Description => "Go to previous track";

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        _ = Task.Run(() => Main.SendCommandAsync(new { type = "previous" }));
    }
}
