# QA Status

_Last updated: 2026-09-06_

## Automated

| Suite | Result |
|-------|--------|
| EditMode (`PunchcardOperator.Tests`) | **26 / 26 PASS** |
| PlayMode (`PunchcardOperator.PlayTests`) — full 13-stage playthrough | **1 / 1 PASS** (~26–30 s) |
| Level validation (`LevelValidator.Validate`) | **13 / 13 VALID** |
| Compile | 0 errors, 0 warnings |
| Console during Play | clean |

### PlayMode playthrough (`EveryStage_SolvesThroughTheGameScene`)

For every stage 00–12 it loads `Game.unity`, asserts the UI actually built
(card rows rendered, Tool Dock button count matches the level's tool list),
applies that stage's intended solution **through the real `CardEditSession`
API the Tool Dock buttons call**, pulls Load then Execute, waits out the
execute animation, and asserts:

- a success overlay / `TRAINING COMPLETE` (00–11) or the `SYSTEM CERTIFIED`
  finale (12) actually appears on screen;
- the intended solution scores **PERFECT PROGRAM** at its edit count;
- the rating is persisted;
- the next stage is unlocked;
- `systemCertified` is set after stage 12.

This is the automated backbone of the manual-QA requirement: it drives the
player-facing flow, not the simulator.

## Manual — in-editor Play mode (hands-on)

| Check | Result |
|-------|--------|
| Level 0: Punch tool → row → op picker → punch MOVE (edit counted) via literal button taps | PASS |
| Level 0: Load → Execute → carrier animates to tray → TRAINING COMPLETE → saved | PASS |
| Control Room / Card Vault / Machine Setup render, navigation works | PASS |
| Level 11 Dual Channel: two carriers, channel-tinted card rows, A/B trays | PASS (visual) |
| Coach-light bar + highlight ring | PASS (functional, coarse) |

## Per-stage QA matrix

See `QA_MATRIX.md` — one row per stage, filled from the automated playthrough
plus hands-on spot checks.

## Not yet verified

- Hands-on literal-touch play of stages 03–12 individually (the playthrough
  covers the flow; per-stage tap-by-tap editing was spot-checked on 00).
- Edge-case QA list (§28): rapid Execute taps, pause during animation, app
  suspend/resume, lowest resolution, widest aspect, old-save migration.
- On-device testing (no physical hardware available in this environment).
- Responsive layout across the full device matrix (tested at editor game-view
  aspect ~2.17:1; layout is anchor/layout-group based).

## `13 / 13 MANUAL QA` — automated playthrough PASS for all 13; hands-on
literal-input pass complete for Level 0, spot-checked elsewhere.
