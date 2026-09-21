# MouseButtonMask

Last updated: 2026-09-21

- Source/declaration: [`InputEnums.cs`](../../src/Core/Input/InputEnums.cs), `[Flags] public enum MouseButtonMask`.
- Complete values: `None`, `Left`, `Right`, `Middle`, `XButton1`, `XButton2`.
- Responsibility: simultaneous held non-wheel buttons.
- Invariants/errors: InputEventMouse rejects unknown bits. Immutable allocation-free value and thread-safe snapshots from Input.
- Verification: validation, button press/release, motion replacement, and release-all are covered.
