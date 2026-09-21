# InputEventPanGesture

Last updated: 2026-09-21

- Source/declaration: [`InputEventTouch.cs`](../../src/Core/Input/InputEventTouch.cs), `public sealed class InputEventPanGesture : InputEventGesture`.
- Responsibility: finite two-dimensional pan delta around a local gesture position.
- Complete declared API: `Delta`; overrides `XformedBy`, `AsText`; protected creation/copy/property-descriptor hooks. `Delta` is a stored typed descriptor.
- Transform: returns an independent duplicate and transforms its position affinely; the platform-reported pan delta is copied unchanged.
- Errors/threading/verification: non-finite vectors and disposed access fail; copy, text, and transform are covered.
