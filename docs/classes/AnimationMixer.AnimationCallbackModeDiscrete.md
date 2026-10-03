# AnimationMixer.AnimationCallbackModeDiscrete

Last updated: 2026-10-04

Controls precedence between discrete keys and continuously mixed values. Declared by [AnimationMixer](AnimationMixer.md).

| Value | Number | Behavior |
| --- | --- | --- |
| Dominant | 0 | Crossed discrete writes prevail over continuous output. |
| Recessive | 1 | Continuous output prevails over discrete writes. |
| ForceContinuous | 2 | Discrete tracks sample nearest keys each update and enter the typed accumulator. |

See [weighted transitions](../components/scene-animation.md#weighted-transitions-and-capture) for unsupported-value fallback, source timing, errors and verification.
