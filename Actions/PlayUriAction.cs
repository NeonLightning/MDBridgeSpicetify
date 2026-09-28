using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using NeonLightning.SpicetifyBridge.Services;
using Serilog;

namespace NeonLightning.SpicetifyBridge.Actions;

public sealed class PlayUriAction : IActionDefinition
{
    private readonly ILogger _logger;
    private readonly IWebSocketService _wsService;

    public PlayUriAction(ILogger logger, IWebSocketService wsService)
    {
        _logger = logger.ForContext<PlayUriAction>();
        _wsService = wsService;
    }

    public string Id => "spicetify-playuri";
    public LocalizedText Name => "Play Spotify URI";
    public LocalizedText Description => "Play a specific track, album, or playlist URI";

    public IReadOnlyList<ActionParameter> Parameters => new ActionParameter[]
    {
        ActionParameter.Text(
            "uri",
            label: "URI",
            description: "The Spotify URI (e.g., spotify:track:4uLU6hMCjMI75M1A2tKUQC)",
            placeholder: "spotify:track:4uLU6hMCjMI75M1A2tKUQC",
            required: true
        )
    };

    public MacroDeckPlatform Platforms => MacroDeckPlatform.All;

    public IActionExecutor CreateExecutor() => new Executor(_logger, _wsService);

    private sealed class Executor : IActionExecutor
    {
        private readonly ILogger _logger;
        private readonly IWebSocketService _wsService;

        public Executor(ILogger logger, IWebSocketService wsService)
        {
            _logger = logger;
            _wsService = wsService;
        }

        public async Task<ActionResult> ExecuteAsync(ActionExecutionContext context)
        {
            if (!context.Parameters.TryGetValue("uri", out var value) || value is not string uri || string.IsNullOrEmpty(uri))
            {
                return ActionResult.Failed("InvalidParameter", "Missing or empty URI parameter");
            }

            try
            {
                await _wsService.BroadcastAsync(new { type = "playUri", uri });
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to send play URI command");
                return ActionResult.Failed("ExecutionFailed", "Failed to send play URI command");
            }
        }
    }
}
