using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Niyah.SpicetifyBridge.Services;
using Serilog;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class PlayAction : IActionDefinition
{
    private readonly ILogger _logger;
    private readonly IWebSocketService _wsService;

    public PlayAction(ILogger logger, IWebSocketService wsService)
    {
        _logger = logger.ForContext<PlayAction>();
        _wsService = wsService;
    }

    public string Id => "spicetify-play";
    public LocalizedText Name => "Play";   // implicit conversion from string
    public LocalizedText Description => "Resume playback";
    public IReadOnlyList<ActionParameter> Parameters => Array.Empty<ActionParameter>();
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
            try
            {
                await _wsService.BroadcastAsync(new { type = "play" });
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to send play command");
                return ActionResult.Failed("ExecutionFailed", $"Failed to send play command");
            }
        }
    }
}
