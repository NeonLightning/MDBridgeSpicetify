using System.Text.Json;

namespace Niyah.SpicetifyBridge.Models;

public sealed class VolumeDeltaActionConfigModel
{
    public double Delta { get; set; } = 0.05; // 5%

    public static VolumeDeltaActionConfigModel FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new VolumeDeltaActionConfigModel();
        try
        {
            return JsonSerializer.Deserialize<VolumeDeltaActionConfigModel>(json) ?? new VolumeDeltaActionConfigModel();
        }
        catch
        {
            return new VolumeDeltaActionConfigModel();
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this);
}
