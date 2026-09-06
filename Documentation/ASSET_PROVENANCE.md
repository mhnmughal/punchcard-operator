# Asset Provenance

_Last updated: 2026-09-06_

## Art / visual assets

Punchcard Operator currently uses **no imported art assets**. All visuals are
built from Unity UI primitives (solid-colour `Image`, `Outline`, TextMeshPro
text) styled by `Theme` in `AppShell.cs`. Fonts are Unity's bundled
**LiberationSans SDF** (TextMeshPro Essential Resources, shipped with
`com.unity.ugui`, SIL Open Font License).

| Asset | Source | Licence | Attribution | Modified |
|-------|--------|---------|-------------|----------|
| LiberationSans SDF (+ fallback/materials) | Unity TMP Essential Resources | SIL OFL 1.1 | Not required for embedded use | No |
| All UI shapes / colours | Generated from `UnityEngine.UI` primitives (this project) | Project-owned | — | n/a |
| Interaction SFX | `Sfx.cs` runtime synthesis (this project) | Project-owned | — | n/a — see AUDIO_PROVENANCE.md |

## Packages

See `PACKAGE_AUDIT.md`.

## Game icon

Not yet produced. When added, record the creation method and confirm no
third-party copyrighted content, then fill this table.

## Rule

Every asset added from outside this repository gets a row here before it is used:
source, URL, licence, attribution requirement (and where satisfied), modification
status. No asset ships without an established licence.
