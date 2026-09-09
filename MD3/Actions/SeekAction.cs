using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using Niyah.SpicetifyBridge.Services;
using Serilog;
using System.Globalization;

namespace Niyah.SpicetifyBridge.Actions;

public sealed class SeekAction : IActionDefinition
{
    private readonly ILogger _logger;
    private readonly IWebSocketService _wsService;

    public SeekAction(ILogger logger, IWebSocketService wsService)
    {
        _logger = logger.ForContext<SeekAction>();
        _wsService = wsService;
    }

    public string Id => "spicetify-seek";
    public LocalizedText Name => "Seek";
    public LocalizedText Description => "Seek to a specific position in seconds";
    
    public IReadOnlyList<ActionParameter> Parameters => new[]
    {
        new ActionParameter
        {
            Name = "position",
            Description = "Position in seconds to seek to",
            Type = ActionParameterType.Number,
            DefaultValue = "0"
        }
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
            try
            {
                var positionObj = context.Parameters["position"];
                var seconds = Convert.ToInt32(positionObj, CultureInfo.InvariantCulture);
                if (seconds < 0) seconds = 0;
                
                // ✅ Log the action execution
                _logger.Information("⏩ Seek executing with position {Position}s", seconds);

                await _wsService.BroadcastAsync(new { type = "seek", position = seconds });
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to seek");
                return ActionResult.Failed("ExecutionFailed", "Failed to seek");
            }
        }
    }
}