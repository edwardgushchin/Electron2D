# AudioEffectFilter.FilterDB

Last updated: 2026-10-02

**Declaration:** public nested enum · **Source:** [AudioEffectFilter.cs](../../src/Scene/Resources/AudioEffectFilter.cs) · **Owner:** [AudioEffectFilter](AudioEffectFilter.md).

Selects one through four prepared cascaded filter stages. DB retains these numeric presets. The names are the contract's cutoff presets; actual transfer/quality/gain behavior is described on the owner and verified by PCM and spectral checks.

| Value | Numeric identity | Selected stages |
| --- | --- | --- |
| `Filter6DB` | 0 | One; default. |
| `Filter12DB` | 1 | Two. |
| `Filter18DB` | 2 | Three. |
| `Filter24DB` | 3 | Four. |

<a id="filter6db"></a>
<a id="filter12db"></a>
<a id="filter18db"></a>
<a id="filter24db"></a>
All four values execute on every concrete filter; invalid values reject. Stage edits retain history. Example: `filter.DB = AudioEffectFilter.FilterDB.Filter24DB;`. AudioFilterTests covers each preset in the independent oracle and boundary/native checks.
