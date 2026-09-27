using System.Text.Json;
using Niyah.SpicetifyBridge.Bridge;
using Niyah.SpicetifyBridge.Models;
using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Plugins;

namespace Niyah.SpicetifyBridge;

public static class PluginInstance
{
    public static Main? Main { get; set; }
}

public sealed class Main : MacroDeckPlugin
{
    internal readonly SpiceWebSocketServer WsServer = new();

    private readonly MacroDeckVariableUpdater _variables;

    // Smooth local progress updates (avoids relying solely on Spotify/Electron timers,
    // which may get throttled when the Spotify window is backgrounded).
    private readonly System.Threading.Timer _progressTimer;
    private readonly object _progressLock = new();
    private DateTime _progressBaseAtUtc = DateTime.MinValue;
    private int _progressBaseMs;
    private int _durationMs;
    private bool _isPlaying;

    private const int WebSocketPort = 8974;

    public Main()
    {
        PluginInstance.Main ??= this;

        _variables = new MacroDeckVariableUpdater(this);
        WsServer.TextMessageReceived += WsServerOnTextMessageReceived;

        _progressTimer = new System.Threading.Timer(_ => ProgressTimerTick(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public override bool CanConfigure => false;

    public override void Enable()
    {
        // Start local progress smoothing at 1Hz
        _progressTimer.Change(dueTime: 0, period: 1000);

        Actions = new()
        {
            new Actions.PlayAction(),
            new Actions.PauseAction(),
            new Actions.TogglePlayAction(),
            new Actions.NextAction(),
            new Actions.PreviousAction(),
            new Actions.ToggleShuffleAction(),
            new Actions.ToggleRepeatAction(),
            new Actions.ToggleMuteAction(),
            new Actions.VolumeUpAction(),
            new Actions.VolumeDownAction(),
            new Actions.PlayUriAction(),
        };

        // Pre-create variables so they appear in Macro Deck's variable list even before the first update arrives.
        // Uses the same VariableManager.SetValue approach as the official Twitch plugin.
        _variables.EnsureInt("spice_volume_percent", 0);
        _variables.EnsureString("spice_volume", "0");
        _variables.EnsureBool("spice_muted", false);
        _variables.EnsureBool("spice_shuffle", false);
        _variables.EnsureInt("spice_repeat", 0);
        _variables.EnsureString("spice_repeat_mode", "off");
        _variables.EnsureBool("spice_is_playing", false);
        _variables.EnsureString("spice_playback_state", "paused");

        _variables.EnsureInt("spice_progress_percent", 0);
        _variables.EnsureInt("spice_progress_ms", 0);
        _variables.EnsureInt("spice_duration_ms", 0);
        _variables.EnsureString("spice_progress_mmss", "00:00");
        _variables.EnsureString("spice_duration_mmss", "00:00");

        _variables.EnsureString("spice_track_name", "");
        _variables.EnsureString("spice_track_artists", "");
        _variables.EnsureString("spice_track_uri", "");
        _variables.EnsureString("spice_track", "");

        try
        {
            WsServer.Start(WebSocketPort);
            MacroDeckLogger.Info(this, $"WebSocket server started on ws://127.0.0.1:{WebSocketPort}/ws");
        }
        catch (Exception ex)
        {
            MacroDeckLogger.Error(this, $"Failed to start WebSocket server on port {WebSocketPort}: {ex.Message}");
        }
    }

    public void Disable()
    {
        try { _progressTimer.Change(Timeout.Infinite, Timeout.Infinite); } catch { }
        WsServer.Stop();
    }

    private void WsServerOnTextMessageReceived(object? sender, string text)
    {
        // Expect messages from spicetify-extension/macrodeck-bridge.js
        // { type: "playerState", isPlaying, volume, muted, shuffle, repeat, progressMs, durationMs, progressPercent, trackName, trackArtists, trackUri }
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("type", out var typeEl)) return;
            if (!string.Equals(typeEl.GetString(), "playerState", StringComparison.Ordinal)) return;

            var update = JsonSerializer.Deserialize<PlayerStateUpdate>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (update == null) return;

            UpdateVariables(update);
        }
        catch
        {
            // ignore non-json / other chatter
        }
    }

    private void UpdateVariables(PlayerStateUpdate s)
    {
        // Progress updates come in once per second.
        // To keep Macro Deck responsive, only update the progress-related variables for those messages.
        if (string.Equals(s.Reason, "progress", StringComparison.Ordinal))
        {
            UpdateProgressVariables(s, updateBaseline: true);
            // isPlaying can change during progress (e.g. pause), so keep these updated too
            _variables.SetBool("spice_is_playing", s.IsPlaying);
            _variables.SetString("spice_playback_state", s.IsPlaying ? "playing" : "paused");

            lock (_progressLock)
            {
                _isPlaying = s.IsPlaying;
            }

            return;
        }

        // Full updates (songchange/playpause/poll/connected/command)
        var volumePercent = (int)Math.Round(Math.Clamp(s.Volume, 0, 1) * 100.0);

        _variables.SetInt("spice_volume_percent", volumePercent);
        _variables.SetString("spice_volume", s.Volume.ToString("0.###"));
        _variables.SetBool("spice_muted", s.Muted);
        _variables.SetBool("spice_shuffle", s.Shuffle);
        _variables.SetInt("spice_repeat", s.Repeat);
        _variables.SetString("spice_repeat_mode", s.Repeat switch { 1 => "all", 2 => "one", _ => "off" });
        _variables.SetBool("spice_is_playing", s.IsPlaying);
        _variables.SetString("spice_playback_state", s.IsPlaying ? "playing" : "paused");

        UpdateProgressVariables(s, updateBaseline: true);

        lock (_progressLock)
        {
            _isPlaying = s.IsPlaying;
        }

        _variables.SetString("spice_track_name", s.TrackName ?? "");
        _variables.SetString("spice_track_artists", s.TrackArtists ?? "");
        _variables.SetString("spice_track_uri", s.TrackUri ?? "");

        var title = string.IsNullOrWhiteSpace(s.TrackArtists)
            ? (s.TrackName ?? "")
            : $"{s.TrackName} — {s.TrackArtists}";

        _variables.SetString("spice_track", title);
    }

    private void UpdateProgressVariables(PlayerStateUpdate s, bool updateBaseline)
    {
        var durationMs = Math.Max(0, s.DurationMs);
        var progressMs = Math.Clamp(s.ProgressMs, 0, durationMs == 0 ? int.MaxValue : durationMs);

        var progressPercent = durationMs > 0
            ? (int)Math.Round((double)progressMs / durationMs * 100.0)
            : (int)Math.Round(Math.Clamp(s.ProgressPercent, 0, 1) * 100.0);

        progressPercent = Math.Clamp(progressPercent, 0, 100);

        _variables.SetInt("spice_progress_percent", progressPercent);
        _variables.SetInt("spice_progress_ms", progressMs);
        _variables.SetInt("spice_duration_ms", durationMs);
        _variables.SetString("spice_progress_mmss", FormatTrackTimeMs(progressMs));
        _variables.SetString("spice_duration_mmss", FormatTrackTimeMs(durationMs));

        if (updateBaseline)
        {
            lock (_progressLock)
            {
                _progressBaseAtUtc = DateTime.UtcNow;
                _progressBaseMs = progressMs;
                _durationMs = durationMs;
            }
        }
    }

    private void ProgressTimerTick()
    {
        // Locally interpolate progress between snapshots.
        // This keeps Macro Deck updating smoothly even if Spotify/Electron throttles JS timers.
        DateTime baseAt;
        int baseMs;
        int durationMs;
        bool isPlaying;

        lock (_progressLock)
        {
            baseAt = _progressBaseAtUtc;
            baseMs = _progressBaseMs;
            durationMs = _durationMs;
            isPlaying = _isPlaying;
        }

        if (durationMs <= 0 || baseAt == DateTime.MinValue)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var elapsedMs = (int)Math.Max(0, (now - baseAt).TotalMilliseconds);

        var estimatedProgressMs = isPlaying ? baseMs + elapsedMs : baseMs;
        estimatedProgressMs = Math.Clamp(estimatedProgressMs, 0, durationMs);

        var estimatedPercent = (int)Math.Round((double)estimatedProgressMs / durationMs * 100.0);
        estimatedPercent = Math.Clamp(estimatedPercent, 0, 100);

        _variables.SetInt("spice_progress_percent", estimatedPercent);
        _variables.SetInt("spice_progress_ms", estimatedProgressMs);
        _variables.SetString("spice_progress_mmss", FormatTrackTimeMs(estimatedProgressMs));
    }

    private static string FormatTrackTimeMs(int ms)
    {
        if (ms <= 0) return "00:00";

        var ts = TimeSpan.FromMilliseconds(ms);

        // Most tracks are < 1 hour, show mm:ss. If >= 1h, show h:mm:ss.
        if (ts.TotalHours >= 1)
        {
            return ts.ToString(@"h\:mm\:ss");
        }

        return ts.ToString(@"mm\:ss");
    }

    public override void OpenConfigurator()
    {
    }

    internal async Task SendCommandAsync(object message)
    {
        try
        {
            await WsServer.BroadcastAsync(message);
        }
        catch (Exception ex)
        {
            MacroDeckLogger.Warning(this, $"Failed to broadcast WS message: {ex.Message}");
        }
    }

}
