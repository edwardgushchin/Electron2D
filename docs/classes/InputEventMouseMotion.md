# InputEventMouseMotion

Last updated: 2026-09-21

- Source/declaration: [`InputEventMouse.cs`](../../src/Core/Input/InputEventMouse.cs), `public sealed class InputEventMouseMotion : InputEventMouse`.
- Responsibility: mouse/stylus movement in local and unscaled screen coordinate spaces.
- Complete declared API: `PenInverted`, unit `Pressure`, `Relative`, `ScreenRelative`, `Velocity`, `ScreenVelocity`, bounded `Tilt`; overrides `Accumulate`, `XformedBy`, `AsText`; protected creation/copy/property-descriptor hooks. All seven declared values are stored typed descriptors.
- Accumulation: requires equal window, press/cancel, buttons, and modifiers; validates the summed deltas, atomically adopts newest positions/velocities and both sums, then emits one change notification.
- Transform: transforms local position/relative/velocity only, preserving global/screen values.
- Errors/threading/verification: all vectors and transform inputs must be finite, pressure `[0,1]`, tilt components `[-1,1]`; disposed/invalid access fails. Accumulation validation, throwing-observer post-commit state, transform, copy, and Input velocity publication are covered.
