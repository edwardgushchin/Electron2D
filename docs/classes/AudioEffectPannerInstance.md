# AudioEffectPannerInstance

Last updated: 2026-10-02

**Declaration:** internal sealed `AudioEffectInstance` · **Source:** [AudioEffectLevel.cs](../../src/Scene/Resources/AudioEffectLevel.cs).

Borrows [AudioEffectPanner](AudioEffectPanner.md), reads one Pan value per block, computes bounded left/right gains and crossfeeds each frame using local input copies so output may alias input. It allocates no processing storage and requests no inactive silence processing. Resource disposal fails later processing; bus removal or native closure disposes the instance. The public base contract and errors are in [AudioEffectInstance](AudioEffectInstance.md).
