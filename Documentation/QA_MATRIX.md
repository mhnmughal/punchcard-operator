# Manual QA Matrix

_Last updated: 2026-09-06_

Source of PASS marks:
- **AP** = automated playthrough (`CampaignPlaythroughTests.EveryStage_SolvesThroughTheGameScene`)
  — loads `Game.unity`, verifies UI built, applies the intended solution through
  the real `CardEditSession` edit API, Load + Execute, asserts on-screen success,
  PERFECT rating, persistence and unlock. Runs every commit.
- **HS** = hands-on spot check in the editor (literal button taps / tool picker).

Columns: Load · Card+tools shown · Invalid-edit rejected · Valid solution ·
Execute · Success · Rating · Save · Unlock · Replay

| Stage | Load | UI | Invalid | Solve | Exec | Win | Rating | Save | Unlock | Replay | Method | Notes |
|-------|------|----|---------|-------|------|-----|--------|------|--------|--------|--------|-------|
| 00 Operator Training | PASS | PASS | PASS | PASS | PASS | PASS | n/a (unrated) | PASS | PASS (L01) | PASS | AP + HS | coach steps, Execute gated behind the required Punch, tutorialComplete persists |
| 01 First Program | PASS | PASS | PASS¹ | PASS | PASS | PASS | PERFECT | PASS | PASS | PASS | AP | Punch a missing turn |
| 02 Turn Table | PASS | PASS | PASS¹ | PASS | PASS | PASS | PERFECT | PASS | PASS | PASS | AP | two missing moves |
| 03 Bad Hole | PASS | PASS | PASS¹ | PASS | PASS | PASS | PERFECT | PASS | PASS | PASS | AP | Patch one wrong turn |
| 04 Control Check | PASS | PASS | PASS¹ | PASS | PASS | PASS | PERFECT | PASS | PASS | PASS | AP | Punch + Patch |
| 05 Shift Line | PASS | PASS | PASS¹ | PASS | PASS | PASS | PERFECT | PASS | PASS | PASS | AP | Shift adjacent rows |
| 06 Cross Wire | PASS | PASS | PASS¹ | PASS | PASS | PASS | PERFECT | PASS | PASS | PASS | AP | Swap two rows |
| 07 Reverse Feed | PASS | PASS | PASS¹ | PASS | PASS | PASS | PERFECT | PASS | PASS | PASS | AP | Flip execution order |
| 08 Compact Program | PASS | PASS | PASS¹ | PASS | PASS | PASS | PERFECT | PASS | PASS | PASS | AP | Shift + Patch (2 edits) |
| 09 Copy Cycle | PASS | PASS | PASS¹ | PASS | PASS | PASS | PERFECT | PASS | PASS | PASS | AP | 3× Copy into a tight card |
| 10 Repeat Loop | PASS | PASS | PASS¹ | PASS | PASS | PASS | PERFECT | PASS | PASS | PASS | AP | Patch the visible Repeat dial |
| 11 Dual Channel | PASS | PASS | PASS¹ | PASS | PASS | PASS | PERFECT | PASS | PASS | PASS | AP + HS | two carriers, channel-tagged rows, A/B trays |
| 12 Master Program | PASS | PASS | PASS¹ | PASS | PASS | PASS (SYSTEM CERTIFIED) | PERFECT | PASS | n/a (last) | PASS | AP | dual channel + Repeat + Swap → certified finale |

¹ Invalid-edit rejection is covered generically: `CardEditSession` returns a
reason string for every illegal edit and this is unit-tested
(`CardEditSessionTests`); the playthrough asserts each legal edit succeeds. A
per-stage hands-on "tap an illegal target, read the toast" pass is still
outstanding for 01–10.

## Failure-state check

- Executing a wrong card halts with a plain-language reason and a RETRY button —
  verified in `SimulatorTests` (wall / off-board / no-target / repeat-no-prev)
  and hands-on on Level 0. Per-stage hands-on failure check: outstanding for 01–12.

## Outstanding for a full hands-on `13/13`

Per-stage literal-input runs of 01–12 (tap tools, pick ops, mis-tap to see the
rejection toast, force a failure, then solve). The flow, scoring, persistence and
unlock for all 13 are already machine-verified every commit.
