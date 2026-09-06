# Offline Audit

_Last updated: 2026-09-06_

Punchcard Operator must function fully offline with no telemetry.

## Source scan

`grep -rnE 'UnityWebRequest|WebRequest|HttpClient|Socket|TcpClient|UdpClient|WWW|
DownloadHandler|Application\.OpenURL|http://|https://|Ping|Dns\.|NetworkReachability|
Analytics|Firebase|Advertisement|RemoteConfig|RemoteSettings'` over
`Assets/PunchcardOperator/Scripts/` → **no matches.**

Runtime behaviour:

| System | Implementation | Network |
|--------|----------------|---------|
| Save/load | `System.IO.File` read/write of one JSON file in `Application.persistentDataPath`, atomic temp+move | None |
| Audio | `AudioClip.Create` + `SetData` — synthesised at startup | None |
| Level data | `Resources.Load<LevelDatabase>` — in-build asset | None |
| Icon / art | Generated at edit time into the project; no runtime fetch | None |

No ads, analytics, remote config, cloud save, login, social, crash-reporting,
or AI-runtime code exists in the project.

## Build configuration

- `PlayerSettings.Android.forceInternetPermission = false`
- No code references `UnityWebRequest*`; the `com.unity.modules.unitywebrequest*`
  built-in modules remain in the manifest (engine defaults) but nothing calls
  them — IL2CPP managed stripping (Low) drops unreferenced code.
- `com.unity.modules.unityanalytics` is a dormant engine module; no
  `UnityEngine.Analytics` call sites exist.

## Remaining verification

- `NOT VERIFIED`: inspecting the shipped APK's merged `AndroidManifest.xml` to
  confirm zero `android.permission.INTERNET` / `ACCESS_NETWORK_STATE`. To be done
  once the APK is unzipped/`aapt`-dumped.
