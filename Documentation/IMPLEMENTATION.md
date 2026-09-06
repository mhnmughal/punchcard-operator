# Punchcard Operator - Implementation Summary

_Last updated: 2026-09-06_

Tactile programming puzzle game. The player repairs punch cards for an automation
machine: a card holds instruction rows, the reader runs them top-to-bottom, and a
carrier crosses a grid toward its output tray.

## Scope (authoritative)

**13 playable stages total: Level 0 (Operator Training) + Levels 01-12.**
The older 42-level / 7-chapter / "Level 1 is the tutorial" / "Level 42 finale"
material is obsolete and is not present anywhere in this project (it was a
green-field build - there was nothing to migrate).

## Architecture

```
Assets/PunchcardOperator/
  Scripts/Runtime/   PunchcardOperator.Runtime.asmdef  (refs: Unity.TextMeshPro, UnityEngine.UI)
    PunchcardCore.cs          enums, CardRow, PunchCard, GridDef, MachineConfig,
                              PunchCardSimulator, CardEditSession, RatingRules
    PunchCardLevelDefinition.cs   ScriptableObject, one per stage
    LevelDatabase.cs          ScriptableObject list + lookups (lives in Resources/)
    SaveSystem.cs             SaveData + JSON persistence + ProgressionManager
    LevelValidator.cs         13/13 campaign validation, simulator-checked solutions
    AppShell.cs               AppState (cross-scene), Theme (palette + UGUI builders)
    SafeAreaPanel.cs          notch / safe-area handling
    Sfx.cs                    procedurally synthesised interaction sounds
    GameController.cs         the stage loop (build floor + card, edit, execute, score)
    MenuController.cs         Control Room / Card Vault / Machine Setup
  Scripts/Editor/    PunchcardOperator.Editor.asmdef
    PunchcardLevelBuilder.cs  "Punchcard Operator/Rebuild All Levels" - regenerates
                              all 13 level assets from code and runs the validator
  Scripts/Tests/     PunchcardOperator.Tests.asmdef  (EditMode, 26 tests)
    PunchcardTests.cs
  Levels/            Level_00..Level_12 .asset
  Resources/         LevelDatabase.asset
  Scenes/            Menu.unity, Game.unity
```

### Simulation model (`PunchCardSimulator`)

- Grid with walls. Carrier has position + facing (N/E/S/W).
- Ops: `Move` (step forward), `TurnLeft`, `TurnRight`, `Repeat` (re-runs that
  channel's previous op N times - N is stored on the row and shown to the player),
  `Empty` (skipped).
- `PunchCard.flipped` reverses execution order (Level 07 mechanic).
- Dual channel: rows are tagged Channel A / B, each drives its own carrier; both
  must finish on their target. `Repeat` is per-channel.
- Failure returns a plain-language reason ("MACHINE A HIT A WALL (card row 4)").
- Returns an ordered `SimStep` list that `GameController` plays back as animation.

### Editing (`CardEditSession`)

Punch (fill empty row), Patch (rewrite a punched row), Shift (swap with adjacent
row), Swap (exchange any two rows), Flip (toggle card direction), Copy (duplicate
a row into an empty one). Every rejected edit returns a reason string. `EditCount`
drives the rating.

### Ratings (`RatingRules`)

`ACCEPTED` (solved) → `COMPACT CARD` (edits ≤ compact target) → `PERFECT PROGRAM`
(edits ≤ perfect target). Level 0 is unrated. Best rating never downgrades.

### Progression (`ProgressionManager` + `SaveSystem`)

Tutorial gates Level 01. Then strictly sequential 01→12. Completing 12 sets
`systemCertified`. JSON at `Application.persistentDataPath`, atomic write, safe
defaults on missing/corrupt file. Nothing networked.

## Level design - why each stage exists

| ID | Name | Bank | Introduces / tests |
|----|------|------|--------------------|
| 00 | Operator Training | - | the full loop, hands-on, coach-lit |
| 01 | First Program | A | Move + one missing turn on an L route |
| 02 | Turn Table | A | multi-move sequencing, two gaps, no handholding |
| 03 | Bad Hole | A | Patch - one wrongly punched turn drives off the board |
| 04 | Control Check | A | Punch + Patch together; first real mastery check |
| 05 | Shift Line | B | Shift - right instruction, one row too early |
| 06 | Cross Wire | B | Swap - two turns wired in the wrong order |
| 07 | Reverse Feed | B | Flip - card punched back-to-front |
| 08 | Compact Program | B | optimisation - shift+patch (2) beats 3 patches |
| 09 | Copy Cycle | C | Copy - fill a tight card from one MOVE, no Punch tool |
| 10 | Repeat Loop | C | Repeat - set the visible dial so the carrier stops on the tray |
| 11 | Dual Channel | C | one card, Machine A + Machine B, both must park |
| 12 | Master Program | C | dual channel + Repeat + a Swap; culmination |

All 12 campaign solutions are authored in `PunchcardLevelBuilder` and verified by
the simulator inside `LevelValidator` - current result **13 / 13 VALID**.

## Screen flow

`Boot` (power-on, first-launch routing) → `Menu` (Control Room hub · Card Vault ·
Settings · How To Play, cross-faded) → `Game` (the stage
loop: brief → edit → load → execute → score → finale). Every scene change
fades through black. In-game: PAUSE (Status Rail / Escape / app-focus) →
Resume / Restart / Sound / Quit.

## Build & release state

- **Android:** `Build/Android/PunchcardOperator.apk` - IL2CPP ARM64 release,
  ~30 MB, Build Result: Success. `aapt2` verified: **no permissions**. Icon on
  every density bucket. `com.operatorworks.punchcardoperator` (placeholder id),
  landscape, minSdk 24, Medium stripping + `link.xml`.
- **Windows:** standalone build succeeds; launches through Boot → route with no
  exceptions (also exercises the level-DB / save deserialization path).
- **iOS:** full Xcode project generated at `Build/iOS` and audited (no network
  frameworks, hardened `Info.plist`, export compliance, AVKit removed, both
  landscape). Compiling the `.ipa` requires Xcode on macOS.
- `StripNetworkPermissions` (Android post-processor) removes `INTERNET`;
  `IosBuildPostprocess` sets `ITSAppUsesNonExemptEncryption=false` + drops AVKit.

## Known limitations / debt

- **UI is composed at runtime** from `Theme` builders under a `SafeAreaPanel`
  root - styled as a machine console (bolted panels, card reader, indicator
  lamps, animated punch head, boot power-on, cassette vault) but still 2D UI,
  not a modelled 3D scene with camera + lighting.
- Coach-light system is functional but coarse (keyword-driven ring placement,
  advances on the matching action, hard-gates only Execute-before-Punch).
- SFX are synthesised (deliberate - zero licence risk); no music track.
- On-device test descoped by the owner; iOS `.ipa` needs macOS/Xcode.
- `GameCenter` / `Multiplayer` engine stubs remain in the iOS IL2CPP output
  (Unity always ships them) but are inert - no GameKit framework, no call sites.

See `AUDIT.md` for item-by-item status and `AGENT_CONTINUITY.md` for the
remaining task list.
