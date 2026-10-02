# AudioEffectEQInstance

Last updated: 2026-10-02

**Declaration:** internal sealed `AudioEffectInstance` · **Source:** [AudioEffectEQ.cs](../../src/Scene/Resources/AudioEffectEQ.cs).

Borrows [AudioEffectEQ](AudioEffectEQ.md), allocates two fixed arrays of prepared band histories and one gain snapshot at construction, then filters each original stereo input independently for every band and sums the weighted outputs. Resource gain edits apply per block without clearing history; output-rate changes refresh coefficients. It supports aliased spans because each source frame is captured before destination write. Overflow clears histories and reports ArithmeticException. Default inactive-silence policy is false; bus removal/closure disposes the instance. See the public [AudioEffectInstance](AudioEffectInstance.md) contract and [component evidence](../components/audio-playback.md#graphic-eq).
