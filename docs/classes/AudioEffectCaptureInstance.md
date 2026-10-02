# AudioEffectCaptureInstance

Last updated: 2026-10-02

**Declaration:** internal sealed AudioEffectInstance · **Source:** [AudioEffectCapture.cs](../../src/Scene/Resources/AudioEffectCapture.cs).

**Inherits:** [AudioEffectInstance](AudioEffectInstance.md).

Internal borrowed-source stereo capture state. OnProcess copies the complete source block into the resource's shared ring when space permits, then copies the same PCM to output. It supports aliased spans and requests processing of silence. The shared resource owns ring capacity and counters; this instance owns no native device or separate ring. Source disposal fails through ordinary standalone/native processing semantics. Factories and buses own instance lifetime as described by [AudioEffectCapture](AudioEffectCapture.md).

AudioEffectTests checks FIFO, aliases, lifetime and real native PCM. This type is not publicly exported.
