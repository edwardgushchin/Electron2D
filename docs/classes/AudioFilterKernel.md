# AudioFilterKernel

Last updated: 2026-10-02

**Declaration:** internal static class · **Source:** [AudioFilterKernel.cs](../../src/Servers/Audio/AudioFilterKernel.cs).

Private coefficient/history arithmetic for seven frequency-response modes: LowPass, HighPass, BandPass, Notch, BandLimit, LowShelf and HighShelf. Nested readonly Settings contains the coherent mode/cutoff/resonance/gain/stage snapshot; Coefficients contains five normalized doubles; mutable History holds four float input/output samples. Prepare computes stable bounded coefficients at the actual positive mix rate. Process executes one direct-form stage without allocation; the owning instance supplies eight histories.

Ordinary mode equations, stage quality/gain roots and float history match the pinned C++ oracle. Effective cutoff is limited to 0.4999 × mix rate, quality is positive with an effective ceiling, and coefficient damping is floored so extreme controls do not produce unit-circle poles. BandLimit uses the complementary broad band-rejection numerator with its retained logarithmic bandwidth denominator and positive/reordered edge safeguards. Shelf damping gets a relative floor at extreme controls. Scalar raw controls stay on the resource; no new public backend/math API is exposed.

The existing MIT runtime adaptation notice accompanies these equations. AudioFilterTests checks coefficient/PCM oracle, analytic gain/response, stable pole inequalities, 640 edge combinations and real native output. No vendor source or dependency is added. Other-platform native audio and physical acceptance remain unverified.
