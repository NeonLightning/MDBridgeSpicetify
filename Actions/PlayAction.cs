using SuchByte.MacroDeck.ActionButton;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class PlayAction : SpiceActionBase
{
    public override string Name => "Spotify: Play";
    public override string Description => "Resume playback";

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        _ = Task.Run(() => Main.SendCommandAsync(new { type = "play" }));
    }
}
