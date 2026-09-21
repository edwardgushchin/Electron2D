# InputEventWithModifiers

Last updated: 2026-09-21

- Source/declaration: [`InputEvent.cs`](../../src/Core/Input/InputEvent.cs), `public abstract class InputEventWithModifiers : InputEventFromWindow`.
- Responsibility: stores Alt, Shift, Control, Meta, and portable command-or-control state; initializes `Device` to the primary keyboard.
- Complete declared API: protected constructor; `AltPressed`, `ShiftPressed`, `ControlPressed`, `MetaPressed`, `CommandOrControlAutoremap`; `GetModifiersMask()`, `IsCommandOrControlPressed()`, `SetModifiersFromEvent(...)`; override `AsText()`; protected overrides `CopyEventStateTo` and `GetPropertyDescriptors`. All five modifier values are stored typed descriptors.
- Invariants/errors: enabling autoremap chooses Meta on macOS and Control elsewhere; concrete Control/Meta assignment is rejected while enabled; disabling clears both. Disposed resources fail.
- Threading: mutable caller-owned Resource, no internal synchronization.
- Verification/limits: exact/non-exact modifier matching and copy/text paths are covered. Platform choice uses .NET OS detection and requires no native host.
