# AudioEffectAmplifyInstance

Last updated: 2026-10-02

**Declaration:** internal sealed `AudioEffectInstance` · **Source:** [AudioEffectLevel.cs](../../src/Scene/Resources/AudioEffectLevel.cs).

Borrows [AudioEffectAmplify](AudioEffectAmplify.md), stores its previous decibel target and processes each stereo frame with a block-linear gain increment. Each instance starts at the resource's gain, so later resource edits ramp independently per output pair. It allocates no processing storage. Default `ProcessSilence` is false. Resource disposal fails later processing; bus removal or native closure disposes the instance. The public base contract and errors are in [AudioEffectInstance](AudioEffectInstance.md).
