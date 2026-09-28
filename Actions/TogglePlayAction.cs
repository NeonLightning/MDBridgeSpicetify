using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using NeonLightning.SpicetifyBridge.Services;
using Serilog;

namespace NeonLightning.SpicetifyBridge.Actions;

public sealed class TogglePlayAction : IActionDefinition
{
    private readonly ILogger _logger;
    private readonly IWebSocketService _wsService;

    public TogglePlayAction(ILogger logger, IWebSocketService wsService)
    {
        _logger = logger.ForContext<TogglePlayAction>();
        _wsService = wsService;
    }

    public string Id => "spicetify-toggleplay";
    public LocalizedText Name => "Toggle Play";   // implicit conversion from string
    public LocalizedText Description => "Toggle between play and pause";
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
                await _wsService.BroadcastAsync(new { type = "togglePlay" });
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to send togglePlay command");
                return ActionResult.Failed("ExecutionFailed", $"Failed to send togglePlay command");
            }
        }
    }
}
