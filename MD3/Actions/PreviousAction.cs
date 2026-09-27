using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Niyah.SpicetifyBridge.Services;
using Serilog;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class PreviousAction : IActionDefinition
{
    private readonly ILogger _logger;
    private readonly IWebSocketService _wsService;

    public PreviousAction(ILogger logger, IWebSocketService wsService)
    {
        _logger = logger.ForContext<PreviousAction>();
        _wsService = wsService;
    }

    public string Id => "spicetify-previous";
    public LocalizedText Name => "Previous Track";   // implicit conversion from string
    public LocalizedText Description => "Skip to previous track";
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
                await _wsService.BroadcastAsync(new { type = "previous" });
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to send previous command");
                return ActionResult.Failed("ExecutionFailed", $"Failed to send previous command");
            }
        }
    }
}
