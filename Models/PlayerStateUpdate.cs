namespace NeonLightning.SpicetifyBridge.Models;

public class PlayerStateUpdate
{
    public bool IsPlaying { get; set; }
    public string? TrackName { get; set; }
    public string? ArtistName { get; set; }
    public string? AlbumName { get; set; }   // added
    public int CurrentPosition { get; set; }
    public int TrackDuration { get; set; }
    public bool Shuffle { get; set; }
    public string? Repeat { get; set; }
    public double Volume { get; set; }
    public bool Muted { get; set; }
}