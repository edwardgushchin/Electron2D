# FrameDeltaSmoother

Last updated: 2026-10-21

**Namespace:** Electron2D. **Declaration:** internal sealed class FrameDeltaSmoother. **Source:** [FrameDeltaSmoother.cs](../../src/Core/Config/FrameDeltaSmoother.cs). **Component:** [host delta smoothing](../components/host-delta-smoothing.md).

Engine retains this scalar-only, allocation-free estimator and resets it for each successfully attached loop. Smooth accepts measured microseconds plus eligibility and reports whether a confident filter actually applied. The caller preserves original fractional seconds otherwise. Sixty-sample consensus and a separate 64-sample measured-rate gate control integer-period quantization with a signed carried remainder. Disabled/nonclassical/resume bypass preserves history; explicit Reset clears all confidence and sums. Inputs are nonnegative on the host path; a negative diagnostic input passes through unchanged.

It owns no native handles, callbacks, engine-object identity or static service policy. The OS property and effective VSync query belong to the caller. Manual/fixed hosts do not call it. Detailed rate/threshold semantics, C++ oracle agreement, native integration and measurement limits are in the component. The existing fixed-step synchronizer remains the next stage, with its own catch-up/interpolation contract.
