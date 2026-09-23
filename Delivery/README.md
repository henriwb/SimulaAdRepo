# Scrambly Playable — TODO: game title

TODO: one-paragraph pitch — core mechanic, progression/ending, and how it connects to Scrambly.

## Run instructions

The build is a single self-contained file (`index.html`) with no external requests, login, backend or API keys.

1. Unzip the package.
2. From the unzipped folder, start any static HTTP server, e.g.:
   - Python 3: `python -m http.server 8000`
   - Node: `npx serve .`
3. Open http://localhost:8000 in a browser.
4. Mobile testing: use DevTools device emulation (390×844 or 320×568), or open
   `http://<your-PC-IP>:8000` from a phone on the same network
   (start the server with `python -m http.server 8000 --bind 0.0.0.0`).

Opening `index.html` directly (file://) also runs the game, but a static server is the intended setup:
under file:// Chrome logs a harmless "Unsafe attempt to load URL … 'file:' URLs are treated as unique
security origins" warning, because the Playworks wrapper uses an internal iframe and file:// gives each
frame its own origin. Served over HTTP, the warning does not appear.

## Package contents

```
index.html      Runnable build (Unity Playworks export, single file)
README.md       This file
source/         Unity project source: Assets/, Packages/manifest.json, ProjectSettings/, luna.json
```

## Build instructions (rebuild from source)

Toolchain:
- Unity **6000.0.84f1** (Unity 6 LTS). Unity 6.3 is not supported by Playworks 7.2 — the exporter fails with `BuiltinResource.m_InstanceID not found`.
- Unity Playworks Plugin **7.2.0** (free Lite account). Install via Package Manager → Add package from disk → `<plugin>/scripts/package.json`.
  `Packages/manifest.json` references it by relative path (`file:../../../Pluguins/7.2.0/scripts`); adjust to your plugin location.
- Visual Studio 2022 Build Tools (MSBuild 17, ".NET desktop build tools") + **.NET Framework 4.7 Targeting Pack**.
  Set the MSBuild path in `luna.json` → `msbuildWin64`.

Steps:
1. Open `source/` in Unity 6000.0.84f1.
2. Playworks window → Upload To Creative Library → **Build And Upload**.
3. Creative Library → export **Unity Ads** → rename `*_unityads.html` to `index.html`.

Build-time only: the Playworks login/upload is used to produce the HTML. The exported file does not depend on it at runtime.

## Runtime verification (no external requests)

TODO (re-run on the final build): served via `python -m http.server`; DevTools → Network shows only `localhost` requests; the playable also runs end-to-end with DevTools Network set to **Offline**.

Note: the exported HTML contains URL strings from Playworks' built-in debug tooling (`stats.js`, `spector.js`, `console.re`). They are never loaded at runtime — see the Network check above. Luna analytics calls were removed from the game code.

## Requirements checklist

| Requirement | Status / implementation |
|---|---|
| Touch + mouse | Horizontal swipe anywhere on the game UI aims (EventSystem drag → `SwipeInputView`); Shoot is a UI Button. TODO: verify on device |
| Portrait 320×568 / 390×844 | TODO |
| Other orientation (adapt or rotate prompt) | TODO |
| No page scroll competing with gameplay | TODO |
| CTA: local "CTA clicked — demo only" + console log, no navigation | `EndCardController.ClickCTA` (verify console log in the web build) |
| Pause gameplay/clocks while hidden, resume without time jumps | TODO |
| Audio: starts after interaction, mute, silent while hidden | TODO (or "no audio") |
| Restart resets cleanly (no duplicate timers/listeners/effects) | Replay → `ResetGame()` (in-place reset, hides End Card); TODO: verify 4× replays |
| Resize / interrupted input handling | TODO |
| ZIP ≤ 5,000,000 bytes | TODO: final size in bytes |

## Testing

| Browser / device | Real or emulated | Result |
|---|---|---|
| TODO | TODO | TODO |

Edge cases tested: TODO (e.g. hide tab mid-run, restart 4× in a row, rotate mid-run, release touch outside the canvas).

## Known limitations

- TODO
- Additive scene loading (`LoadSceneAsync`/`UnloadSceneAsync` with `LoadSceneMode.Additive`) worked in the Editor but hung in the Playworks web build, so the game runs as a single scene.

## Untested / unfinished

- TODO

## Project note

### Tools, AI and reused work
- **Unity 6 + Unity Playworks Plugin** (Unity's official playable-ad tool) to write the game in C# and export an HTML5 playable.
- **Unity Playworks tutorial project** (Luna) as the starting project; reused: TODO (e.g. End Card prefab/controller).
- **Claude Code (AI)**: TODO — what it was used for (e.g. toolchain diagnosis, build-log analysis, code review, boilerplate).
- Libraries: DOTween, Newtonsoft.Json (Playworks-supported JSON), TextMesh Pro.

### What I contributed
TODO

### Decisions, corrections and rejected outputs
- **Engine choice:** kept Unity (my strongest tool) and used Playworks to meet the HTML5 / single-file / ≤5 MB / offline constraints instead of a raw Unity WebGL build.
- **Unity 6.3 → 6.0 LTS:** 6.3 is unsupported by the Playworks exporter; downgraded to the supported LTS.
- **Rejected: additive boot scene.** A GameBoot scene loading/unloading the gameplay scene additively worked in the Editor but hung in the web build; reverted to a single scene with an in-scene state reset for replay.
- **Replay leak fix:** tutorial events are ScriptableObjects that outlive scene reloads; listeners that never unsubscribed kept calling destroyed objects (`MissingReferenceException`). Added `RemoveListener` in `OnDestroy`.
- **Playworks API gaps found:** `JsonUtility` (replaced with Newtonsoft.Json) and `AudioListener.pause` (compile error in the web build).
- **Broken shaders (all pink):** a failed build left the Playworks shader cache empty (`shaders.json` = `[]`); fixed by clearing `LunaTemp/` and reverting `SVC_Luna.asset`.
- **Game: Puzzle Bobble–style bubble shooter** built entirely in UI (RectTransforms, no physics): circle math in the puzzle area's local space, so it is deterministic and Playworks-safe. Hex offset-row grid; views re-layout from the area width on resize/rotation.
- **Strict MVC, no static state:** Models are data only (`BubbleGameConfig`, `BubbleGridModel`, `GameSessionModel`); grid rules (clusters, floating bubbles, landing cell) live in `BoardController`; Views only draw and forward input; `GameLoopController` is the composition root wired by serialized references.
- **Aim guide from the real flight code:** `TrajectoryController` holds the step/bounce/contact rules used by both the shot and the preview, so the dotted guide (1 wall bounce, like Puzzle Bobble) and the ghost bubble on the landing cell can't disagree with the actual shot. Dots are pooled UI Images (LineRenderer doesn't render in a UI Canvas; a custom mesh Graphic was avoided as a Playworks risk). Recomputed only on aim change, board change or resize.
- **Always winnable:** the next bubble is drawn only from colors still on the board; no lose state (short ad session).
- **Replay = in-place state reset** (`GameLoopController.ResetGame`), not a scene reload: stops coroutines, cancels an in-flight shot, returns every bubble to the pool, hides the End Card (a GameObject toggled with `SetActive`).
- **End Card fixes:** CTA listener registered once (it was added on every open → duplicate clicks after replay); CTA shows a local "CTA clicked — demo only" message + console log instead of `InstallFullGame()`; removed `LifeCycle.GameEnded()`.
- TODO: decisions made during the 6-hour session.

### Time spent
TODO (environment setup and pipeline validation were done before the timed session).
