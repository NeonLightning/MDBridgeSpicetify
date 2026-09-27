using System.Collections.Concurrent;
using System.Globalization;
using SuchByte.MacroDeck.Plugins;
using SuchByte.MacroDeck.Variables;

namespace Niyah.SpicetifyBridge.Bridge;

/// <summary>
/// Macro Deck variable helper.
///
/// The official Twitch plugin uses <see cref="VariableManager.SetValue"/> which both updates
/// and implicitly creates/registers variables so they show up in Macro Deck's UI.
/// We follow the same approach.
/// </summary>
internal sealed class MacroDeckVariableUpdater
{
    private readonly MacroDeckPlugin _plugin;

    // Avoid spamming Macro Deck UI updates: only write when value actually changes.
    private readonly ConcurrentDictionary<string, string> _lastValues = new(StringComparer.Ordinal);

    public MacroDeckVariableUpdater(MacroDeckPlugin plugin)
    {
        _plugin = plugin;
    }

    /// <summary>
    /// Ensures the variable exists by writing an initial value.
    /// (We force the write once so the variable is registered in Macro Deck.)
    /// </summary>
    public void EnsureString(string name, string initialValue = "") =>
        SetString(name, initialValue ?? string.Empty, force: true);

    public void EnsureBool(string name, bool initialValue = false) =>
        SetBool(name, initialValue, force: true);

    public void EnsureInt(string name, int initialValue = 0) =>
        SetInt(name, initialValue, force: true);

    public void SetString(string name, string value) =>
        SetString(name, value, force: false);

    public void SetBool(string name, bool value) =>
        SetBool(name, value, force: false);

    public void SetInt(string name, int value) =>
        SetInt(name, value, force: false);

    private void SetString(string name, string? value, bool force)
    {
        var v = value ?? string.Empty;
        if (!force && !ShouldWrite(name, v)) return;

        VariableManager.SetValue(name, v, VariableType.String, _plugin, null!);
    }

    private void SetBool(string name, bool value, bool force)
    {
        // normalize bool into stable string representation for caching
        var cacheValue = value ? "true" : "false";
        if (!force && !ShouldWrite(name, cacheValue)) return;

        VariableManager.SetValue(name, value, VariableType.Bool, _plugin, null!);
    }

    private void SetInt(string name, int value, bool force)
    {
        var cacheValue = value.ToString(CultureInfo.InvariantCulture);
        if (!force && !ShouldWrite(name, cacheValue)) return;

        VariableManager.SetValue(name, value, VariableType.Integer, _plugin, null!);
    }

    private bool ShouldWrite(string name, string newValue)
    {
        if (_lastValues.TryGetValue(name, out var oldValue) && string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            return false;
        }

        _lastValues[name] = newValue;
        return true;
    }
}
