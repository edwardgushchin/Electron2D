# AudioEffectDistortionInstance

Last updated: 2026-10-02

**Declaration:** `internal sealed class Electron2D.AudioEffectDistortionInstance` · **Source:** [AudioEffectDistortion.cs](../../src/Scene/Resources/AudioEffectDistortion.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectInstance](AudioEffectInstance.md).

## Description

Independent prepared stereo processor borrowing one [AudioEffectDistortion](AudioEffectDistortion.md). It owns only two one-pole filter histories and the creation-time output rate; no processing buffer is allocated. `OnProcess` samples the resource once per block, transforms every left/right sample and supports aliased spans. `OnProcessSilence` requests continued filter-tail processing. Nonfinite filter history or output clears both histories before an arithmetic error. Resource copies do not copy transient histories; attached instances follow the inherited borrowed lifetime and cannot be processed or disposed by callers.

[AudioDistortionTests](../../tests/Electron2D.Tests/AudioDistortionTests.cs) verifies all five byte-pinned curves and native/public host processing. Physical listening and other platforms remain unverified.
