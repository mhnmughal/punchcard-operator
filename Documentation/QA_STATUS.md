# QA Status

_Last updated: 2026-09-06_

## Automated

| Suite | Result |
|-------|--------|
| EditMode tests (`PunchcardOperator.Tests`, 26 tests) | **26 / 26 PASS** (Unity Test Runner, run via MCP) |
| Level validation (`LevelValidator.Validate`) | **13 / 13 VALID** — all campaign solution cards simulator-verified |
| Compile | 0 errors, 0 warnings |
| Console during Play (Game scene, Level 0) | clean |

Test coverage: simulator (straight run, turn, wall block, off-board, end-off-target,
Repeat, Repeat-no-prev, Flip, dual-channel both-must-reach, per-channel Repeat);
CardEditSession (Punch fill/occupied/locked, Shift, Swap, Reset); RatingRules;
SaveSystem (fresh defaults, corrupt-file fallback, round-trip, no rating
downgrade, tutorial gate); full 13-level campaign validation.

## Manual — in-editor Play mode

| Check | Result |
|-------|--------|
| Level 0 loads, board + card + Tool Dock render | PASS |
| Level 0: select Punch → row → op picker → punch MOVE (edit counted) | PASS |
| Level 0: Load Card → Execute → carrier animates to tray | PASS |
| Level 0: "TRAINING COMPLETE" → tutorial completion saved | PASS |
| Coach-light bar shows step text and highlight ring | PASS (coarse) |
| Menu scene: Control Room / Card Vault / Machine Setup build | PASS (hierarchy + no errors) |

## Not yet verified

- Levels 01–12 played through the actual UI (data + solutions are validated;
  hands-on run of each stage in the scene is pending).
- Edge-case QA list (rapid Execute taps, pause during animation, app suspend/
  resume, lowest resolution, widest aspect, old-save migration).
- Device testing (any real iOS / Android hardware).
- Production build.
- Safe-area behaviour on a real notched device (SafeAreaPanel is implemented and
  applies `Screen.safeArea`, not yet seen on hardware).

## `13 / 13 MANUAL QA PASS` — NOT YET. Currently 1 / 13 (Level 0).
