# InputEventMagnifyGesture

Last updated: 2026-09-21

- Source/declaration: [`InputEventTouch.cs`](../../src/Core/Input/InputEventTouch.cs), `public sealed class InputEventMagnifyGesture : InputEventGesture`.
- Responsibility: pinch magnification around a local position.
- Complete declared API: finite positive `Factor` (default `1`); overrides `XformedBy`, `AsText`; protected creation/copy/property-descriptor hooks. `Factor` is a stored typed descriptor.
- Transform: returns an independent duplicate with transformed position; factor/modifiers/window/device are preserved.
- Errors/threading/verification: zero, negative, non-finite, or disposed access fails; factor boundaries, copy, text, and transform are covered.
