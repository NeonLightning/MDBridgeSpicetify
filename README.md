# Spicetify Bridge

`Spicetify Bridge` is a Macro Deck 2 plugin that controls Spotify through Spicetify's `Spicetify.Player` API.

This project was also made heavily with the help of AI.

Pull requests are welcome.

## How it works

1. The Macro Deck plugin starts a local WebSocket server on `127.0.0.1:8974`.
2. The Spicetify extension in `spicetify-extension/macrodeck-bridge.js` connects to that server.
3. Macro Deck actions send commands to the extension.
4. The extension calls `Spicetify.Player.*` and sends player state updates back to Macro Deck.

## Requirements

- Macro Deck 2
- Spotify desktop app
- Spicetify installed and working

## Install from the release zip

1. Download the latest release zip from the GitHub releases page.
2. Extract the zip somewhere on your PC.
3. Inside the extracted zip, you will find:
   - a `Niyah.SpicetifyBridge` folder containing the Macro Deck plugin files
   - `macrodeck-bridge.js` for Spicetify
4. Copy the entire `Niyah.SpicetifyBridge` folder into your Macro Deck plugins folder.
5. Start or restart Macro Deck.
6. Enable the `Spicetify Bridge` plugin inside Macro Deck.

## Enable the Spicetify extension

Follow these steps exactly.

### 1. Locate your Spicetify extensions folder

Open a terminal and run:

```powershell
spicetify -c
```

This shows your Spicetify configuration path. From there, locate the `Extensions` folder used by Spicetify.

### 2. Copy the extension file

From the extracted release zip, copy:

- `macrodeck-bridge.js`

into your Spicetify `Extensions` folder.

This file is included in the release zip next to the `Niyah.SpicetifyBridge` plugin folder.

The filename should stay:

- `macrodeck-bridge.js`

### 3. Register the extension with Spicetify

Run:

```powershell
spicetify config extensions macrodeck-bridge.js
```

If you already use other extensions, make sure this command does not unintentionally remove them from your existing Spicetify config. Add `macrodeck-bridge.js` alongside your other extension entries as needed.

### 4. Apply the Spicetify changes

Run:

```powershell
spicetify apply
```

### 5. Restart Spotify

Close Spotify completely, then open it again.

### 6. Verify the extension connects

Once Spotify and Macro Deck are both running and the plugin is enabled, `spicetify-extension/macrodeck-bridge.js` should connect automatically to:

- `ws://127.0.0.1:8974/ws/`

No token or port setup is required.

## Usage

After installation:

1. Add the plugin actions in Macro Deck.
2. Press a button such as play, pause, next, previous, mute, volume up/down, or play URI.
3. Macro Deck variables should update from Spotify state changes.

## Macro Deck variables

The plugin creates and updates these variables in `Main.cs`.

### Directly synced from Spotify state

These are updated from player-state messages sent by `spicetify-extension/macrodeck-bridge.js`:

- `spice_volume_percent`
- `spice_volume`
- `spice_muted`
- `spice_shuffle`
- `spice_repeat`
- `spice_repeat_mode`
- `spice_is_playing`
- `spice_playback_state`
- `spice_duration_ms`
- `spice_duration_mmss`
- `spice_track_name`
- `spice_track_artists`
- `spice_track_uri`
- `spice_track`

### Synced from Spotify, then locally smoothed

These are based on Spotify progress updates, but `Main.cs` also interpolates them locally once per second so Macro Deck stays responsive even if Spotify throttles timers:

- `spice_progress_percent`
- `spice_progress_ms`
- `spice_progress_mmss`

### Sync behavior notes

- Full state updates refresh most variables when Spotify reports events like connect, song change, play/pause, polling, or command results.
- Progress-only updates mainly refresh the progress variables and playback state.
- `spice_track` is a derived display string built in `Main.cs` from track name and artists.
- `spice_repeat_mode` is a derived string in `Main.cs` based on the numeric `spice_repeat` value.

## Build from source instead

If you do not want to use the release zip:

1. Build the plugin yourself.
2. Copy the built plugin DLL and `ExtensionManifest.json` into your Macro Deck plugins folder.
3. Copy `spicetify-extension/macrodeck-bridge.js` into your Spicetify `Extensions` folder.

## Notes for development

- `Main.cs` hard-codes the WebSocket port to `8974`.
- `Bridge/SpiceWebSocketServer.cs` accepts local WebSocket connections without token authentication.
- `spicetify-extension/macrodeck-bridge.js` connects to the fixed local WebSocket URL.
- If your local Macro Deck SDK path differs, update `Directory.Build.props`.
