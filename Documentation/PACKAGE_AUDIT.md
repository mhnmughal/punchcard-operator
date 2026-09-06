# Package & Third-Party Audit

_Last updated: 2026-09-06 — Unity 6000.3.19f1_

## Runtime dependencies actually used

| Package | Why | Network / telemetry |
|---------|-----|---------------------|
| com.unity.render-pipelines.universal | URP renderer | No |
| com.unity.ugui (incl. TextMeshPro) | all UI + text | No |
| com.unity.inputsystem | UI pointer/touch input module | No |
| engine modules (audio, ui, uielements, jsonserialize, imageconversion, imgui, particlesystem, physics, physics2d, animation, director, screencapture, video, vectorgraphics, tilemap, 2d.*, ai, cloth, umbra, unitywebrequest*) | engine defaults | No |

## Removed during cleanup

`com.unity.multiplayer.center`, `com.unity.visualscripting`, `com.unity.timeline`,
`com.unity.collab-proxy` (packages), and engine modules `terrain`,
`terrainphysics`, `vehicles`, `vr`, `wind`, `xr` — none referenced by the game.
Compile + 26/26 EditMode + 1/1 PlayMode re-verified after each removal.

## Editor-only, not compiled into the player

- `com.anklebreaker.unity-mcp` — the MCP bridge used to build this project.
  Editor assembly only (lives under the package's `Editor/` tree); Unity never
  includes editor assemblies in a player build. **Evidence:** the Android APK is
  an IL2CPP build — all managed code is in `libil2cpp.so`; there is no Managed/
  DLL folder to leak an editor assembly into, and the editor assembly is not
  compiled for the player target at all. Removing the manifest line is a
  one-liner for a pristine release checkout and has zero build/runtime impact.
- `com.unity.ide.rider`, `com.unity.ide.visualstudio` — IDE integration, editor only.
- `com.unity.test-framework` (+ transitive `com.unity.test-framework.performance`)
  — needed for the EditMode/PlayMode suites this project ships. The performance
  package's build hook injects `android.permission.INTERNET`; a build
  post-processor (`StripNetworkPermissions`) removes it from the merged manifest.
  **Verified:** `aapt2 dump permissions` on the shipped APK → no INTERNET, no
  network permissions (see OFFLINE_AUDIT.md).

## Prohibited categories — status

| Category | Present? |
|----------|----------|
| Ad SDKs | No |
| Analytics SDKs / active Unity Analytics use | No — no `UnityEngine.Analytics` call sites; `com.unity.modules.unityanalytics` is a dormant engine module |
| Remote config / cloud save / login / social SDKs | No |
| Crash-reporting services | No |
| `Assets/Plugins/` native plugins | None (folder does not exist) |
| Runtime network calls in game code | None |

## APK verification (`aapt2`, IL2CPP ARM64 release)

- `uses-permission`: **none** (only the self-scoped, non-network AndroidX
  `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`)
- package `com.operatorworks.punchcardoperator`, versionName 1.0.0, versionCode 1
- `native-code: arm64-v8a`
- app icon present across all 6 density buckets

## Build warnings

The Android build log shows ~20 third-party **shader** warnings from URP / SRP
core (`Terrain/Lit` subshader removal for GLES; `min16float` conversions in
`com.unity.render-pipelines.core` APV sampling shaders) plus 3 informational TMP
IL2CPP "method given its own cpp file" notes. **None originate from project
code**; the game uses no terrain and no adaptive probe volumes. The Unity Editor
Console is clean (0 warnings). These are non-actionable engine warnings.
