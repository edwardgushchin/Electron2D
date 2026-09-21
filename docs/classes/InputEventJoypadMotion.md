# InputEventJoypadMotion

Last updated: 2026-09-21

- Source/declaration: [`InputEventJoypad.cs`](../../src/Core/Input/InputEventJoypad.cs), `public sealed class InputEventJoypadMotion : InputEvent`.
- Responsibility: signed motion on one standardized/raw controller axis.
- Complete declared API: `Axis`, `AxisValue`; overrides `IsMatch`, `AsText`; protected creation/copy/property-descriptor hooks. Inherited `IsActionType` classifies this sealed built-in as bindable. Both values are stored typed descriptors.
- Matching/state: axis identity plus direction for exact matching. The event itself is not a button-like press; action matching derives pressed state from that action's deadzone. Action raw strength is absolute magnitude in the binding direction, and adjusted strength remaps the remaining range to `[0,1]`.
- Invariants/errors: axis `0..9`, finite value `[-1,1]`; disposed access fails.
- Verification: per-device state, direction/exactness, deadzone boundaries, copy, and descriptions are covered.
