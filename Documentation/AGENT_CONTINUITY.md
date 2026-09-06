# Agent Continuity

_Last updated: 2026-09-06_

Read this + `IMPLEMENTATION.md` before continuing. Do not rediscover the project
from chat history.

## Current state

Green-field Unity 6000.3.19f1 URP project. Built so far:

- **Core architecture — done & tested.** Simulator, edit session, level data
  (ScriptableObjects), save/progression, validator. 26 EditMode tests pass.
  `Punchcard Operator/Rebuild All Levels` menu item regenerates all 13 level
  assets from `PunchcardLevelBuilder.cs` and validates them (13/13).
- **All 13 levels designed as data** with simulator-verified solutions.
- **Playable vertical slice — done.** `Game.unity` runs any level id; full loop
  (edit → load → execute → animate → score → save → advance) verified in Play
  mode for Level 0. `Menu.unity` has Control Room / Card Vault / Machine Setup.
- **Audio:** runtime-synthesised SFX only (`Sfx.cs`). No music.

## Architecture decisions made

1. One `PunchcardOperator.Runtime` asmdef holds gameplay + UI MonoBehaviours
   (references TMP + UGUI). Editor tooling and tests are separate asmdefs.
2. Level design lives in code (`PunchcardLevelBuilder`) so it is reviewable and
   every solution is simulator-checked on rebuild. The `.asset` files are
   generated output.
3. `LevelDatabase.asset` lives in `Resources/` so the build can load it without a
   scene reference.
4. Gameplay UI is composed at runtime from `Theme` builders under an authored
   `SafeAreaPanel` root. **Known tradeoff** — see debt list.
5. Shift = swap with the adjacent row (empty or not); Swap = any two rows.
6. `Repeat` is per-channel and its count is always visible on the row.
7. Success requires the carrier to *finish* on the target (passing through and
   leaving is a fail) — deliberate, keeps the rule legible.

## Remaining tasks (roughly in order)

1. **Vertical-slice quality gate** (directive §17): play Levels 0–2 through the
   real UI, fix UI hierarchy / touch targets / overlap / safe area, tighten the
   coach-light system, verify originality of layout & terminology.
2. **Scene-authoring pass:** turn the Control Room and Automation Floor into
   authored environments with real props (reader, punch mechanism, Status Rail,
   Tool Dock, Machine Service/Setup panels, indicator tower). Convert gameplay
   chrome from runtime-built to prefabs.
3. **Play-test & polish Levels 3–12** one at a time (they are data-valid; confirm
   each is solvable and fun in the scene, tune rating targets).
4. **System Certified sequence** (directive §15): indicator tower, bank
   illumination, reader finale, master-status plate, `REVIEW PROGRAMS` /
   `RETURN TO CONTROL ROOM`.
5. **Animation / feel pass:** punch + chad, patch, shift/swap/flip/copy, card
   insertion/removal, reader travel, machine turn, success/failure.
6. **Audio pass:** balance, anti-repetition, optional licence-clean ambience/music
   (update `AUDIO_PROVENANCE.md`).
7. **Game icon** (directive §31) + Player Settings: package name, orientation
   lock (landscape — current layout assumes it), min SDK/iOS version, splash.
8. **Edge-case QA** (directive §28) + full `13/13` manual QA.
9. **Package cleanup:** remove `com.anklebreaker.unity-mcp` and unused packages
   before build (`PACKAGE_AUDIT.md`).
10. **Production build** for the target platform; verify it completes and runs
    offline; re-run the third-party audit on the build output.
11. Keep `AUDIT.md` honest; flip `PROJECT STATUS` only when nothing is FAIL/NOT
    VERIFIED.

## Gotchas

- The project uses the Input System package — EventSystems need
  `InputSystemUIInputModule`, not `StandaloneInputModule`.
- `unity_execute_code` takes a bare method body: no `using` directives, fully
  qualify `UnityEngine.UI.*`, `UnityEngine.EventSystems.*`, `PunchcardOperator.*`.
- `unity_screenshot_game` writes on the next render and can lag a few seconds; a
  camera capture (`unity_graphics_game_capture`) can't see a Screen-Space-Overlay
  canvas.
- Editor sometimes drops out of Play mode when a script recompiles.

## Last successful validation

`13 / 13 VALID` and `26 / 26` EditMode tests — 2026-09-06.
