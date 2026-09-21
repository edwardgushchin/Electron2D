# InputEventJoypadButton

Last updated: 2026-09-21

- Source/declaration: [`InputEventJoypad.cs`](../../src/Core/Input/InputEventJoypad.cs), `public sealed class InputEventJoypadButton : InputEvent`.
- Responsibility: one standardized/raw controller button press/release and optional pressure.
- Complete declared API: `ButtonIndex`, `Pressed`, `Pressure`; overrides `IsMatch`, `AsText`; protected creation/copy/property-descriptor hooks. Inherited `IsActionType` classifies this sealed built-in as bindable. All three values are stored typed descriptors.
- Invariants/errors: button is `0..127`, pressure finite `[0,1]`; binding identity ignores pressure; disposed access fails.
- Threading/verification: caller-owned mutable state; per-device raw tracking, action matching, boundaries, copy, and text are covered. Device discovery/effects remain under the SDL gamepad trigger.
