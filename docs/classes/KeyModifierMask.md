# KeyModifierMask

Last updated: 2026-09-21

- Source/declaration: [`InputEnums.cs`](../../src/Core/Input/InputEnums.cs), `[Flags] public enum KeyModifierMask`.
- Complete values: `CodeMask`, `ModifierMask`, `CommandOrControl`, `Shift`, `Alt`, `Meta`, `Control`, `Keypad`, `GroupSwitch`.
- Responsibility: separates key-code bits from combinable modifier/layout bits. Command-or-control maps to Meta on macOS and Control elsewhere when used by modifier events.
- Invariants/threading: immutable allocation-free value; unknown bits may exist through casts but concrete event APIs only produce documented bits.
- Verification: mask production, exact/non-exact matching, platform mapping, and key-code composition are covered.
