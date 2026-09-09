using Niyah.SpicetifyBridge.Models;
using MacroDeck.Sdk.Variables;

namespace Niyah.SpicetifyBridge.Services;

public interface IWebSocketService
{
    void SetVariableApi(IVariableApi variableApi);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
    Task BroadcastAsync(object message);
    event EventHandler<PlayerStateUpdate>? PlayerStateUpdated;
    
    PlayerStateUpdate? LastState { get; }   // needed for variable provider
}