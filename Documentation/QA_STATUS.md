# QA Status

_Last updated: 2026-09-06_

## Automated

| Suite | Result |
|-------|--------|
| EditMode (`PunchcardOperator.Tests`) | **29 / 29 PASS** |
| PlayMode (`PunchcardOperator.PlayTests`) | **19 / 19 PASS** |
| Level validation (`LevelValidator.Validate`) | **13 / 13 VALID** |
| Compile | 0 errors, 0 warnings |
| Editor console during Play | clean |

### PlayMode tests (19)

| Test | Covers |
|------|--------|
| `CampaignPlaythroughTests.EveryStage_SolvesThroughTheGameScene` | loads all 13 stages, applies each solution through the real edit API, Load+Execute, asserts success/rating/persistence/unlock, `systemCertified` after 12 |
| `PerStageInputTests.EveryCampaignStage_PlayableByClicking` ×12 | stages 01–12, driven by **literal `Button.onClick`** — tool buttons, card-row buttons, op-picker buttons, Load/Execute lever. Per stage: unsolved card halts with a reason + working RETRY; an illegal edit is rejected with a toast and costs 0; the stage solves by clicking at the intended edit count; it wins and scores PERFECT |
| `ResponsiveLayoutTests` ×2 | Menu + Game (levels 04, 11) at 20:9 / 19.5:9 / 16:9 / 4:3 / 1334×750 — nothing off-screen, tap targets ≥ 30 px, the Status Rail / board / card / Tool Dock bands never overlap, console clean |
| `PerformanceProbeTests` | GC-allocation probe on the editing screen + execute animation vs. an editor baseline |

## Manual — in-editor Play mode (hands-on)

| Check | Result |
|-------|--------|
| Level 0: Punch tool → row → op picker → punch MOVE via literal taps | PASS |
| Level 0: Load → Execute → carrier animates to tray → TRAINING COMPLETE → saved | PASS |
| Control Room / Card Vault / Machine Setup render + navigation | PASS |
| Level 11 Dual Channel visual (two carriers, channel-tinted rows, A/B trays) | PASS |

## Per-stage QA matrix

`QA_MATRIX.md` — one row per stage. **13 / 13** stages now verified through the
player-facing UI (Level 0 hands-on; 01–12 via the per-stage literal-click test
which also checks the failure state and illegal-edit rejection per stage).

## Performance

`PerformanceProbeTests` (level 12, most complex board):

```
baseline (empty scene, editor + MCP bridge):  ~262 KB/frame
idle editing screen:                          ~249 KB/frame  (delta -13 KB)
execute animation:                            ~248 KB/frame  (delta -14 KB)
```

The game adds **no measurable per-frame GC allocation** over the editor baseline —
`GameController.Update`, the execute coroutine and the tween helpers are
allocation-free. The ~262 KB baseline is editor + MCP-bridge churn that does not
exist in a player build. Editor frame times are not device-representative and are
not asserted; the meaningful mobile signal (per-frame heap allocation → GC
hitch) is clean.

## Not verified

- **Authored 3D environment** — Control Room / Automation Floor are styled UI,
  not modelled machinery. Functionally complete; art depth is the open item.
- **On-device run** — descoped by the project owner.
- **iOS build** — needs macOS/Xcode; Android is the verified mobile target.
- Device-level safe-area rendering (the anchor math is unit-tested; not seen on
  a physical notched screen).
