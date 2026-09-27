using SuchByte.MacroDeck.ActionButton;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class ToggleRepeatAction : SpiceActionBase
{
    public override string Name => "Spotify: Toggle Repeat";
    public override string Description => "Cycle repeat mode";

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        _ = Task.Run(() => Main.SendCommandAsync(new { type = "toggleRepeat" }));
    }
}
