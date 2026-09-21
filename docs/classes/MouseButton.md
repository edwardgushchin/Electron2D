# MouseButton

Last updated: 2026-09-21

- Source/declaration: [`InputEnums.cs`](../../src/Core/Input/InputEnums.cs), `public enum MouseButton`.
- Complete values: `None = 0`, `Left = 1`, `Right = 2`, `Middle = 3`, `WheelUp = 4`, `WheelDown = 5`, `WheelLeft = 6`, `WheelRight = 7`, `XButton1 = 8`, `XButton2 = 9`.
- Responsibility: identifies one button or wheel direction. Wheel values are transient events, not held-mask bits.
- Invariants/errors: InputEventMouseButton rejects undefined assignments. Immutable allocation-free value.
- Verification: matching, held-mask conversion, wheel exclusion, text, and invalid values are covered.
