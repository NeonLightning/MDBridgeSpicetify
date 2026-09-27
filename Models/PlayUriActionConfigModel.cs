using System.Text.Json;

namespace Niyah.SpicetifyBridge.Models;

public sealed class PlayUriActionConfigModel
{
    public string Uri { get; set; } = "";

    public static PlayUriActionConfigModel FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new PlayUriActionConfigModel();
        try
        {
            return JsonSerializer.Deserialize<PlayUriActionConfigModel>(json) ?? new PlayUriActionConfigModel();
        }
        catch
        {
            return new PlayUriActionConfigModel();
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this);
}
