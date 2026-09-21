# Key

Last updated: 2026-09-21

- Source/declaration: [`InputEnums.cs`](../../src/Core/Input/InputEnums.cs), `public enum Key`.
- Responsibility: logical/physical/label key identity. Printable values equal Unicode scalar values; special keys set bit 22. Modifier bits belong to [`KeyModifierMask`](KeyModifierMask.md).
- Complete values:
  - Control/navigation: `None`, `Special`, `Escape`, `Tab`, `Backtab`, `Backspace`, `Enter`, `KeypadEnter`, `Insert`, `Delete`, `Pause`, `Print`, `SystemRequest`, `Clear`, `Home`, `End`, `Left`, `Up`, `Right`, `Down`, `PageUp`, `PageDown`, `Shift`, `Control`, `Meta`, `Alt`, `CapsLock`, `NumLock`, `ScrollLock`.
  - Function/system/media: `F1` through `F35`, `Menu`, `Hyper`, `Help`, `Back`, `Forward`, `Stop`, `Refresh`, `VolumeDown`, `VolumeMute`, `VolumeUp`, `MediaPlay`, `MediaStop`, `MediaPrevious`, `MediaNext`, `MediaRecord`, `HomePage`, `Favorites`, `Search`, `Standby`, `OpenUrl`, `LaunchMail`, `LaunchMedia`, `Launch0` through `Launch9`, `LaunchA` through `LaunchF`, `Globe`, `Keyboard`, `JisEisu`, `JisKana`.
  - Keypad/sentinel: `KeypadMultiply`, `KeypadDivide`, `KeypadSubtract`, `KeypadPeriod`, `KeypadAdd`, `Keypad0` through `Keypad9`, `Unknown`.
  - Printable: `Space`, `Exclamation`, `QuoteDouble`, `NumberSign`, `Dollar`, `Percent`, `Ampersand`, `Apostrophe`, `ParenthesisLeft`, `ParenthesisRight`, `Asterisk`, `Plus`, `Comma`, `Minus`, `Period`, `Slash`, `Key0` through `Key9`, `Colon`, `Semicolon`, `Less`, `Equal`, `Greater`, `Question`, `At`, `A` through `Z`, `BracketLeft`, `Backslash`, `BracketRight`, `AsciiCircumflex`, `Underscore`, `QuoteLeft`, `BraceLeft`, `Bar`, `BraceRight`, `AsciiTilde`, `Yen`, `Section`.
- Invariants/threading: value type, immutable, allocation-free, and thread-safe. Unknown numeric values can exist through casts; event formatting reports their number.
- Verification: constants, printable/special formatting, modifier composition, and raw-state use are covered.
