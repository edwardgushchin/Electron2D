# InputEventKey

Last updated: 2026-09-21

- Source/declaration: [`InputEventKey.cs`](../../src/Core/Input/InputEventKey.cs), `public sealed class InputEventKey : InputEventWithModifiers`.
- Responsibility: keyboard press/release/repeat with logical, physical, label, Unicode-scalar, location, window, device, and modifier data.
- Complete declared API: `Pressed`, `Echo`, `Keycode`, `PhysicalKeycode`, `KeyLabel`, `Unicode`, `Location`; `GetKeycodeWithModifiers`, `GetPhysicalKeycodeWithModifiers`, `GetKeyLabelWithModifiers`; `AsTextKeycode`, `AsTextPhysicalKeycode`, `AsTextKeyLabel`, `AsTextLocation`; overrides `IsEcho`, `IsMatch`, `AsText`; protected creation/copy/property-descriptor hooks. Inherited `IsActionType` classifies this sealed built-in as bindable. All seven declared values are stored typed descriptors.
- Matching: label-only bindings use labels; otherwise logical code wins over physical code. Physical bindings may require location. Non-exact presses allow extra modifiers; releases ignore required modifiers; exact matching requires equality.
- Errors/threading: Unicode must be zero or a scalar, location must be defined, and disposed access fails. Mutable caller-owned state is not synchronized.
- Verification: raw logical/physical/label state, modifiers, exactness, repeat policy, text, duplication, and release matching are covered.
