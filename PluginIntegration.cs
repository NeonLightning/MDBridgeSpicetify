using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Sdk.Variables;
using NeonLightning.SpicetifyBridge.Actions;
using NeonLightning.SpicetifyBridge.Services;
using Serilog;

namespace NeonLightning.SpicetifyBridge;

public sealed class PluginIntegration : IPluginIntegration, IVariableProvider
{
    private readonly ILogger _logger;
    private readonly IWebSocketService _wsService;

    public PluginIntegration(ILogger logger, IWebSocketService wsService)
    {
        _logger = logger.ForContext<PluginIntegration>();
        _wsService = wsService;

        Actions = new IActionDefinition[]
        {
            new PlayAction(logger, wsService),
            new PauseAction(logger, wsService),
            new TogglePlayAction(logger, wsService),
            new NextAction(logger, wsService),
            new PreviousAction(logger, wsService),
            new ToggleShuffleAction(logger, wsService),
            new ToggleRepeatAction(logger, wsService),
            new ToggleMuteAction(logger, wsService),
            new VolumeUpAction(logger, wsService),
            new VolumeDownAction(logger, wsService),
            new PlayUriAction(logger, wsService),
            new SetVolumeAction(logger, wsService),
            new SeekAction(logger, wsService),
        };
    }

    public IReadOnlyList<IActionDefinition> Actions { get; }

    public IReadOnlyList<VariableDefinition> Variables { get; } = new VariableDefinition[]
    {
        new() { Id = "playback-status",  Name = "Playback Status",  Type = VariableType.Text,    Materialization = VariableMaterialization.Eager },
        new() { Id = "track-name",       Name = "Track Name",       Type = VariableType.Text,    Materialization = VariableMaterialization.Eager },
        new() { Id = "artist-name",      Name = "Artist Name",      Type = VariableType.Text,    Materialization = VariableMaterialization.Eager },
        new() { Id = "album-name",       Name = "Album Name",       Type = VariableType.Text,    Materialization = VariableMaterialization.Eager },
        new() { Id = "current-position", Name = "Current Position", Type = VariableType.Numeric, Materialization = VariableMaterialization.Eager },
        new() { Id = "track-duration",   Name = "Track Duration",   Type = VariableType.Numeric, Materialization = VariableMaterialization.Eager },
        new() { Id = "progress-percent", Name = "Progress Percent", Type = VariableType.Numeric, Materialization = VariableMaterialization.Eager },
        new() { Id = "shuffle",          Name = "Shuffle",          Type = VariableType.Boolean, Materialization = VariableMaterialization.Eager },
        new() { Id = "repeat",           Name = "Repeat",           Type = VariableType.Text,    Materialization = VariableMaterialization.Eager },
        new() { Id = "volume",           Name = "Volume",           Type = VariableType.Numeric, Materialization = VariableMaterialization.Eager },
        new() { Id = "muted",            Name = "Muted",            Type = VariableType.Boolean, Materialization = VariableMaterialization.Eager },
        new() { Id = "dummy",            Name = "Dummy",            Type = VariableType.Text,    Materialization = VariableMaterialization.Eager },
    };

    public async Task InitializeAsync(IIntegrationContext context)
    {
        _wsService.SetVariableApi(context.Variables);
        try
        {
            await _wsService.StartAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "WebSocket server failed to start – variables will not update.");
        }
        _logger.Information("Spicetify Bridge initialized.");
    }

    public async Task ShutdownAsync()
    {
        await _wsService.StopAsync();
        _logger.Information("Spicetify Bridge shutdown.");
    }

    public ValueTask<VariableReading> ReadAsync(string localId, CancellationToken cancellationToken = default)
    {
        var state = _wsService.LastState;
        
        // Return placeholder values if no state yet
        if (state == null)
        {
            return localId switch
            {
                "playback-status"  => ValueTask.FromResult(VariableReading.Of("Loading...")),
                "track-name"       => ValueTask.FromResult(VariableReading.Of("")),
                "artist-name"      => ValueTask.FromResult(VariableReading.Of("")),
                "album-name"       => ValueTask.FromResult(VariableReading.Of("")),
                "current-position" => ValueTask.FromResult(VariableReading.Of(0.0)),
                "track-duration"   => ValueTask.FromResult(VariableReading.Of(0.0)),
                "progress-percent" => ValueTask.FromResult(VariableReading.Of(0.0)),
                "shuffle"          => ValueTask.FromResult(VariableReading.Of(false)),
                "repeat"           => ValueTask.FromResult(VariableReading.Of("off")),
                "volume"           => ValueTask.FromResult(VariableReading.Of(0.0)),
                "muted"            => ValueTask.FromResult(VariableReading.Of(false)),
                _ => ValueTask.FromResult(VariableReading.Unavailable)
            };
        }

        // Real values
        return localId switch
        {
            "playback-status"  => ValueTask.FromResult(VariableReading.Of(state.IsPlaying ? "Playing" : "Paused")),
            "track-name"       => ValueTask.FromResult(VariableReading.Of(state.TrackName ?? "")),
            "artist-name"      => ValueTask.FromResult(VariableReading.Of(state.ArtistName ?? "")),
            "album-name"       => ValueTask.FromResult(VariableReading.Of(state.AlbumName ?? "")),
            "current-position" => ValueTask.FromResult(VariableReading.Of((double)state.CurrentPosition)),
            "track-duration"   => ValueTask.FromResult(VariableReading.Of((double)state.TrackDuration)),
            "progress-percent" => ValueTask.FromResult(VariableReading.Of(
                state.TrackDuration > 0 ? state.CurrentPosition * 100.0 / state.TrackDuration : 0.0)),
            "shuffle"          => ValueTask.FromResult(VariableReading.Of(state.Shuffle)),
            "repeat"           => ValueTask.FromResult(VariableReading.Of(state.Repeat ?? "off")),
            "volume"           => ValueTask.FromResult(VariableReading.Of(state.Volume)),
            "muted"            => ValueTask.FromResult(VariableReading.Of(state.Muted)),
            _ => ValueTask.FromResult(VariableReading.Unavailable)
        };
    }
}