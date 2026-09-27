using Niyah.SpicetifyBridge.Bridge;
using Niyah.SpicetifyBridge.Models;
using Serilog;
using System.Net.Sockets;
using System.Text.Json;
using System.Timers;
using MacroDeck.Sdk.Variables;

namespace Niyah.SpicetifyBridge.Services;

public sealed class SpiceWebSocketService : IWebSocketService, IDisposable
{
    private readonly ILogger _logger;
    private SpiceWebSocketServer? _server;
    private PlayerStateUpdate? _lastState;
    private IVariableApi? _variableApi;
    private bool _disposed;
    private int _actualPort = 8974;
    private bool _isRunning;
    private System.Timers.Timer? _reconnectTimer;
    private const int RECONNECT_DELAY_MS = 5000;

    public PlayerStateUpdate? LastState => _lastState;

    public event EventHandler<PlayerStateUpdate>? PlayerStateUpdated;

    public SpiceWebSocketService(ILogger logger)
    {
        _logger = logger.ForContext<SpiceWebSocketService>();
    }

    public void SetVariableApi(IVariableApi variableApi)
    {
        _variableApi = variableApi;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isRunning) return;
        _isRunning = true;

        if (!await TryStartServer())
            ScheduleReconnect();
    }

    private async Task<bool> TryStartServer()
    {
        int[] ports = { 8974, 8975, 8976, 8977, 8978, 8979, 8980 };
        Exception? lastException = null;

        foreach (var port in ports)
        {
            try
            {
                _server = new SpiceWebSocketServer();
                _server.TextMessageReceived += OnTextMessageReceived;
                _server.Start(port);
                _actualPort = port;
                _logger.Information("WebSocket server started on ws://127.0.0.1:{Port}/ws", port);
                return true;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AccessDenied ||
                                             ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                lastException = ex;
                _logger.Warning("Port {Port} is not available: {Message}", port, ex.Message);
                _server?.Dispose();
                _server = null;
            }
        }

        _logger.Error(lastException, "Could not start WebSocket server on any port.");
        return false;
    }

    private void ScheduleReconnect()
    {
        if (_reconnectTimer != null) return;

        _reconnectTimer = new System.Timers.Timer(RECONNECT_DELAY_MS);
        _reconnectTimer.Elapsed += OnReconnectTimerElapsed;
        _reconnectTimer.AutoReset = true;
        _reconnectTimer.Enabled = true;
        _logger.Warning("Scheduled reconnect in {Delay}ms", RECONNECT_DELAY_MS);
    }

    private async void OnReconnectTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_disposed || !_isRunning) return;

        _reconnectTimer?.Stop();
        _reconnectTimer?.Dispose();
        _reconnectTimer = null;

        if (!_isRunning) return;

        _logger.Information("Attempting to restart WebSocket server...");
        if (await TryStartServer())
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
        }
        else
        {
            ScheduleReconnect();
        }
    }

    private void OnTextMessageReceived(object? sender, string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            var state = _lastState != null
                ? new PlayerStateUpdate
                {
                    IsPlaying = _lastState.IsPlaying,
                    TrackName = _lastState.TrackName,
                    ArtistName = _lastState.ArtistName,
                    AlbumName = _lastState.AlbumName,
                    CurrentPosition = _lastState.CurrentPosition,
                    TrackDuration = _lastState.TrackDuration,
                    Shuffle = _lastState.Shuffle,
                    Repeat = _lastState.Repeat,
                    Volume = _lastState.Volume,
                    Muted = _lastState.Muted
                }
                : new PlayerStateUpdate();

            if (root.TryGetProperty("isPlaying", out var ip))
                state.IsPlaying = ip.ValueKind == JsonValueKind.True;

            if (root.TryGetProperty("trackName", out var tn))
                state.TrackName = tn.GetString();

            if (root.TryGetProperty("trackArtists", out var ta))
                state.ArtistName = ta.GetString();

            if (root.TryGetProperty("albumName", out var album))
                state.AlbumName = album.GetString();

            if (root.TryGetProperty("durationMs", out var dur))
                state.TrackDuration = dur.GetInt32() / 1000;

            if (root.TryGetProperty("progressMs", out var prog))
                state.CurrentPosition = prog.GetInt32() / 1000;

            if (root.TryGetProperty("shuffle", out var sh))
                state.Shuffle = sh.ValueKind == JsonValueKind.True;

            if (root.TryGetProperty("repeat", out var rep))
            {
                if (rep.ValueKind == JsonValueKind.Number)
                {
                    int r = rep.GetInt32();
                    state.Repeat = r switch { 0 => "off", 1 => "context", 2 => "track", _ => "off" };
                }
                else if (rep.ValueKind == JsonValueKind.String)
                    state.Repeat = rep.GetString() ?? "off";
            }

            if (root.TryGetProperty("volume", out var vol))
                state.Volume = vol.GetDouble();

            if (root.TryGetProperty("muted", out var mut))
                state.Muted = mut.ValueKind == JsonValueKind.True;

            _lastState = state;
            PlayerStateUpdated?.Invoke(this, state);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error parsing WebSocket message.");
        }
    }

    public async Task BroadcastAsync(object message)
    {
        if (_server == null)
        {
            _logger.Warning("BroadcastAsync: _server is null. No WebSocket server.");
            return;
        }
        var json = JsonSerializer.Serialize(message);
        _logger.Information("📤 BroadcastAsync sending: {Json}", json);
        _server.Broadcast(json);
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _isRunning = false;
        _reconnectTimer?.Stop();
        _reconnectTimer?.Dispose();
        _reconnectTimer = null;

        if (_server != null)
        {
            _server.TextMessageReceived -= OnTextMessageReceived;
            _server.Stop();
            _server.Dispose();
            _server = null;
        }
        _logger.Information("WebSocket server stopped.");
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopAsync().Wait();
    }
}