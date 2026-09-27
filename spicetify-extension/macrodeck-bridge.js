/*
  Spicetify Extension: Macro Deck Bridge

  - Connects to the local WebSocket server started by the Macro Deck plugin.
  - Receives JSON commands and calls Spicetify.Player.*

  The WebSocket endpoint is fixed at ws://127.0.0.1:8974/ws/
*/

(function macroDeckBridge() {
  const WS_URL = "ws://127.0.0.1:8974/ws/";

  /** @type {WebSocket | null} */
  let ws = null;

  /** @type {number | null} */
  let stateInterval = null;
  let playerEventsAttached = false;

  function clamp01(v) {
    return Math.max(0, Math.min(1, v));
  }

  function buildFullPlayerStatePayload(reason) {
    const item = Spicetify.Player.data?.item;
    const artists = Array.isArray(item?.artists)
      ? item.artists.map((a) => a?.name).filter(Boolean).join(", ")
      : "";

    return {
      type: "playerState",
      reason: reason || "",
      isPlaying: Spicetify.Player.isPlaying(),
      volume: Spicetify.Player.getVolume(),
      muted: Spicetify.Player.getMute(),
      shuffle: Spicetify.Player.getShuffle(),
      repeat: Spicetify.Player.getRepeat(),
      progressMs: Spicetify.Player.getProgress(),
      durationMs: Spicetify.Player.getDuration(),
      progressPercent: Spicetify.Player.getProgressPercent(),
      trackName: item?.name || "",
      trackArtists: artists,
      trackUri: item?.uri || "",
    };
  }

  function buildProgressPayload(reason) {
    return {
      type: "playerState",
      reason: reason || "",
      isPlaying: Spicetify.Player.isPlaying(),
      progressMs: Spicetify.Player.getProgress(),
      durationMs: Spicetify.Player.getDuration(),
      progressPercent: Spicetify.Player.getProgressPercent(),
    };
  }

  function sendPayload(payload) {
    if (!ws || ws.readyState !== WebSocket.OPEN) return;

    try {
      ws.send(JSON.stringify(payload));
    } catch (e) {
      // ignore
    }
  }

  function sendFullState(reason) {
    sendPayload(buildFullPlayerStatePayload(reason));
  }

  function sendProgress(reason) {
    sendPayload(buildProgressPayload(reason));
  }

  function attachPlayerEventsOnce() {
    if (playerEventsAttached) return;
    playerEventsAttached = true;

    let lastProgressUpdate = 0;

    // Event listeners: https://spicetify.app/docs/development/api-wrapper/methods/player#eventlisteners
    Spicetify.Player.addEventListener("songchange", () => sendFullState("songchange"));
    Spicetify.Player.addEventListener("onplaypause", () => sendFullState("playpause"));

    // onprogress can fire very frequently; throttle to ~1 update/sec.
    Spicetify.Player.addEventListener("onprogress", () => {
      const now = Date.now();
      if (now - lastProgressUpdate < 1000) return;
      lastProgressUpdate = now;
      sendProgress("progress");
    });
  }

  function handleMessage(msg) {
    switch (msg.type) {
      case "play":
        Spicetify.Player.play();
        sendFullState("command");
        break;
      case "pause":
        Spicetify.Player.pause();
        sendFullState("command");
        break;
      case "togglePlay":
        Spicetify.Player.togglePlay();
        sendFullState("command");
        break;
      case "next":
        Spicetify.Player.next();
        sendFullState("command");
        break;
      case "previous":
        Spicetify.Player.back();
        sendFullState("command");
        break;
      case "toggleShuffle":
        Spicetify.Player.toggleShuffle();
        sendFullState("command");
        // UI/apply can be async; send a follow-up snapshot shortly after.
        setTimeout(() => sendFullState("command_confirm"), 300);
        break;
      case "toggleRepeat":
        Spicetify.Player.toggleRepeat();
        sendFullState("command");
        setTimeout(() => sendFullState("command_confirm"), 300);
        break;
      case "toggleMute":
        Spicetify.Player.toggleMute();
        sendFullState("command");
        setTimeout(() => sendFullState("command_confirm"), 300);
        break;
      case "volumeDelta":
        {
          const delta = typeof msg.delta === "number" ? msg.delta : 0;
          const current = Spicetify.Player.getVolume();
          Spicetify.Player.setVolume(clamp01(current + delta));
          sendFullState("command");
          setTimeout(() => sendFullState("command_confirm"), 300);
        }
        break;
      case "playUri":
        if (typeof msg.uri === "string" && msg.uri.length > 0) {
          Spicetify.Player.playUri(msg.uri);
          // songchange will fire; still send a quick update
          sendFullState("command");
        }
        break;
      default:
        console.log("[MacroDeckBridge] Unknown message", msg);
        break;
    }
  }

  function connect() {
    try {
      ws = new WebSocket(WS_URL);
    } catch (e) {
      console.log("[MacroDeckBridge] Failed to create WebSocket", e);
      setTimeout(connect, 2000);
      return;
    }

    ws.onopen = () => {
      console.log("[MacroDeckBridge] Connected", WS_URL);

      attachPlayerEventsOnce();
      sendFullState("connected");

      // Poll so Macro Deck stays updated if you change volume/shuffle/etc. inside Spotify.
      // With caching on the plugin side, 2s is usually fine and makes shuffle feel responsive.
      if (stateInterval == null) {
        stateInterval = setInterval(() => sendFullState("poll"), 2000);
      }
    };

    ws.onmessage = (ev) => {
      try {
        const msg = JSON.parse(ev.data);
        handleMessage(msg);
      } catch (e) {
        console.log("[MacroDeckBridge] Invalid JSON", e);
      }
    };

    ws.onclose = () => {
      console.log("[MacroDeckBridge] Disconnected, retrying...");
      ws = null;

      if (stateInterval != null) {
        clearInterval(stateInterval);
        stateInterval = null;
      }

      setTimeout(connect, 2000);
    };

    ws.onerror = () => {
      // onclose will follow
    };
  }

  function waitForSpicetify() {
    if (!window.Spicetify?.Player) return false;
    return true;
  }

  const timer = setInterval(() => {
    if (!waitForSpicetify()) return;
    clearInterval(timer);
    connect();
  }, 1000);
})();
