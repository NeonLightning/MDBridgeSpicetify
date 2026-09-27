using SuchByte.MacroDeck.ActionButton;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class ToggleMuteAction : SpiceActionBase
{
    public override string Name => "Spotify: Toggle Mute";
    public override string Description => "Toggle Spotify mute on/off";
    public override bool CanConfigure => false;

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        _ = Task.Run(() => Main.SendCommandAsync(new { type = "toggleMute" }));
    }
}