using MacroDeck.Plugin.Hosting;
using MacroDeck.Plugin.Serilog;
using NeonLightning.SpicetifyBridge;
using NeonLightning.SpicetifyBridge.Services;
using Microsoft.Extensions.DependencyInjection;

var plugin = MacroDeckPlugin.CreatePlugin(args)
    .UseMacroDeckLogging()
    .RegisterIntegration<PluginIntegration>()
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IWebSocketService, SpiceWebSocketService>();
    })
    .Build();

await plugin.RunAsync();