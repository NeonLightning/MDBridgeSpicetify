using Niyah.SpicetifyBridge.Models;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;

namespace Niyah.SpicetifyBridge.Views;

public sealed class PlayUriActionConfigView : ActionConfigControl
{
    private readonly PluginAction _action;
    private readonly TextBox _uri;

    public PlayUriActionConfigView(PluginAction action)
    {
        _action = action;

        var model = PlayUriActionConfigModel.FromJson(action.Configuration);

        Dock = DockStyle.Fill;

        var label = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = "Spotify URI (e.g. spotify:track:... or spotify:playlist:...)"
        };

        _uri = new TextBox
        {
            Dock = DockStyle.Top,
            Text = model.Uri,
        };

        Controls.Add(_uri);
        Controls.Add(label);
    }

    public override bool OnActionSave()
    {
        var model = new PlayUriActionConfigModel { Uri = _uri.Text?.Trim() ?? string.Empty };
        _action.Configuration = model.ToJson();
        return true;
    }
}
