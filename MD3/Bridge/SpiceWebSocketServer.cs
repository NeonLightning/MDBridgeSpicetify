using Fleck;
using Serilog;

namespace Niyah.SpicetifyBridge.Bridge;

public class SpiceWebSocketServer : IDisposable
{
    private WebSocketServer? _server;
    private readonly List<IWebSocketConnection> _sockets = new();
    private readonly ILogger _logger = Log.ForContext<SpiceWebSocketServer>();

    public event EventHandler<string>? TextMessageReceived;

    public void Start(int port)
    {
        _server = new WebSocketServer($"ws://127.0.0.1:{port}/ws");
        _server.Start(socket =>
        {
            socket.OnOpen = () =>
            {
                _logger.Debug("WebSocket client connected");
                _sockets.Add(socket);
            };
            socket.OnClose = () =>
            {
                _logger.Debug("WebSocket client disconnected");
                _sockets.Remove(socket);
            };
            socket.OnMessage = message =>
            {
                _logger.Debug("Received message: {Message}", message);
                TextMessageReceived?.Invoke(this, message);
            };
        });
        _logger.Information("WebSocket server started on port {Port}", port);
    }

    public void Stop()
    {
        foreach (var socket in _sockets.ToList())
        {
            try { socket.Close(); } catch { }
        }
        _sockets.Clear();
        _server?.Dispose();
        _server = null;
        _logger.Information("WebSocket server stopped.");
    }

    public void Broadcast(string message)
    {
        foreach (var socket in _sockets.ToList())
        {
            try { socket.Send(message); } catch (Exception ex)
            {
                _logger.Error(ex, "Error broadcasting to a socket");
            }
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this); // removes the warning
    }
}