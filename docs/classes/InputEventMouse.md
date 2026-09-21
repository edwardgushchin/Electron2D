# InputEventMouse

Last updated: 2026-09-21

- Source/declaration: [`InputEventMouse.cs`](../../src/Core/Input/InputEventMouse.cs), `public abstract class InputEventMouse : InputEventWithModifiers`.
- Responsibility: common mouse button mask, local position, and top-level global position; initializes `Device` to the primary mouse.
- Complete declared API: protected constructor; `ButtonMask`, `Position`, `GlobalPosition`; protected overrides `CopyEventStateTo` and `GetPropertyDescriptors`. All three values are stored typed descriptors.
- Invariants: mask accepts Left/Right/Middle/X1/X2 only; positions are finite pixels. Local transforms affect `Position`; concrete classes preserve `GlobalPosition`.
- Threading/lifecycle: caller-owned mutable Resource; disposed access fails.
- Verification: mask validation, copying, concrete transforms, and state queries are covered.
