# Audio Provenance

_Last updated: 2026-09-06_

## Summary

Punchcard Operator ships **no external audio files**. Every interaction sound is
synthesised at runtime by `Assets/PunchcardOperator/Scripts/Runtime/Sfx.cs` as a
short tone + filtered-noise burst (`AudioClip.Create` + `SetData`). There is
therefore nothing to licence and no third-party attribution requirement.

| Cue | Source | Licence | Attribution | Modified |
|-----|--------|---------|-------------|----------|
| punch, chad, patch, shift, swap, flip, copy, load, reader, step, turn, execute, ui, error, success, certified | Generated in-engine by `Sfx.Bake()` (original DSP, this project) | Original work — project-owned | None required | n/a (generated) |

## If external audio is added later

Record here for every asset: file, creator/source, source URL, licence, whether
attribution is required (and where it is given), and whether the file was
modified. Do not add an asset whose licence cannot be established. Music, if
added, must be cleared for commercial distribution.
