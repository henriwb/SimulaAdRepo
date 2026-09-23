# Scrambly Bubble Pop — playable ad

A short Puzzle Bobble–style bubble shooter built as a Scrambly playable. Drag anywhere to aim (a dotted
guide and a "ghost" bubble show exactly where the shot will land), release to shoot. Match 4 or more of
the same color to pop them; bubbles left hanging fall for bonus points. Three special wildcard bubbles
clear a whole row when popped. Every cleared bubble turns into a coin that flies into the coin counter —
the "play and earn" loop Scrambly is about — with cheers at 80% / 50% / 20% of the board cleared.
Clearing the board shows GAME CLEAR, then the End Card with the CTA and a Replay button.

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

**Console logs (CTA click):** the Playworks wrapper suppresses `console.log` while DevTools is closed.
Open DevTools (F12 → Console) *before* loading the page, then click the CTA to see `CTA clicked — demo only`.

Opening `index.html` directly (file://) also runs the game, but a static server is the intended setup:
under file:// Chrome logs a harmless "Unsafe attempt to load URL … 'file:' URLs are treated as unique
security origins" warning, because the Playworks wrapper uses an internal iframe and file:// gives each
frame its own origin. Served over HTTP, the warning does not appear.

## How to play

- **Aim:** drag left/right anywhere on the game screen (touch or mouse). The lever arrow, dotted guide
  (one wall bounce) and ghost bubble follow the aim.
- **Shoot:** release the drag. A plain tap does not shoot.
- **Match:** 4+ bubbles of the same color pop (+10 each); disconnected bubbles fall (+20 each).
- **Special bubble:** wildcard for any color; when popped it clears its whole row.
- **Win:** clear the board → GAME CLEAR → End Card (CTA, Replay). There is no lose state (short ad session).
- **Mute:** speaker button, top-left.

## Package contents

```
index.html      Runnable build (Unity Playworks export, single file)
README.md       This file
source/         Game source code (C# scripts, config, scene, luna.json)
```

Full Unity project (all assets, packages and settings): https://github.com/henriwb/SimulaAdRepo

## Build instructions (rebuild from source)

Toolchain:
- Unity **6000.0.84f1** (Unity 6 LTS). Unity 6.3 is not supported by Playworks 7.2 — the exporter fails with `BuiltinResource.m_InstanceID not found`.
- Unity Playworks Plugin **7.2.0** (free Lite account). Install via Package Manager → Add package from disk → `<plugin>/scripts/package.json`.
  `Packages/manifest.json` references it by relative path (`file:../../../Pluguins/7.2.0/scripts`); adjust to your plugin location.
- Visual Studio 2022 Build Tools (MSBuild 17, ".NET desktop build tools") + **.NET Framework 4.7 Targeting Pack**.
  Set the MSBuild path in `luna.json` → `msbuildWin64`.

Required Playworks settings (already in `luna.json`):
- **Disable Code Stripping ON** (`disableRuntimeAnalysisForCode: true`) — see "Playworks runtime analysis" below.
- Shader cache off (`useShadersCache: false`).

Steps:
1. Clone https://github.com/henriwb/SimulaAdRepo and open it in Unity 6000.0.84f1. Startup scene: `Assets/Game/Shared/Scenes/Boot.unity`.
2. Playworks window → Upload To Creative Library → **Build And Upload**.
3. Creative Library → export **Unity Ads** → rename `*_unityads.html` to `index.html`.

Build-time only: the Playworks login/upload is used to produce the HTML. The exported file does not depend on it at runtime.

If a build renders everything pink: close Unity, delete `LunaTemp/`, revert `Assets/SVC_Luna.asset`, rebuild.

## Runtime verification

Verified on the final export (headless Chrome, served from a local static HTTP server, 390×844, 320×568 and 844×390):
- The server received only `GET /index.html` (+ the browser's automatic `/favicon.ico`); no request to any other host.
- Console (logs forced on in a test copy): no errors.

The exported HTML contains URL strings from Playworks' built-in debug tooling (`stats.js`, `spector.js`,
`console.re`) and the Unity Ads wrapper's store-link code. None of them is loaded or called at runtime (see above).
Luna analytics calls and `InstallFullGame()` were removed from the game code.

Suggested manual check: DevTools → Network → reload: only `localhost`; then set Network to **Offline** and play a full run.

## Requirements checklist

| Requirement | Status / implementation |
|---|---|
| Understandable, responsive interaction | Drag anywhere to aim with live guide + ghost bubble; release to shoot; immediate pop/score/coin feedback |
| Purposeful progression, clear ending | Board clearing with milestone cheers (80/50/20%) → GAME CLEAR → End Card (CTA + Replay) |
| Connection to Scrambly | Cleared bubbles become coins flying into the coin counter ("play and earn"); End Card CTA |
| Touch + mouse | UGUI EventSystem drag (`SwipeInputView`) works for both; a drag cut by focus loss is cancelled, not fired |
| Portrait 320×568 / 390×844 | Primary orientation. CanvasScaler reference 390×844, Expand (`OrientationCanvasScaler`); board sized from a fixed design width |
| Other orientation | **Adapts** (no rotate prompt): landscape uses its own layout for the play area (`OrientationLayoutView`) and a 844×390 CanvasScaler reference so UI keeps the same on-screen size; bubbles ×0.7 in landscape |
| No page scroll competing with gameplay | Playworks wrapper: full-screen canvas, `user-scalable=no`, `overflow: hidden` |
| CTA | Explicit click → on-screen "CTA clicked — demo only" + `console.log`; no navigation (`EndCardController.ClickCTA`) |
| Pause while hidden, resume without time jumps | Playworks dispatches `luna:pause`/`luna:resume` on `visibilitychange`; gameplay and animations use a clamped delta (`SafeTime`, max 0.05 s per frame), so the first frame after resuming can't jump |
| Audio | Effects only (shot, pop, clear), all triggered by player actions (never before interaction); mute button (`MuteController`) that skips playing effects and zeroes `AudioListener.volume`; silenced while hidden through `luna:pause` |
| Resize / interrupted input | Board re-lays out whenever the play area changes size/orientation (checked each frame); drag state cleared on focus loss; End Card blocks game input while open |
| Outcomes used | Win only (board cleared); no timer, no lose state |
| Restart without duplicates | Replay = in-place reset (`GameLoopController.ResetGame`): stops coroutines, cancels an in-flight shot, returns bubbles/coins/popups to their pools, resets cheers and End Card; listeners are registered once |
| ZIP ≤ 5,000,000 bytes | Final ZIP ≈ 2.03 MB (2,027,649 bytes when packaged): index.html (2,175,361 bytes uncompressed) + this README + game source code |

## Testing

| Browser / device | Real or emulated | What was checked | Result |
|---|---|---|---|
| Unity Editor (Game view 390×844, 320×568, 844×390, 568×320) | Emulated | Full loop, special bubbles, coins, cheers, replay, End Card, mute, layouts in both orientations | OK |
| Chrome (Windows desktop), mouse | Real | Full loop in the web build, End Card, mute | OK |
| Xiaomi POCO X5 (Android), Mi Browser and Chrome for Android (web build) | Real | Touch drag-to-aim/release-to-shoot, portrait and landscape, full loop, End Card, mute | OK |
| Browser at several resolutions (portrait and landscape) | Emulated | Layout and scaling of the play area and UI | OK |
| Chrome headless, 844×390 / 390×844 / 320×568 | Emulated | Load, console errors, network requests | OK (headless Chrome enforces a ~500 px minimum viewport, so its portrait screenshots are clipped; portrait layout was checked on the phone and in the other resolution tests) |

Edge cases covered by the implementation: hiding the tab mid-shot (clamped delta, no jump on return), repeated replays (pooled objects and listeners registered once), rotating mid-run (layout re-applied every frame it changes), releasing the drag outside the canvas or losing focus (drag cancelled), mute before shooting (effects skipped).

## Known limitations

- No lose state (by design, short ad session); the first board of each page load is the same (fixed random seed — `UnityEngine.Random` is unavailable in the Playworks runtime); replays vary.
- Mute lasts for the current session only (no saved settings).
- On small phones in landscape (e.g. 568×320) the play area is small; portrait is the intended orientation.
- The Playworks simulator shows no CTA event: the CTA intentionally does not call `InstallFullGame()` (brief: local confirmation, no navigation).
- Console logs appear only if DevTools is open before the page loads (Playworks wrapper behavior).
- Single scene: additive scene loading worked in the Editor but hung in the web build.

## Untested / unfinished

- Safari and Firefox were not tested.
- Audio silence while the tab is hidden relies on the Playworks `luna:pause` event; resume timing was not measured with instruments.

## Project note

### Tools, AI and reused work
- **Unity 6 + Unity Playworks Plugin** (Unity's official playable-ad tool): game written in C#, exported as a single-file HTML5 playable.
- **Unity Playworks tutorial project** as the starting project. Reused: End Card prefab/controller, AudioManager, the idea and scene objects of `CoinEffectManager` and `CheerPhraseController` (both rewritten), DOTween/TextMesh Pro setup, UI art kit.
- **Claude Code (AI assistant)**: toolchain setup and diagnosis (Unity version, MSBuild, .NET targeting pack, Playworks settings), build-log and runtime debugging of the web export (headless Chrome, forced-on logs, reading the generated JavaScript), implementation of the MVC scripts under my direction, and this README.
- Libraries: DOTween (tutorial UI only), Newtonsoft.Json (Playworks-supported JSON), TextMesh Pro.

### What I contributed
Game concept and design (bubble shooter for a rewards app, drag-to-aim/release-to-shoot, special wildcard bubbles, coin feedback, milestone cheers, no lose state), the architecture rules (strict MVC, no static state, in-place replay), scene and UI layout for both orientations, art and sprite choices, all Editor, browser and phone testing, and every accept/reject decision on the AI's proposals.

### Decisions, corrections and rejected outputs

**Design**
- **Engine:** kept Unity (my strongest tool) and used Playworks to meet the single-file / ≤5 MB / offline HTML5 constraints instead of a raw Unity WebGL build.
- **Puzzle Bobble–style shooter built entirely in UI** (RectTransforms, no physics): circle math in the play area's local space — deterministic and Playworks-safe. Hex offset-row grid.
- **Strict MVC, no static state:** Models are data only (`BubbleGameConfig`, `BubbleGridModel`, `GameSessionModel`); grid rules live in `BoardController`; Views only draw and forward input; `GameLoopController` is the composition root wired by serialized references.
- **Always winnable:** the next bubble is drawn only from colors still on the board; no lose state.
- **Aim guide from the real flight code:** `TrajectoryController` holds the step/bounce/contact rules used by both the shot and the preview, so the dotted guide and ghost bubble can't disagree with the actual shot.
- **Input changed during development:** a circular lever and a Shoot button were replaced by "drag anywhere to aim, release to shoot".
- **Special bubbles:** 3 wildcards per board; they count as any color and clear their row; if only specials remain they pop automatically so the board can always be cleared.
- **Juice:** "+points" labels (pop-in, wobble, rainbow, rise, fade), coins arcing into the counter with a punch on arrival, milestone cheers (pop, flash, wobble, 2 s), a mascot that hops idly and jumps on each shot, a screen shake on clears (scaled by bubbles cleared, stronger for row wipes), an impact ripple (damped squash & stretch) on the landed bubble and its neighbors, an accelerating shot (ease-in from 35% to top speed — speed only, so the aim guide stays exact), the "next" indicator jumping in an arc onto the "current" slot on each shot, and a lever backfire (kick back opposite the aim + squash along the barrel, damped spring). All deterministic (no random motion) and hand-animated.
- **Orientation:** portrait-first. A rotate-only prompt was rejected (reviewers on desktop can't rotate a monitor); landscape adapts instead, with the same on-screen UI size (swapped CanvasScaler reference) and its own play-area layout.
- **Replay = in-place state reset**, not a scene reload.
- **CTA follows the brief, not the ad-network flow:** `InstallFullGame()` would log a Playworks CTA event but, without MRAID, calls `window.open(storeLink)` (navigation + external request). Replaced by a local confirmation + console log.

**Engineering (Playworks web runtime)**
- **Unity 6.3 → 6.0 LTS:** 6.3 is unsupported by the Playworks exporter.
- **Rejected: additive boot scene** (GameBoot loading/unloading the gameplay scene): worked in the Editor, hung in the web build.
- **Playworks runtime analysis (root cause of most web-only failures):** with code stripping by runtime analysis on, Playworks removes engine methods that didn't run in a previous session. Early-crashing builds marked used methods as unused (e.g. the `Image.sprite` setter, integer-cast helpers, `UnityEngine.Random`), so each build stripped what the next one needed. Fixed by turning on "Disable Code Stripping" and resetting 6,386 stale exclusions in `luna.json`.
- **How it was found:** the export hides console logs unless DevTools is open, so I ran a patched copy with logs forced on in headless Chrome and mapped obfuscated names (e.g. `zzb$` → `Bridge.Int.customFormat`) through the engine's name maps.
- **Web-safe code rules kept afterwards (harmless and more robust):** no custom numeric formats or `string.Format` with numbers, no float→int casts, index `for` loops instead of `foreach`, plain arrays instead of `Dictionary` enumeration, own `RandomSource` (Park–Miller LCG, fixed seeds, created lazily — seeding from `DateTime.Now` in field initializers caused a black-screen load), and hand-written animations instead of DOTween loops/yoyo (which ran endlessly in the web build).
- **Layout timing:** bubbles sized in `Awake` rendered at size 0 on the web (canvas sized later) → `BoardView` re-lays out whenever the play area changes.
- **End Card buttons unclickable on the web:** all canvases had sort order 0 and the web runtime resolved the tie differently → the game canvas's raycaster is disabled while the End Card is open, and the End Card canvas sorts above.
- **Mute not muting on the web:** `AudioSource.mute` wasn't honored for `PlayOneShot` → effects are skipped while muted and `AudioListener.volume` is zeroed.
- **Replay leak (tutorial code):** ScriptableObject events outlived scene reloads and kept calling destroyed listeners → `RemoveListener` in `OnDestroy`.
- **Pink shaders:** an interrupted build left the Playworks shader cache empty → cache cleared and disabled.
- **API gaps:** `JsonUtility` (→ Newtonsoft.Json), `AudioListener.pause` (compile error in the web build).

### Time spent
Covered in the walkthrough video. Environment setup and Playworks pipeline validation were done before the timed session.
