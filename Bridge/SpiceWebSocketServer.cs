using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Niyah.SpicetifyBridge.Bridge;

public sealed class SpiceWebSocketServer : IDisposable
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();

    private WebApplication? _app;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _app != null;
    public int Port { get; private set; }

    public event EventHandler<string>? TextMessageReceived;

    public event EventHandler? ClientCountChanged;

    public int ClientCount => _clients.Count;

    public void Start(int port)
    {
        if (IsRunning) return;

        Port = port;

        _cts = new CancellationTokenSource();
        var cts = _cts;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(SpiceWebSocketServer).Assembly.FullName,
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.WebHost.UseKestrel();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        var app = builder.Build();

        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(30),
        });

        app.Map("/ws", async context => await HandleWsEndpointAsync(context, cts.Token));
        app.MapGet("/health", () => Results.Text("ok"));

        _app = app;

        // fire-and-forget (Macro Deck plugin Enable is sync)
        _ = Task.Run(() => app.StartAsync(cts.Token));
    }

    public async Task BroadcastAsync(object message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        var buffer = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(buffer);

        foreach (var kv in _clients.ToArray())
        {
            var socket = kv.Value;
            if (socket.State != WebSocketState.Open)
            {
                _clients.TryRemove(kv.Key, out _);
                ClientCountChanged?.Invoke(this, EventArgs.Empty);
                continue;
            }

            try
            {
                await socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
            }
            catch
            {
                _clients.TryRemove(kv.Key, out _);
                ClientCountChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private async Task HandleWsEndpointAsync(HttpContext context, CancellationToken cancellationToken)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();

        var id = Guid.NewGuid();
        _clients[id] = socket;
        ClientCountChanged?.Invoke(this, EventArgs.Empty);

        try
        {
            await ReceiveLoopAsync(socket, cancellationToken);
        }
        finally
        {
            _clients.TryRemove(id, out _);
            ClientCountChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task ReceiveLoopAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8 * 1024];
        using var message = new MemoryStream();

        while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await socket.ReceiveAsync(buffer, cancellationToken);
            }
            catch
            {
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                }
                catch
                {
                    // ignore
                }

                break;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                // ignore binary frames
                continue;
            }

            message.Write(buffer, 0, result.Count);

            if (!result.EndOfMessage) continue;

            var text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
            message.SetLength(0);

            if (!string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    TextMessageReceived?.Invoke(this, text);
                }
                catch
                {
                    // ignore consumer errors
                }
            }
        }
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }

        var app = _app;
        _app = null;

        if (app != null)
        {
            try { app.StopAsync().GetAwaiter().GetResult(); } catch { }

            // WebApplication doesn't have a public Dispose() method; dispose via interface
            try
            {
                if (app is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
                }
                else if (app is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch { }
        }

        foreach (var ws in _clients.Values)
        {
            try { ws.Dispose(); } catch { }
        }

        _clients.Clear();
        ClientCountChanged?.Invoke(this, EventArgs.Empty);

        try { _cts?.Dispose(); } catch { }
        _cts = null;
        Port = 0;
    }

    public void Dispose() => Stop();
}
