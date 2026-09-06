# Manual QA Matrix

_Last updated: 2026-09-06_

Verification method:
- **AP** - automated playthrough (`CampaignPlaythroughTests`) - real Game-scene
  flow, solution applied through the `CardEditSession` API, run every commit.
- **CL** - per-stage literal-click test (`PerStageInputTests`) - drives the stage
  through actual `Button.onClick`: tool buttons, card-row buttons, op-picker
  buttons, Load/Execute lever. Also checks the failure state and illegal-edit
  rejection per stage. Run every commit.
- **HS** - hands-on in the editor.

| Stage | Load | UI built | Illegal edit rejected | Failure state | Valid solution | Execute | Success | Rating | Save | Unlock | Replay | By |
|-------|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|----|
| 00 Operator Training | P | P | P | P | P | P | P | n/a | P | P (L01) | P | AP + HS |
| 01 First Program | P | P | P | P | P | P | P | PERFECT | P | P | P | AP + CL |
| 02 Turn Table | P | P | P | P | P | P | P | PERFECT | P | P | P | AP + CL |
| 03 Bad Hole | P | P | P | P | P | P | P | PERFECT | P | P | P | AP + CL |
| 04 Control Check | P | P | P | P | P | P | P | PERFECT | P | P | P | AP + CL |
| 05 Shift Line | P | P | P | P | P | P | P | PERFECT | P | P | P | AP + CL |
| 06 Cross Wire | P | P | P | P | P | P | P | PERFECT | P | P | P | AP + CL |
| 07 Reverse Feed | P | P | P | P | P | P | P | PERFECT | P | P | P | AP + CL |
| 08 Compact Program | P | P | P | P | P | P | P | PERFECT | P | P | P | AP + CL |
| 09 Copy Cycle | P | P | P | P | P | P | P | PERFECT | P | P | P | AP + CL |
| 10 Repeat Loop | P | P | P | P | P | P | P | PERFECT | P | P | P | AP + CL |
| 11 Dual Channel | P | P | P | P | P | P | P | PERFECT | P | P | P | AP + CL + HS |
| 12 Master Program | P | P | P | P | P | P | P (SYSTEM CERTIFIED) | PERFECT | P | n/a | P | AP + CL |

**13 / 13 stages verified through the player-facing UI.**

Per stage, `PerStageInputTests` explicitly:
- runs the unsolved card and asserts it halts with `PROGRAM HALTED` + a working
  RETRY that returns to editing with the edit count reset;
- selects a tool, taps an illegal target, and asserts a rejection toast
  (`FACTORY-LOCKED` / `ALREADY PUNCHED` / `IS EMPTY - USE PUNCH`) with 0 edits spent;
- solves the stage by clicking, asserting the exact intended edit count;
- runs it and asserts the win + `PERFECT PROGRAM`.

## Remaining

- Physical-device play - descoped by the project owner.
- Long-form human playtesting for feel / difficulty tuning (subjective, not a
  pass/fail gate) - recommended before store submission.
