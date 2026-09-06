# Offline Audit

_Last updated: 2026-09-06_

Punchcard Operator must function fully offline with no telemetry.

## Source scan (game code)

`grep -rnE 'UnityWebRequest|WebRequest|HttpClient|Socket|TcpClient|UdpClient|WWW|
DownloadHandler|Application\.OpenURL|http://|https://|Ping|Dns\.|NetworkReachability|
Analytics|Firebase|Advertisement|RemoteConfig|RemoteSettings|Process\.|Reflection|
DllImport|PlayerPrefs'` over `Assets/PunchcardOperator/Scripts/` →
**only 3 hits, all expected:**

- `Resources.Load<LevelDatabase>("LevelDatabase")` — in-build level data
- `Application.Quit()` — the SHUT DOWN button
- `System.IO.Path.Combine(Application.persistentDataPath, FileName)` — the one
  save file

No network, telemetry, reflection, subprocess, or arbitrary-file code anywhere.

## Runtime behaviour

| System | Implementation | Network |
|--------|----------------|---------|
| Save / load | one JSON file in `Application.persistentDataPath`, atomic temp+move | None |
| Audio | `AudioClip.Create` + `SetData` — synthesised at startup | None |
| Level data | `Resources.Load` | None |
| Icon / textures | generated at edit time into the project | None |

## Build configuration

- **Networking engine modules removed** from the manifest: `unitywebrequest*`,
  `unityanalytics`. The IL2CPP output of both the Android APK and the iOS Xcode
  project no longer contains `UnityWebRequestModule` or `UnityAnalyticsModule`.
- `PlayerSettings.Android.forceInternetPermission = false`; a Gradle
  post-processor (`StripNetworkPermissions`) removes `INTERNET` /
  `ACCESS_NETWORK_STATE` from the merged Android manifest.
- iOS `Info.plist`: no `NSAppTransportSecurity`, no `UIBackgroundModes`, no
  network entitlements; `ITSAppUsesNonExemptEncryption = false`.

## Verified build artefacts

- **Android APK** — `aapt2 dump permissions` → **no permissions** (only the
  self-scoped AndroidX `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`). No
  `android.permission.INTERNET`, no `ACCESS_NETWORK_STATE`.
- **iOS Xcode project** — no `GameKit.framework`, no `Network.framework`,
  no `CFNetwork`. `Security` + `SystemConfiguration` are linked by
  `UnityFramework` itself (engine defaults, no game code path).

`PASS` — the game is fully offline; every removable network component is removed,
and the two remaining engine-default iOS framework links are inert.
