# DemoReplayFusion

> Parse CS2 demo files and replay them on a local server, making bots follow real match trajectories.

##  Features

- Loads demo-exported JSON data and drives bots frame-by-frame
- Bot positions and view angles fully sync with demo
- Weapon slot locking (requires `BotLocker` plugin)
- Wallhack aimbot (optional, toggleable)
- Auto-release bots after 50 seconds
- Round event-driven (`RoundStart` / `RoundEnd`)
- Detailed file logging

##  Requirements

| Component | Version |
|-----------|---------|
| CounterStrikeSharp | 1.0.367+ |
\ RAY TRACE\   https://github.com/FUNPLAY-pro-CS2/Ray-Trace
| Metamod:Source | 2.0+ |
\Base on cs2-demo-parser https://github.com/LaihoE/demoparser
| [BotLocker](https://github.com/XBribo/CS2-Bot-Locker) | 0.3.0+ (optional, for weapon lock) |

### Recommended Companion Plugins

For the best experience, use **DemoReplayFusion** together with:

| Plugin | Purpose |
|--------|---------|
| [BotImprover]((https://github.com/ed0ard/CS2-Bot-Improver)) | Enhances bot AI after replay release |
| [Nade System]((https://github.com/ed0ard/CS2-Bot-NadeSystem)) | Grenade practice and lineup system |

##  Installation

1. Download `DemoReplayFusion.dll` from [Releases]
2. Place it in your server's plugin directory

2. Filter the JSON (recommended)

Use the included Python script to extract clean frame data:

```bash
python filter_demo.py your_demo.json
```

This produces a your_demo_clean.json ready for playback.But in release,i made a bat that you can drag your json to be pasered

3. Load and replay

In CS2 server console or chat:

```
!loaddemo your_demo_clean.json
```
## ⚠️ Known Compatibility Issue with CS2-Smarter-Bot

We have great respect for the **CS2-Smarter-Bot** project and the work its author has put into making bots more intelligent and responsive. Unfortunately, when running alongside **DemoReplayFusion**, bots may become permanently stuck in a crouched position during early rounds.

After extensive debugging, we traced the root cause to the `BotState` module within CS2-Smarter-Bot, which continuously manages bot physics and posture at a low level. This conflicts with DemoReplayFusion's `Teleport`-based control, and we were unable to override it through `-duck` commands, `FL_ONGROUND`/`FL_DUCKING` flag modifications, or even `AcceptInput("Stand")`.

We sincerely hope that a future update to CS2-Smarter-Bot might provide a way to temporarily suspend `BotState` (e.g., via a public API, a convar toggle, or a time-window configuration) so that plugins like ours can coexist harmoniously.

**Temporary workaround for users:** If you experience this issue, please move the botstate plugin to a disabled state (e.g., rename its folder or move it out of the `plugins` directory) before starting a replay session with DemoReplayFusion.
