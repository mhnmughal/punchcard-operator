# Agent Continuity

_Last updated: 2026-09-06_

Read this + `IMPLEMENTATION.md` before continuing. Do not rediscover the project
from chat history.

## Current state — production completion phase

Unity 6000.3.19f1, URP, offline mobile puzzle game. **13 stages: Level 0 +
Levels 01–12.** No obsolete 42-level content anywhere (green-field build).

Done and verified:

| Area | State |
|------|-------|
| Core architecture | `PunchCardSimulator`, `CardEditSession`, `PunchCardLevelDefinition`/`LevelDatabase`, `SaveSystem`/`ProgressionManager`, `LevelValidator` — stable, don't rewrite |
| Levels | all 13 authored in `PunchcardLevelBuilder`; `LevelValidator` → **13/13 VALID** |
| Automated tests | EditMode **29/29**, PlayMode **16/16** — `CampaignPlaythroughTests` (13-stage flow), `PerStageInputTests` ×12 (literal-click per stage + failure state + illegal-edit rejection), `ResponsiveLayoutTests` ×2 (5 aspect ratios), `PerformanceProbeTests` (per-frame GC probe). All run every commit |
| Scenes | `Menu.unity` (Control Room / Card Vault / Machine Setup), `Game.unity` (any stage) |
| Tutorial | Level 0 interactive, coach-lit, Execute gated behind the required Punch, saves completion |
| Finale | `GameController.CertifiedFinale` — tower lights, bank plates, reader sweep, master plate, SYSTEM CERTIFIED, REVIEW PROGRAMS / RETURN TO CONTROL ROOM |
| Audio | `Sfx.cs` — synthesised, 6-source pool, per-cue pitch drift, layering, chords. No external files |
| Icon | `IconGenerator` → procedural punch-card art, assigned to all icon slots |
| Player Settings | landscape-only, `com.operatorworks.punchcardoperator` (placeholder), Android IL2CPP/ARM64, minSdk 24, no internet permission, iOS 13 |
| Offline | no network APIs in code; `StripNetworkPermissions` post-processor removes INTERNET; **APK verified permission-free via aapt2** |
| Builds | **Android APK — Build Success, IL2CPP ARM64, verified offline.** Windows standalone — Build Success, launches to Control Room with no exceptions |
| Console | Editor Console clean (0 errors, 0 warnings). Build log has ~20 third-party URP/TMP shader warnings, documented as non-actionable |
| Docs | `Documentation/` — implementation, audit, QA matrix, provenance ×2, package audit, offline audit, originality, this file |

## Architecture decisions (unchanged)

1. One `PunchcardOperator.Runtime` asmdef (gameplay + UI + refs TMP/UGUI).
   `PunchcardOperator.Editor` (tooling, refs `UnityEditor.Android.Extensions`).
   `PunchcardOperator.Tests` (EditMode). `PunchcardOperator.PlayTests` (PlayMode,
   no editor deps — kept separate so PlayMode tests are discoverable).
2. Level design lives in code; `.asset` files are generated output. Menu:
   `Punchcard Operator/Rebuild All Levels`.
3. `LevelDatabase.asset` lives in `Resources/`.
4. Gameplay + menu UI is composed at runtime from `Theme` builders under an
   authored `SafeAreaPanel` root. **Deliberate tradeoff** — functional, safe-area
   aware, styled (frames, plates, lamps, channel colour). A bespoke authored
   3D environment is the main thing still missing (see below).
5. Shift = swap with adjacent row; Swap = any two rows. Repeat is per-channel,
   count always visible. Success requires the carrier to finish on the target.
6. TMP font assets set to Dynamic atlas mode; UI text avoids glyphs LiberationSans
   lacks (no emoji, geometric marks replaced with Image shapes or words).

## Remaining / not verified

1. **Authored environment art** — Control Room and Automation Floor are styled UI,
   not modelled 3D machinery with props. The single largest gap vs. a "premium"
   bar. Convert `Theme`-built chrome to authored prefabs, add real reader / punch
   mechanism / indicator-tower geometry. (This is a modelling/art task — outside
   what could be done with primitives + procedural generation here.)
2. **iOS build** — not attempted (needs macOS/Xcode). Android is the verified
   mobile target.
3. **On-device test** — descoped by the project owner.
4. Long-form human playtesting for feel / difficulty tuning (subjective).

### Done since the first completion-phase report

- Per-stage literal-input QA for 01–12 (`PerStageInputTests`) — includes the
  failure state and illegal-edit rejection per stage.
- Responsive verification (`ResponsiveLayoutTests`) at 5 aspect ratios.
- Safe-area anchor math extracted to `SafeAreaPanel.TryComputeAnchors` + unit tests.
- Performance probe (`PerformanceProbeTests`) — game is per-frame allocation-clean.
- MCP bridge line left in `manifest.json` (editor-only, zero build impact,
  evidence in `PACKAGE_AUDIT.md`); one-line removal for a release checkout.

## Gotchas

- Input System package → `InputSystemUIInputModule`, not `StandaloneInputModule`.
- `unity_execute_code` = bare method body, no `using`, fully-qualified types.
- `unity_build` for Android exceeds the 120 s MCP queue timeout and returns a
  "failed" notification while the build actually continues and succeeds — watch
  the Editor.log / output path, not the MCP result.
- Editor drops out of Play mode on script recompile.
- `com.unity.test-framework.performance` injects INTERNET at build time —
  handled by `StripNetworkPermissions`.

## Last successful verification (2026-09-06)

`13 / 13 VALID` · EditMode `29 / 29` · PlayMode `16 / 16` · Android APK build
Success, permission-free · Windows build Success, launches clean · Editor
console clean.
