# InputEventScreenDrag

Last updated: 2026-09-21

- Source/declaration: [`InputEventTouch.cs`](../../src/Core/Input/InputEventTouch.cs), `public sealed class InputEventScreenDrag : InputEventFromWindow`.
- Responsibility: movement/stylus data for one active touch contact.
- Complete declared API: `Index`, `PenInverted`, `Position`, `Pressure`, `Relative`, `ScreenRelative`, `Velocity`, `ScreenVelocity`, `Tilt`; overrides `Accumulate`, `XformedBy`, `AsText`; protected creation/copy/property-descriptor hooks. All nine declared values are stored typed descriptors.
- Accumulation/transform: equal contact indexes merge atomically after validating summed deltas, adopt newest position/velocities, and emit one change notification. Only local position/relative/velocity transform; screen values remain unchanged.
- Invariants/errors: index non-negative; vectors finite; pressure `[0,1]`; tilt components `[-1,1]`; disposed access fails.
- Verification: boundary validation, compatible/incompatible accumulation, duplication, and coordinate transforms are covered.
