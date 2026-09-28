# NeonLightning Spicetify Bridge

This Macro Deck 3 bridge is maintained by NeonLightning and is based on the original Spicetify Bridge created by NiyahVE. The original project and its foundational implementation are credited to NiyahVE: https://github.com/NiyahVE/MDBridgeSpicetify. This project continues that work with updates for Macro Deck 3; thanks to NiyahVE for creating and sharing the original bridge.

Spicetify Bridge is an out-of-process **Macro Deck 3** plugin, currently version `1.0.2`, that controls Spotify through the Spicetify `Spicetify.Player` API. It uses the Macro Deck 3 SDK beta.14 packages.

## How it works

The Macro Deck plugin exposes actions and variables, and runs a local WebSocket server. The Spicetify extension, `spicetify-extension/macrodeck-bridge.js`, connects to the server, executes player commands, and sends Spotify state updates back to Macro Deck.

## Requirements

- Macro Deck 3
- Spotify desktop app
- Spicetify installed and working
- For building from source: .NET 10 SDK and the `macrodeck-plugin` CLI

## Install

1. Download and extract the latest release assets.
2. Install `com.neonlightning.spicetify-bridge-<version>.macroDeckPlugin` through Macro Deck 3's plugin manager.
3. Copy `spicetify-extension/macrodeck-bridge.js` into the Extensions folder used by your Spicetify installation. The JS file is a separate release asset; it is not inside the Macro Deck plugin artifact.
4. Add `macrodeck-bridge.js` to Spicetify's configured extensions, preserving any extensions already listed:

   ```sh
   spicetify config extensions "existing-extension.js|macrodeck-bridge.js"
   ```

   Replace `existing-extension.js` with your current extension list. If you have no other extensions, use `macrodeck-bridge.js` as the value.

5. Apply the Spicetify configuration and restart Spotify:

   ```sh
   spicetify apply
   ```

6. Start Macro Deck 3 and Spotify. The extension should connect to `ws://127.0.0.1:8974/ws/`.

To locate your Spicetify configuration directory, run `spicetify -c`, then find the active profile's Extensions folder.

## Actions

The plugin provides these Macro Deck actions:

- Play, Pause, Toggle Play
- Next Track, Previous Track
- Toggle Shuffle, Toggle Repeat, Toggle Mute
- Volume Up, Volume Down, Set Volume
- Play Spotify URI
- Seek

Volume Up and Volume Down accept a step size from `0.0` to `1.0`. Set Volume accepts `0` to `100`. Seek uses seconds. Play Spotify URI accepts a Spotify URI, for example `spotify:track:4uLU6hMCjMI75M1A2tKUQC`.

## Variables

The plugin exposes these Macro Deck variables:

- `playback-status` (text)
- `track-name` (text)
- `artist-name` (text)
- `album-name` (text)
- `current-position` (seconds)
- `track-duration` (seconds)
- `progress-percent` (percent)
- `shuffle` (boolean)
- `repeat` (text: `off`, `context`, or `track`)
- `volume` (numeric)
- `muted` (boolean)

Spotify sends progress updates while playing; the plugin reads the latest received state for these variables.

## Build from source

From the repository root, build and validate the Macro Deck plugin artifact:

```sh
macrodeck-plugin build --output ./artifacts
macrodeck-plugin validate --artifact ./artifacts/com.neonlightning.spicetify-bridge-1.0.2.macroDeckPlugin
```

The build packages the platform payloads declared by `manifest.json`. The Spicetify extension is intentionally distributed separately; copy it from `spicetify-extension/macrodeck-bridge.js` when preparing a release.

## Development notes

- The extension connects to the fixed URL `ws://127.0.0.1:8974/ws/`. Keep port `8974` available; although the plugin server tries later ports if it is occupied, the extension does not currently discover that fallback port.
- The WebSocket server listens on loopback and does not use token authentication.
- Macro Deck SDK, Hosting, and Serilog packages are pinned to `3.0.0-beta.14`; the matching analyzer is build-only.

Pull requests are welcome.
