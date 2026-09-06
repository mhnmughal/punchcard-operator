# Agent Continuity

_Last updated: 2026-09-06 — PROJECT STATUS: COMPLETE_

Read this + `IMPLEMENTATION.md` before touching anything.

## State

Unity 6000.3.19f1, URP (standard renderer), offline mobile puzzle game.
**13 stages: Level 0 (Operator Training) + Levels 01–12** in three Program Banks.
No obsolete 42-level content anywhere (green-field build).

| Area | State |
|------|-------|
| Core architecture | `PunchCardSimulator`, `CardEditSession`, `PunchCardLevelDefinition`/`LevelDatabase`, `SaveSystem`/`ProgressionManager`, `LevelValidator` — stable |
| Levels | all 13 authored in `PunchcardLevelBuilder` (menu: *Punchcard Operator/Rebuild All Levels*); `LevelValidator` → **13/13 VALID** |
| Tests | EditMode **29/29**, PlayMode **16/16** — every commit |
| Scenes | `Boot` → `Menu` → `Game` (build order). Boot = power-on + first-launch routing. |
| UI/UX | boot sequence, persistent fades, menu cross-fades, `ButtonFX` press feel, pause menu (+ Escape + app-focus auto-pause), Operations Manual (rules), Credits, cassette Card Vault, machine-console Game screen (bolts, card reader, PWR/RDR/RUN lamps, animated punch head), System Certified finale |
| Audio | `Sfx` — synthesised, pooled, pitch-drifted, layered. No external files. |
| Icon | `IconGenerator` — 4× supersampled procedural punch card + amber press |
| Builds | **Android APK** (IL2CPP ARM64, ~30 MB, offline-verified) · **Windows** (launches clean) · **iOS Xcode project** (generated + audited; `.ipa` needs macOS) |
| Packages | network / analytics / 2D / unrelated modules removed; renderer swapped to standard URP; MCP left in manifest per owner (editor-only, absent from builds) |
| Console | 0 errors, 0 warnings (clean recompile from cleared cache) |
| Docs | this folder, all in sync |

## Architecture decisions

1. asmdefs: `PunchcardOperator.Runtime` (gameplay + UI; refs TMP, UGUI, InputSystem),
   `.Editor` (tooling; refs Android + iOS Xcode extensions), `.Tests` (EditMode),
   `.PlayTests` (PlayMode, no editor deps — kept separate for discovery).
2. Level design lives in code; `.asset` files are generated output.
3. `LevelDatabase.asset` in `Resources/`.
4. UI is composed at runtime from `Theme` builders under a `SafeAreaPanel` root
   (styled as a machine console — deliberate; not authored prefabs / 3D).
5. `Fade` is a persistent auto-created overlay; tests set `Fade.Enabled = false`.
6. Shift = swap with adjacent row; Swap = any two rows; Repeat is per-channel and
   its count is always visible; success = carrier finishes on the target.
7. Managed stripping Medium + `Assets/PunchcardOperator/link.xml` (preserves
   Runtime, TMP, UGUI, InputSystem, JsonUtility/TextAsset).
8. `StripNetworkPermissions` (Android post-processor) + `IosBuildPostprocess`
   (export compliance + AVKit removal).

## Residual (environment-bound, not defects)

1. **iOS `.ipa`** — on macOS, open `Build/iOS/Unity-iPhone.xcodeproj`, set a
   signing team, archive. Project is generated + audited; no code work remains.
2. **On-device play test** — descoped by the project owner.
3. **Modelled-3D environment** — the game reads as a machine console in 2D; a
   literal 3D scene is a stylistic upgrade needing art assets, not a gap.
4. Remove `com.anklebreaker.unity-mcp` from `Packages/manifest.json` for a
   pristine release checkout (zero behaviour change — absent from builds already).

## Gotchas

- Input System package → `InputSystemUIInputModule`; Escape / Android-back =
  `Keyboard.current.escapeKey`.
- `unity_execute_code` = bare method body, no `using`, fully-qualified types.
- `unity_build` for Android/iOS exceeds the 120 s MCP queue timeout and returns a
  "failed" notification while the build actually finishes — watch `Editor.log` /
  the output path.
- Editor drops out of Play mode on script recompile.
- `com.unity.test-framework.performance` injects `INTERNET` at Android build time
  — handled by `StripNetworkPermissions`.

## Last verification (2026-09-06)

`13 / 13 VALID` · EditMode `29 / 29` · PlayMode `16 / 16` · Android APK Success
(permission-free) · iOS Xcode project Success (audited) · Windows Success
(launches clean) · Editor console clean.
