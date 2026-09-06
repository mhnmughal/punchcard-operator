# Package & Third-Party Audit

_Last updated: 2026-09-06 - Unity 6000.3.19f1_

## Full dependency manifest (`Packages/manifest.json`)

```
com.anklebreaker.unity-mcp        (git)   editor-only build tool - see below
com.unity.ide.rider / visualstudio        editor-only IDE integration
com.unity.inputsystem            1.19.0   UI pointer / touch / Escape key
com.unity.render-pipelines.universal 17.3  renderer (standard UniversalRenderer)
com.unity.test-framework          1.6.0   EditMode + PlayMode suites
com.unity.ugui                    2.0.0    UI + TextMeshPro
com.unity.modules.{accessibility, animation, audio, imageconversion, imgui,
  jsonserialize, physics2d, screencapture, ui, uielements, umbra}   engine
```

`packages-lock.json` after resolve pulls back only `physics`, `terrain` and
`hierarchycore` as **URP / UGUI transitive dependencies** - not requestable to
remove without removing URP itself.

## Removed during cleanup (verified gone from `packages-lock.json`)

- **All networking:** `com.unity.modules.unitywebrequest` +
  `unitywebrequestassetbundle` + `unitywebrequestaudio` + `unitywebrequesttexture`
  + `unitywebrequestwww`
- **Analytics:** `com.unity.modules.unityanalytics`
- **All 2D packages:** `com.unity.2d.animation`, `2d.aseprite`, `2d.psdimporter`,
  `2d.sprite`, `2d.spriteshape`, `2d.tilemap`, `2d.tilemap.extras`, `2d.tooling`
- `com.unity.multiplayer.center`, `com.unity.visualscripting`, `com.unity.timeline`,
  `com.unity.collab-proxy`
- Engine modules: `ai`, `cloth`, `director`, `particlesystem`, `video`,
  `vectorgraphics`, `adaptiveperformance`, `androidjni`, `assetbundle`, `tilemap`,
  `terrainphysics`, `vehicles`, `vr`, `wind`, `xr`
- The URP renderer was switched from the template's **2D Renderer** to a standard
  `UniversalRendererData` (`Assets/Settings/UniversalRenderer.asset`), and the
  leftover `Renderer2D.asset` + template scenes were deleted.

## `com.anklebreaker.unity-mcp` (the MCP bridge)

Editor tooling used to build this project. **Not compiled into any player:**
both the Android APK and the iOS Xcode project are IL2CPP builds and their
`Il2CppOutputProject` contains only `PunchcardOperator.Runtime` from this
project's code - no `.Editor`, no `.Tests`, no `Anklebreaker`/`MCP` assembly.
Left in the manifest per the project owner's instruction to skip it; a one-line
removal produces a pristine release checkout with zero behaviour change.

## Prohibited categories - status

| Category | Present? |
|----------|----------|
| Ad SDKs / Analytics SDKs / active Unity Analytics | **No** - modules stripped; no `UnityEngine.Analytics` call sites |
| Remote config / cloud save / login / social SDKs | **No** |
| Crash-reporting services | **No** |
| `Assets/Plugins/` native plugins | **None** (folder does not exist) |
| Runtime network calls in game code | **None** - `grep` of `Assets/PunchcardOperator/Scripts` is clean |
| Reflection / `Process` / `DllImport` / `Application.OpenURL` / `PlayerPrefs` in game code | **None** - only `Resources.Load`, `Application.Quit`, one save file under `persistentDataPath` |

## Android APK (`aapt2`, IL2CPP ARM64 release)

- `uses-permission`: **none** - only the self-scoped, non-network AndroidX
  `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`
- package `com.operatorworks.punchcardoperator`, versionName 1.0.0, versionCode 1
- `native-code: arm64-v8a`, icon across all 6 density buckets
- `StripNetworkPermissions` post-processor removes `INTERNET` (injected by the
  transitive `com.unity.test-framework.performance` build hook)

## iOS Xcode project (`Build/iOS`)

- IL2CPP output: `UnityWebRequestModule`, `UnityAnalytics{,Common}Module`,
  `UnityConsentModule`, `VideoModule`, `AdaptivePerformanceModule` and the 2D
  runtime modules are **all gone** after the package strip.
- `GameCenterModule` / `MultiplayerModule` managed stubs remain in the engine
  core, but **no `GameKit.framework` is linked** → inert, cannot function.
- Frameworks: AVFoundation (audio session), AudioToolbox, CoreGraphics/Media/
  Text/Video, Foundation, GameController (Input System), Metal, QuartzCore,
  UIKit - plus **Security + SystemConfiguration**, which `UnityFramework` links
  by default (engine references `SCNetworkReachability` / keychain APIs the game
  never calls). **AVKit removed** by `IosBuildPostprocess`.
- `Info.plist`: no permission usage descriptions, no `NSAppTransportSecurity`,
  no `UIBackgroundModes`, no push entitlements. `ITSAppUsesNonExemptEncryption
  = false`. Landscape only.

## Action items before store submission

1. Remove `com.anklebreaker.unity-mcp` from the manifest for the release checkout.
2. On macOS: open `Build/iOS/Unity-iPhone.xcodeproj`, set a signing team, archive.
