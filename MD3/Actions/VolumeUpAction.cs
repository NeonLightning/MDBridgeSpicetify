using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Niyah.SpicetifyBridge.Services;
using Serilog;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class VolumeUpAction : IActionDefinition
{
    private readonly ILogger _logger;
    private readonly IWebSocketService _wsService;

    public VolumeUpAction(ILogger logger, IWebSocketService wsService)
    {
        _logger = logger.ForContext<VolumeUpAction>();
        _wsService = wsService;
    }

    public string Id => "spicetify-volumeup";
    public LocalizedText Name => "Volume Up";
    public LocalizedText Description => "Increase volume by a step (0.0–1.0)";

    public IReadOnlyList<ActionParameter> Parameters => new ActionParameter[]
    {
        ActionParameter.Number(
            "delta",
            label: "Step size",
            description: "How much to increase volume (e.g., 0.05)",
            defaultValue: 0.05
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
            if (!context.Parameters.TryGetValue("delta", out var deltaObj) || deltaObj is not double delta)
                delta = 0.05;

            // Clamp to valid range 0.0–1.0
            delta = Math.Clamp(Math.Abs(delta), 0.0, 1.0);

            try
            {
                await _wsService.BroadcastAsync(new { type = "volumeDelta", delta });
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to send volume up command");
                return ActionResult.Failed("ExecutionFailed", "Failed to send volume up command");
            }
        }
    }
}