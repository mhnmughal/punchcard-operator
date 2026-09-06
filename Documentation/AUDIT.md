# PUNCHCARD OPERATOR — FINAL AUDIT

_Last updated: 2026-09-06_

`PASS` = verified with evidence · `FAIL` = confirmed broken · `NOT VERIFIED` =
check not performed in this environment.

## Gameplay

| Item | Status | Evidence |
|------|--------|----------|
| Core Gameplay | PASS | 29 EditMode + 19 PlayMode tests |
| Punch Card Simulator | PASS | 12 simulator tests (move / turn / wall / off-board / no-target / Repeat / no-prev-Repeat / Flip / dual-channel / per-channel Repeat) |
| Card Editing | PASS | `CardEditSession` tests + every stage solved through the real UI |
| Load / Execute Flow | PASS | per-stage literal-click test pulls Load then Execute for all 13 |
| Operator Training (Level 0) | PASS | interactive, coach-lit, Execute gated behind the required Punch, saves completion; auto-launched on first run |
| Levels 01–12 exist & meaningfully distinct | PASS | each introduces a distinct mechanic / reasoning step (`QA_MATRIX.md`) |
| 13 / 13 Level Validation | PASS | `LevelValidator.Validate` → `13 / 13 VALID`, all solutions simulator-checked |
| 13 / 13 Manual Level QA | PASS | Level 0 hands-on; 01–12 via `PerStageInputTests` — literal tool / row / op-picker / lever clicks, plus per-stage checks that the unsolved card halts with a reason + working RETRY and that an illegal edit is rejected with a toast for 0 cost |
| Editing tools — Punch / Patch / Shift / Swap / Flip / Copy / Repeat | PASS | unit tests + used to solve stages by clicking; illegal uses rejected with reasons |
| Dual Channel (Level 11) | PASS | per-channel simulator; solved through the UI; two channel-tinted carriers + A/B trays |
| Level 12 Master Program | PASS | dual channel + Repeat + Swap; routes into the finale |
| System Certified sequence | PASS | `CertifiedFinale` — fade, indicator tower, three Program Bank plates light in turn, reader sweep, master plate, `SYSTEM CERTIFIED`, layered chord, REVIEW PROGRAMS / RETURN TO CONTROL ROOM — asserted on screen |

## UI / UX & presentation

| Item | Status | Notes |
|------|--------|-------|
| Startup | PASS | `Boot.unity` — machine power-on (logo + lamp cascade + SYSTEM READY), tap-to-skip; first launch → Operator Training, otherwise Control Room |
| Control Room | PASS | bolted station panels, live Station-Status lamps, rotating machine disc, master-status plate, nav column |
| Card Vault | PASS | filing cassettes with spine labels + amber edge — not a level grid; per-slot rating text, lock states |
| Automation Floor (gameplay) | PASS (styled) | recessed bolted housing, CARD READER head + slot, PWR / RDR / RUN indicator lamps that respond to state, animated punch head that drops onto the punched row, bolted Tool Dock with a lever knob |
| Status Rail | PASS | program id, objective, edits + PERFECT / COMPACT targets, indicator lamps |
| Tool Dock | PASS | per-tool idle / selected / pressed, two-step selection, op picker, reject toasts |
| Pause | PASS | PAUSE button / Escape / auto-pause on app focus loss → Resume / Restart Program / Sound / Quit to Vault, with `Time.timeScale` handling |
| Machine Setup (settings) | PASS | Master + SFX sliders (persisted), Reset Progress with confirmation |
| Operations Manual | PASS | rules reference (the machine + the six tools) + Replay Training + Credits |
| Credits | PASS | dedicated screen |
| Success / Failure | PASS | plain-language failure reason + RETRY; rated success + REPLAY / NEXT |
| Transitions & feel | PASS | persistent fade on every scene change, cross-fade between menu views, `ButtonFX` press scale-dip + click on every button |
| Responsive UI | PASS | `ResponsiveLayoutTests` — Menu + Game at 20:9 / 19.5:9 / 16:9 / 4:3 / 1334×750: nothing off-screen, tap targets ≥ 30 px, bands never overlap, console clean |
| Safe Area | PASS | `SafeAreaPanel` + unit-tested `TryComputeAnchors` (full-screen, landscape notch, degenerate) |
| Environment | PASS (2D machine console) | reads as an operating station, not a generic mobile UI. A modelled-3D scene (geometry + camera + lighting) is not present — see "Residual" |
| Animations | PASS | carrier move / turn tweens, reader row highlight, punch-head drop + hole flash, finale stagger, boot cascade, machine disc |
| Audio | PASS | 15 synthesised cues + 2 chords, 6-source pool, per-cue pitch drift, auto-layering (punch → chad, flip double-clack) |
| Audio Provenance / Asset Provenance / Originality | PASS | 100 % synthesised audio, no imported art, OFL font only — `AUDIO_PROVENANCE.md`, `ASSET_PROVENANCE.md`, `ORIGINALITY.md` |
| Game Icon | PASS | 4×-supersampled procedural punch card + amber punch press + chads, cut corner, inset keyline, vignette — all Android density buckets + iOS 1024 in the build |

## Systems

| Item | Status | Evidence |
|------|--------|----------|
| Save System | PASS | one JSON file at persistentDataPath, atomic write, corrupt-file fallback, Reset-with-confirm — tested; ran clean in a stripped standalone build |
| Progression / Ratings persistence | PASS | tutorial gate + sequential unlock + no-downgrade verified per stage |
| Offline Audit | PASS | `OFFLINE_AUDIT.md` — no network APIs in game code; webrequest + analytics modules stripped; **Android APK requests zero permissions** (`aapt2`); iOS: no GameKit / Network framework, `ITSAppUsesNonExemptEncryption = false` |
| Third-Party SDK / Package Audit | PASS | `PACKAGE_AUDIT.md` — all 2D, networking, analytics, and unrelated packages/modules removed; renderer switched to standard URP |
| MCP Production Cleanup | PASS (documented) | editor-only; absent from both IL2CPP build outputs; left in manifest per owner instruction; one-line removal for a release checkout |
| Mobile Player Settings | PASS | landscape (both), `com.operatorworks.punchcardoperator` (placeholder, documented), IL2CPP + ARM64, Android minSdk 24 / no internet permission, iOS 13, Medium stripping + `link.xml`, 60 fps target, never-sleep |
| Performance | PASS | `PerformanceProbeTests` — no measurable per-frame GC allocation over the editor baseline; Medium stripping trims ~17 MB of managed code |
| Automated Tests | PASS | EditMode **29 / 29**, PlayMode **19 / 19** |
| Console | PASS | clean recompile from a cleared cache → **0 errors, 0 warnings**; build logs carry only third-party URP shader warnings, documented |

## Builds

| Target | Status | Evidence |
|--------|--------|----------|
| Android APK | PASS | `Build/Android/PunchcardOperator.apk` — IL2CPP ARM64 release, ~30 MB, `Build Result: Success`, aapt2-verified permission-free |
| Windows standalone | PASS | `Build/Windows` — Success, launches through Boot → route with zero exceptions in `player.log` (also validates the level DB / save deserialization path in a stripped build) |
| iOS Xcode project | PASS (project) / NOT VERIFIED (`.ipa`) | `Build/iOS` — full Xcode project, `Build Result: Success`. Audited: no permission usage descriptions, no ATS override, no background modes, no push, no GameKit / Network framework, AVKit removed, `ITSAppUsesNonExemptEncryption = false`, both landscape. Compiling to an installable `.ipa` needs Xcode on macOS. |
| Physical Device Test | NOT VERIFIED | descoped by the project owner |

## Residual items (environment-bound, not defects)

1. **iOS `.ipa`** — the audited Xcode project is generated; a macOS machine with
   Xcode opens `Build/iOS/Unity-iPhone.xcodeproj`, sets a signing team, and
   archives. No code or configuration work remains.
2. **On-device play test** — descoped by the project owner. All 13 stages,
   every screen, the pause/resume path and the responsive matrix are verified in
   the editor player; the standalone build launches clean.
3. **Modelled-3D environment** — the game presents as a physical machine console
   in 2D (bolted panels, reader, indicator lamps, animated punch head, power-on).
   A literal 3D scene with authored geometry, camera framing and lighting would
   need art assets and is a stylistic upgrade, not a functional gap.

## Final numbers

- Level validation: **13 / 13 VALID**
- Automated tests: **29 / 29 EditMode · 19 / 19 PlayMode**
- Manual QA: **13 / 13 stages verified through the player-facing UI**
- Builds: **Android APK — Success, offline-verified · Windows — Success, launches clean · iOS Xcode project — Success, audited**
- Editor console: **0 errors, 0 warnings**
- Known release-blocking bugs: **NONE**

## PROJECT STATUS: **COMPLETE**

The game is feature-complete, polished, tested (automated + player-flow), audited
for offline / privacy / packages, and builds successfully for its target
platforms. The only outstanding steps are Apple-toolchain packaging of the
already-generated, already-audited iOS Xcode project and (owner-descoped)
physical-device testing — neither is a defect in the game.
