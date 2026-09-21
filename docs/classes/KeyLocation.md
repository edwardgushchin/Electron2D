# KeyLocation

Last updated: 2026-09-21

- Source/declaration: [`InputEnums.cs`](../../src/Core/Input/InputEnums.cs), `public enum KeyLocation`.
- Complete values: `Unspecified = 0`, `Left = 1`, `Right = 2`.
- Responsibility: distinguishes left/right physical variants when a binding requests it.
- Invariants/errors: InputEventKey rejects undefined assignments. Immutable allocation-free value.
- Verification: location validation, text, copy, and physical-key matching are covered.
