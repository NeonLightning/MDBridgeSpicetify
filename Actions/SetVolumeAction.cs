using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Localization;
using NeonLightning.SpicetifyBridge.Services;
using Serilog;
using System.Globalization;

namespace NeonLightning.SpicetifyBridge.Actions;

public sealed class SetVolumeAction : IActionDefinition
{
    private readonly ILogger _logger;
    private readonly IWebSocketService _wsService;

    public SetVolumeAction(ILogger logger, IWebSocketService wsService)
    {
        _logger = logger.ForContext<SetVolumeAction>();
        _wsService = wsService;
    }

    public string Id => "spicetify-set-volume";
    public LocalizedText Name => "Set Volume";
    public LocalizedText Description => "Set volume to a specific level (0-100)";
    
    public IReadOnlyList<ActionParameter> Parameters => new[]
    {
        new ActionParameter
        {
            Name = "volume",
            Description = "Volume level 0-100",
            Type = ActionParameterType.Number,
            DefaultValue = "50"
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
                var volumeObj = context.Parameters["volume"];
                var volume = Convert.ToInt32(volumeObj, CultureInfo.InvariantCulture);
                volume = Math.Clamp(volume, 0, 100);
                
                // ✅ Log the action execution
                _logger.Information("🔊 SetVolume executing with volume {Volume}", volume);

                await _wsService.BroadcastAsync(new { type = "set-volume", volume });
                return ActionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to set volume");
                return ActionResult.Failed("ExecutionFailed", "Failed to set volume");
            }
        }
    }
}