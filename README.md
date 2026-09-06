# Punchcard Operator

A tactile programming puzzle game for mobile. You are the operator of a punch‑card
automation machine: repair the instruction card, drop it into the reader, pull the
lever, and watch the carrier cross the floor to its output tray.

Built in **Unity 6** (URP), **fully offline** - no ads, no analytics, no network,
no third‑party runtime SDKs. Every sound is synthesised at runtime and every
texture is generated in‑engine.

---

## The campaign

**13 stages total: Level 0 + Levels 01-12**, grouped into three Program Banks.

| | Bank | Stages | Teaches |
|---|------|--------|---------|
| - | Operator Training | `00` | the full loop, hands‑on, with machine coach‑lights |
| A | Basic Control | `01`-`04` | Move · Left · Right · Punch · Patch |
| B | Card Manipulation | `05`-`08` | Shift · Swap · Flip · editing efficiency |
| C | Master Control | `09`-`12` | Copy · Repeat · Dual Channel · mastery |

Ratings per stage: **ACCEPTED → COMPACT CARD → PERFECT PROGRAM** (by edit count).
Completing Level 12 triggers the **SYSTEM CERTIFIED** finale.

Every campaign solution is verified by the simulator (`13 / 13 VALID`), and every
stage is play‑tested through the real UI by the automated test suites.

---

## How it works

A punch card holds one instruction per row. The reader runs them top‑to‑bottom:

- **MOVE** - the carrier steps one cell the way it faces
- **LEFT / RIGHT** - the carrier turns in place
- **REPEAT ×n** - re‑runs the row above it, `n` times (shown on the dial)
- **FLIP** feeds the card so the reader runs it bottom‑to‑top
- **Dual Channel** cards drive two machines at once - both must park on their trays

You edit the card from the **Tool Dock**: Punch (fill an empty row), Patch
(re‑cut a wrong one), Shift (slide a row), Swap (exchange two rows), Flip, Copy.
Illegal edits are rejected with a plain‑language reason.

---

## Project layout

```
Assets/PunchcardOperator/
  Scripts/Runtime/         gameplay + UI (one asmdef)
    PunchcardCore.cs        simulator, edit session, rating rules
    PunchCardLevelDefinition.cs / LevelDatabase.cs   ScriptableObject level data
    SaveSystem.cs            offline JSON save + progression
    LevelValidator.cs        13/13 campaign validation
    GameController.cs         the stage loop
    MenuController.cs         Control Room / Card Vault / Settings / How To Play
    BootController.cs · Fade.cs · Sfx.cs · SafeAreaPanel.cs · AppShell.cs
  Scripts/Editor/          PunchcardLevelBuilder, IconGenerator, build post-processors
  Scripts/Tests/           EditMode (PunchcardOperator.Tests)
  Scripts/Tests/PlayMode/  PlayMode  (PunchcardOperator.PlayTests)
  Levels/                  generated level assets
  Resources/               LevelDatabase.asset
  Scenes/                  Boot → Menu → Game
  Art/                     procedurally generated icon
Documentation/             implementation, audit, QA matrix, provenance, package audit
```

Level design lives in code (`PunchcardLevelBuilder.cs`); the `.asset` files are
regenerated output. Menu: **Punchcard Operator → Rebuild All Levels**.

---

## Building

Requires **Unity 6000.3.19f1** (or compatible).

- **Android** - `File → Build Settings → Android`, IL2CPP + ARM64. A build
  post‑processor strips network permissions; the APK requests none.
- **iOS** - builds an Xcode project; open it on macOS, set a signing team, archive.
- **Desktop** - Windows/macOS/Linux standalone work out of the box.

Scene order: `Boot`, `Menu`, `Game`.

---

## Tests

Run from **Window → General → Test Runner**.

| Suite | What it covers |
|-------|----------------|
| EditMode (`PunchcardOperator.Tests`) | simulator, edit session, ratings, save system, safe‑area math, full campaign validation |
| PlayMode (`PunchcardOperator.PlayTests`) | 13‑stage playthrough, per‑stage literal‑click QA (incl. failure + rejection paths), responsive layout at 5 aspect ratios, GC‑allocation probe, boot routing + pause |

Current: **29 / 29 EditMode · 19 / 19 PlayMode · 13 / 13 level validation**, clean
console.

---

## Status

Feature‑complete. See `Documentation/AUDIT.md` for the full item‑by‑item report
and `Documentation/AGENT_CONTINUITY.md` for the working notes.
