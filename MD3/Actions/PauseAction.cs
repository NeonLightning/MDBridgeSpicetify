using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Niyah.SpicetifyBridge.Services;
using Serilog;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class PauseAction : IActionDefinition
{
    private readonly ILogger _logger;
    private readonly IWebSocketService _wsService;

    public PauseAction(ILogger logger, IWebSocketService wsService)
    {
        _logger = logger.ForContext<PauseAction>();
        _wsService = wsService;
    }

    public string Id => "spicetify-pause";
    public LocalizedText Name => "Pause";   // implicit conversion from string
    public LocalizedText Description => "Pause playback";
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
                await _wsService.BroadcastAsync(new { type = "pause" });
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to send pause command");
                return ActionResult.Failed("ExecutionFailed", $"Failed to send pause command");
            }
        }
    }
}
