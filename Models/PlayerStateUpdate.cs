using System.Text.Json.Serialization;

namespace Niyah.SpicetifyBridge.Models;

public sealed class PlayerStateUpdate
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("isPlaying")]
    public bool IsPlaying { get; set; }

    [JsonPropertyName("volume")]
    public double Volume { get; set; }

    [JsonPropertyName("muted")]
    public bool Muted { get; set; }

    [JsonPropertyName("shuffle")]
    public bool Shuffle { get; set; }

    [JsonPropertyName("repeat")]
    public int Repeat { get; set; }

    [JsonPropertyName("progressMs")]
    public int ProgressMs { get; set; }

    [JsonPropertyName("durationMs")]
    public int DurationMs { get; set; }

    /// <summary>
    /// 0..1
    /// </summary>
    [JsonPropertyName("progressPercent")]
    public double ProgressPercent { get; set; }

    [JsonPropertyName("trackName")]
    public string TrackName { get; set; } = "";

    [JsonPropertyName("trackArtists")]
    public string TrackArtists { get; set; } = "";

    [JsonPropertyName("trackUri")]
    public string TrackUri { get; set; } = "";

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
