# Package & Third-Party Audit

_Last updated: 2026-09-06 — Unity 6000.3.19f1_

## Runtime dependencies actually used by the game

| Package | Why | Network / telemetry |
|---------|-----|---------------------|
| com.unity.render-pipelines.universal | URP renderer | No |
| com.unity.ugui (incl. TextMeshPro) | all UI + text | No |
| com.unity.inputsystem | UI input module (touch/pointer) | No |
| com.unity.modules.* (audio, ui, jsonserialize, imageconversion, …) | engine built-ins | No |

## Present but editor-only / not shipped in player

- `com.anklebreaker.unity-mcp` — the MCP bridge used to build this project. **Editor
  assembly only; must be removed or confirmed stripped before release.**
- `com.unity.collab-proxy`, `com.unity.multiplayer.center`, `com.unity.ide.rider`,
  `com.unity.ide.visualstudio` — editor tooling, not compiled into the player.
- `com.unity.test-framework` — tests, editor-only.
- `com.unity.visualscripting`, `com.unity.timeline`, 2D packages — unused by the
  game; candidates for removal.

## Prohibited categories — status

| Category | Present? |
|----------|----------|
| Ad SDKs | No |
| Analytics SDKs (Firebase, GameAnalytics, Unity Analytics runtime use) | No — `com.unity.modules.unityanalytics` is a dormant built-in module, no code calls it |
| Remote config / cloud save / login / social SDKs | No |
| Crash reporting services | No |
| `Assets/Plugins/` native plugins (iOS/Android) | None (folder does not exist) |
| Runtime `UnityWebRequest` / socket / HTTP use in game code | None (`Sfx`, save system, gameplay are all local) |

## Action items before release

1. Remove `com.anklebreaker.unity-mcp` from `Packages/manifest.json`.
2. Remove unused packages (visualscripting, timeline, 2D animation/aseprite/psd,
   multiplayer.center) to shrink the build and dependency surface.
3. Re-run this audit on the actual player build (check `Managed/` DLLs and, for
   Android, the merged manifest / Gradle deps; for iOS, the Pods / frameworks).
