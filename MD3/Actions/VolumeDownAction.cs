using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Niyah.SpicetifyBridge.Services;
using Serilog;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class VolumeDownAction : IActionDefinition
{
    private readonly ILogger _logger;
    private readonly IWebSocketService _wsService;

    public VolumeDownAction(ILogger logger, IWebSocketService wsService)
    {
        _logger = logger.ForContext<VolumeDownAction>();
        _wsService = wsService;
    }

    public string Id => "spicetify-volumedown";
    public LocalizedText Name => "Volume Down";
    public LocalizedText Description => "Decrease volume by a step (0.0–1.0)";

    public IReadOnlyList<ActionParameter> Parameters => new ActionParameter[]
    {
        ActionParameter.Number(
            "delta",
            label: "Step size",
            description: "How much to decrease volume (e.g., 0.05)",
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

            delta = Math.Clamp(Math.Abs(delta), 0.0, 1.0);

            try
            {
                await _wsService.BroadcastAsync(new { type = "volumeDelta", delta = -delta });
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to send volume down command");
                return ActionResult.Failed("ExecutionFailed", "Failed to send volume down command");
            }
        }
    }
}