namespace Niyah.SpicetifyBridge.Bridge;

public sealed record SpiceCommand(string Type, object? Payload = null)
{
    public static SpiceCommand Simple(string type) => new(type);
}
