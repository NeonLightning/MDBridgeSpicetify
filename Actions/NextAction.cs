using SuchByte.MacroDeck.ActionButton;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class NextAction : SpiceActionBase
{
    public override string Name => "Spotify: Next";
    public override string Description => "Skip to next track";

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        _ = Task.Run(() => Main.SendCommandAsync(new { type = "next" }));
    }
}
