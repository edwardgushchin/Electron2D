# AudioEffectFilterInstance

Last updated: 2026-10-02

**Declaration:** internal sealed AudioEffectInstance · **Source:** [AudioEffectFilter.cs](../../src/Scene/Resources/AudioEffectFilter.cs).

Borrows [AudioEffectFilter](AudioEffectFilter.md), prepares eight channel/stage histories and initial coefficients, and caches the current coherent primitive snapshot/rate without lazy comparer allocation. OnProcess updates coefficients on actual scalar/rate changes, then processes one through four cascaded stages per channel. Inputs may alias output. Float history and ordinary coefficients match the independent pinned C++ oracle. Unused stage histories remain intact. Default ProcessSilence stays false; active bus tails continue through ordinary processing.

No buffers grow during processing. Nonfinite float output clears all histories and throws ArithmeticException; native containment/lifetime follow AudioEffectInstance and the FAPO chain. Source disposal rejects later processing. Resource duplication never copies this transient object. AudioFilterTests verifies history separation, presets, snapshots/concurrency, overflow recovery and actual native output. This type is not exported.
