# Key

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEnums.cs`](../../src/Core/Input/InputEnums.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Key`

> Identifies logical, physical, and printable keyboard keys.

## Description

Identifies logical, physical, and printable keyboard keys.

- Responsibility: logical/physical/label key identity. Printable values equal Unicode scalar values; special keys set bit 22. Modifier bits belong to [`KeyModifierMask`](KeyModifierMask.md).
- Complete values:
  - Control/navigation: `None`, `Special`, `Escape`, `Tab`, `Backtab`, `Backspace`, `Enter`, `KeypadEnter`, `Insert`, `Delete`, `Pause`, `Print`, `SystemRequest`, `Clear`, `Home`, `End`, `Left`, `Up`, `Right`, `Down`, `PageUp`, `PageDown`, `Shift`, `Control`, `Meta`, `Alt`, `CapsLock`, `NumLock`, `ScrollLock`.
  - Function/system/media: `F1` through `F35`, `Menu`, `Hyper`, `Help`, `Back`, `Forward`, `Stop`, `Refresh`, `VolumeDown`, `VolumeMute`, `VolumeUp`, `MediaPlay`, `MediaStop`, `MediaPrevious`, `MediaNext`, `MediaRecord`, `HomePage`, `Favorites`, `Search`, `Standby`, `OpenUrl`, `LaunchMail`, `LaunchMedia`, `Launch0` through `Launch9`, `LaunchA` through `LaunchF`, `Globe`, `Keyboard`, `JisEisu`, `JisKana`.
  - Keypad/sentinel: `KeypadMultiply`, `KeypadDivide`, `KeypadSubtract`, `KeypadPeriod`, `KeypadAdd`, `Keypad0` through `Keypad9`, `Unknown`.
  - Printable: `Space`, `Exclamation`, `QuoteDouble`, `NumberSign`, `Dollar`, `Percent`, `Ampersand`, `Apostrophe`, `ParenthesisLeft`, `ParenthesisRight`, `Asterisk`, `Plus`, `Comma`, `Minus`, `Period`, `Slash`, `Key0` through `Key9`, `Colon`, `Semicolon`, `Less`, `Equal`, `Greater`, `Question`, `At`, `A` through `Z`, `BracketLeft`, `Backslash`, `BracketRight`, `AsciiCircumflex`, `Underscore`, `QuoteLeft`, `BraceLeft`, `Bar`, `BraceRight`, `AsciiTilde`, `Yen`, `Section`.
- Invariants/threading: value type, immutable, allocation-free, and thread-safe. Unknown numeric values can exist through casts; event formatting reports their number.
- Verification: constants, printable/special formatting, modifier composition, and raw-state use are covered.

Printable values use their Unicode scalar values. Special keys occupy the range beginning at
[`Key.Special`](Key.md#f-electron2d-key-special). Modifier bits are represented separately by [`KeyModifierMask`](KeyModifierMask.md).

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = Key.None;
```

## Constants

| Member | Description |
| --- | --- |
| [`None = 0`](#f-electron2d-key-none) | Identifies no key. |
| [`Special = 4194304`](#f-electron2d-key-special) | Marks the beginning of the non-Unicode special-key range. |
| [`Escape = 4194305`](#f-electron2d-key-escape) | Identifies Escape. |
| [`Tab = 4194306`](#f-electron2d-key-tab) | Identifies Tab. |
| [`Backtab = 4194307`](#f-electron2d-key-backtab) | Identifies reverse Tab. |
| [`Backspace = 4194308`](#f-electron2d-key-backspace) | Identifies Backspace. |
| [`Enter = 4194309`](#f-electron2d-key-enter) | Identifies Enter or Return. |
| [`KeypadEnter = 4194310`](#f-electron2d-key-keypadenter) | Identifies the numeric-keypad Enter key. |
| [`Insert = 4194311`](#f-electron2d-key-insert) | Identifies Insert. |
| [`Delete = 4194312`](#f-electron2d-key-delete) | Identifies Delete. |
| [`Pause = 4194313`](#f-electron2d-key-pause) | Identifies Pause. |
| [`Print = 4194314`](#f-electron2d-key-print) | Identifies Print Screen. |
| [`SystemRequest = 4194315`](#f-electron2d-key-systemrequest) | Identifies System Request. |
| [`Clear = 4194316`](#f-electron2d-key-clear) | Identifies Clear. |
| [`Home = 4194317`](#f-electron2d-key-home) | Identifies Home. |
| [`End = 4194318`](#f-electron2d-key-end) | Identifies End. |
| [`Left = 4194319`](#f-electron2d-key-left) | Identifies the left arrow. |
| [`Up = 4194320`](#f-electron2d-key-up) | Identifies the up arrow. |
| [`Right = 4194321`](#f-electron2d-key-right) | Identifies the right arrow. |
| [`Down = 4194322`](#f-electron2d-key-down) | Identifies the down arrow. |
| [`PageUp = 4194323`](#f-electron2d-key-pageup) | Identifies Page Up. |
| [`PageDown = 4194324`](#f-electron2d-key-pagedown) | Identifies Page Down. |
| [`Shift = 4194325`](#f-electron2d-key-shift) | Identifies Shift. |
| [`Control = 4194326`](#f-electron2d-key-control) | Identifies Control. |
| [`Meta = 4194327`](#f-electron2d-key-meta) | Identifies Meta, Command, Windows, or Super. |
| [`Alt = 4194328`](#f-electron2d-key-alt) | Identifies Alt or Option. |
| [`CapsLock = 4194329`](#f-electron2d-key-capslock) | Identifies Caps Lock. |
| [`NumLock = 4194330`](#f-electron2d-key-numlock) | Identifies Num Lock. |
| [`ScrollLock = 4194331`](#f-electron2d-key-scrolllock) | Identifies Scroll Lock. |
| [`F1 = 4194332`](#f-electron2d-key-f1) | Identifies function key F1. |
| [`F2 = 4194333`](#f-electron2d-key-f2) | Identifies function key F2. |
| [`F3 = 4194334`](#f-electron2d-key-f3) | Identifies function key F3. |
| [`F4 = 4194335`](#f-electron2d-key-f4) | Identifies function key F4. |
| [`F5 = 4194336`](#f-electron2d-key-f5) | Identifies function key F5. |
| [`F6 = 4194337`](#f-electron2d-key-f6) | Identifies function key F6. |
| [`F7 = 4194338`](#f-electron2d-key-f7) | Identifies function key F7. |
| [`F8 = 4194339`](#f-electron2d-key-f8) | Identifies function key F8. |
| [`F9 = 4194340`](#f-electron2d-key-f9) | Identifies function key F9. |
| [`F10 = 4194341`](#f-electron2d-key-f10) | Identifies function key F10. |
| [`F11 = 4194342`](#f-electron2d-key-f11) | Identifies function key F11. |
| [`F12 = 4194343`](#f-electron2d-key-f12) | Identifies function key F12. |
| [`F13 = 4194344`](#f-electron2d-key-f13) | Identifies function key F13. |
| [`F14 = 4194345`](#f-electron2d-key-f14) | Identifies function key F14. |
| [`F15 = 4194346`](#f-electron2d-key-f15) | Identifies function key F15. |
| [`F16 = 4194347`](#f-electron2d-key-f16) | Identifies function key F16. |
| [`F17 = 4194348`](#f-electron2d-key-f17) | Identifies function key F17. |
| [`F18 = 4194349`](#f-electron2d-key-f18) | Identifies function key F18. |
| [`F19 = 4194350`](#f-electron2d-key-f19) | Identifies function key F19. |
| [`F20 = 4194351`](#f-electron2d-key-f20) | Identifies function key F20. |
| [`F21 = 4194352`](#f-electron2d-key-f21) | Identifies function key F21. |
| [`F22 = 4194353`](#f-electron2d-key-f22) | Identifies function key F22. |
| [`F23 = 4194354`](#f-electron2d-key-f23) | Identifies function key F23. |
| [`F24 = 4194355`](#f-electron2d-key-f24) | Identifies function key F24. |
| [`F25 = 4194356`](#f-electron2d-key-f25) | Identifies function key F25. |
| [`F26 = 4194357`](#f-electron2d-key-f26) | Identifies function key F26. |
| [`F27 = 4194358`](#f-electron2d-key-f27) | Identifies function key F27. |
| [`F28 = 4194359`](#f-electron2d-key-f28) | Identifies function key F28. |
| [`F29 = 4194360`](#f-electron2d-key-f29) | Identifies function key F29. |
| [`F30 = 4194361`](#f-electron2d-key-f30) | Identifies function key F30. |
| [`F31 = 4194362`](#f-electron2d-key-f31) | Identifies function key F31. |
| [`F32 = 4194363`](#f-electron2d-key-f32) | Identifies function key F32. |
| [`F33 = 4194364`](#f-electron2d-key-f33) | Identifies function key F33. |
| [`F34 = 4194365`](#f-electron2d-key-f34) | Identifies function key F34. |
| [`F35 = 4194366`](#f-electron2d-key-f35) | Identifies function key F35. |
| [`Menu = 4194370`](#f-electron2d-key-menu) | Identifies the context-menu key. |
| [`Hyper = 4194371`](#f-electron2d-key-hyper) | Identifies Hyper. |
| [`Help = 4194373`](#f-electron2d-key-help) | Identifies Help. |
| [`Back = 4194376`](#f-electron2d-key-back) | Identifies browser Back. |
| [`Forward = 4194377`](#f-electron2d-key-forward) | Identifies browser Forward. |
| [`Stop = 4194378`](#f-electron2d-key-stop) | Identifies browser Stop. |
| [`Refresh = 4194379`](#f-electron2d-key-refresh) | Identifies browser Refresh. |
| [`VolumeDown = 4194380`](#f-electron2d-key-volumedown) | Identifies volume down. |
| [`VolumeMute = 4194381`](#f-electron2d-key-volumemute) | Identifies volume mute. |
| [`VolumeUp = 4194382`](#f-electron2d-key-volumeup) | Identifies volume up. |
| [`MediaPlay = 4194388`](#f-electron2d-key-mediaplay) | Identifies media play. |
| [`MediaStop = 4194389`](#f-electron2d-key-mediastop) | Identifies media stop. |
| [`MediaPrevious = 4194390`](#f-electron2d-key-mediaprevious) | Identifies previous media. |
| [`MediaNext = 4194391`](#f-electron2d-key-medianext) | Identifies next media. |
| [`MediaRecord = 4194392`](#f-electron2d-key-mediarecord) | Identifies media record. |
| [`HomePage = 4194393`](#f-electron2d-key-homepage) | Identifies the home-page key. |
| [`Favorites = 4194394`](#f-electron2d-key-favorites) | Identifies favorites. |
| [`Search = 4194395`](#f-electron2d-key-search) | Identifies search. |
| [`Standby = 4194396`](#f-electron2d-key-standby) | Identifies standby. |
| [`OpenUrl = 4194397`](#f-electron2d-key-openurl) | Identifies open URL. |
| [`LaunchMail = 4194398`](#f-electron2d-key-launchmail) | Identifies launch mail. |
| [`LaunchMedia = 4194399`](#f-electron2d-key-launchmedia) | Identifies launch media. |
| [`Launch0 = 4194400`](#f-electron2d-key-launch0) | Identifies application-launch key zero. |
| [`Launch1 = 4194401`](#f-electron2d-key-launch1) | Identifies application-launch key one. |
| [`Launch2 = 4194402`](#f-electron2d-key-launch2) | Identifies application-launch key two. |
| [`Launch3 = 4194403`](#f-electron2d-key-launch3) | Identifies application-launch key three. |
| [`Launch4 = 4194404`](#f-electron2d-key-launch4) | Identifies application-launch key four. |
| [`Launch5 = 4194405`](#f-electron2d-key-launch5) | Identifies application-launch key five. |
| [`Launch6 = 4194406`](#f-electron2d-key-launch6) | Identifies application-launch key six. |
| [`Launch7 = 4194407`](#f-electron2d-key-launch7) | Identifies application-launch key seven. |
| [`Launch8 = 4194408`](#f-electron2d-key-launch8) | Identifies application-launch key eight. |
| [`Launch9 = 4194409`](#f-electron2d-key-launch9) | Identifies application-launch key nine. |
| [`LaunchA = 4194410`](#f-electron2d-key-launcha) | Identifies application-launch key A. |
| [`LaunchB = 4194411`](#f-electron2d-key-launchb) | Identifies application-launch key B. |
| [`LaunchC = 4194412`](#f-electron2d-key-launchc) | Identifies application-launch key C. |
| [`LaunchD = 4194413`](#f-electron2d-key-launchd) | Identifies application-launch key D. |
| [`LaunchE = 4194414`](#f-electron2d-key-launche) | Identifies application-launch key E. |
| [`LaunchF = 4194415`](#f-electron2d-key-launchf) | Identifies application-launch key F. |
| [`Globe = 4194416`](#f-electron2d-key-globe) | Identifies the globe key. |
| [`Keyboard = 4194417`](#f-electron2d-key-keyboard) | Identifies the on-screen-keyboard key. |
| [`JisEisu = 4194418`](#f-electron2d-key-jiseisu) | Identifies the Japanese alphanumeric key. |
| [`JisKana = 4194419`](#f-electron2d-key-jiskana) | Identifies the Japanese kana key. |
| [`KeypadMultiply = 4194433`](#f-electron2d-key-keypadmultiply) | Identifies numeric-keypad multiplication. |
| [`KeypadDivide = 4194434`](#f-electron2d-key-keypaddivide) | Identifies numeric-keypad division. |
| [`KeypadSubtract = 4194435`](#f-electron2d-key-keypadsubtract) | Identifies numeric-keypad subtraction. |
| [`KeypadPeriod = 4194436`](#f-electron2d-key-keypadperiod) | Identifies the numeric-keypad decimal separator. |
| [`KeypadAdd = 4194437`](#f-electron2d-key-keypadadd) | Identifies numeric-keypad addition. |
| [`Keypad0 = 4194438`](#f-electron2d-key-keypad0) | Identifies numeric-keypad zero. |
| [`Keypad1 = 4194439`](#f-electron2d-key-keypad1) | Identifies numeric-keypad one. |
| [`Keypad2 = 4194440`](#f-electron2d-key-keypad2) | Identifies numeric-keypad two. |
| [`Keypad3 = 4194441`](#f-electron2d-key-keypad3) | Identifies numeric-keypad three. |
| [`Keypad4 = 4194442`](#f-electron2d-key-keypad4) | Identifies numeric-keypad four. |
| [`Keypad5 = 4194443`](#f-electron2d-key-keypad5) | Identifies numeric-keypad five. |
| [`Keypad6 = 4194444`](#f-electron2d-key-keypad6) | Identifies numeric-keypad six. |
| [`Keypad7 = 4194445`](#f-electron2d-key-keypad7) | Identifies numeric-keypad seven. |
| [`Keypad8 = 4194446`](#f-electron2d-key-keypad8) | Identifies numeric-keypad eight. |
| [`Keypad9 = 4194447`](#f-electron2d-key-keypad9) | Identifies numeric-keypad nine. |
| [`Unknown = 8388607`](#f-electron2d-key-unknown) | Identifies an unknown key. |
| [`Space = 32`](#f-electron2d-key-space) | Identifies Space. |
| [`Exclamation = 33`](#f-electron2d-key-exclamation) | Identifies `!`. |
| [`QuoteDouble = 34`](#f-electron2d-key-quotedouble) | Identifies a double quote. |
| [`NumberSign = 35`](#f-electron2d-key-numbersign) | Identifies `#`. |
| [`Dollar = 36`](#f-electron2d-key-dollar) | Identifies `$`. |
| [`Percent = 37`](#f-electron2d-key-percent) | Identifies `%`. |
| [`Ampersand = 38`](#f-electron2d-key-ampersand) | Identifies `&`. |
| [`Apostrophe = 39`](#f-electron2d-key-apostrophe) | Identifies an apostrophe. |
| [`ParenthesisLeft = 40`](#f-electron2d-key-parenthesisleft) | Identifies `(`. |
| [`ParenthesisRight = 41`](#f-electron2d-key-parenthesisright) | Identifies `)`. |
| [`Asterisk = 42`](#f-electron2d-key-asterisk) | Identifies `*`. |
| [`Plus = 43`](#f-electron2d-key-plus) | Identifies `+`. |
| [`Comma = 44`](#f-electron2d-key-comma) | Identifies `,`. |
| [`Minus = 45`](#f-electron2d-key-minus) | Identifies `-`. |
| [`Period = 46`](#f-electron2d-key-period) | Identifies `.`. |
| [`Slash = 47`](#f-electron2d-key-slash) | Identifies `/`. |
| [`Key0 = 48`](#f-electron2d-key-key0) | Identifies digit zero. |
| [`Key1 = 49`](#f-electron2d-key-key1) | Identifies digit one. |
| [`Key2 = 50`](#f-electron2d-key-key2) | Identifies digit two. |
| [`Key3 = 51`](#f-electron2d-key-key3) | Identifies digit three. |
| [`Key4 = 52`](#f-electron2d-key-key4) | Identifies digit four. |
| [`Key5 = 53`](#f-electron2d-key-key5) | Identifies digit five. |
| [`Key6 = 54`](#f-electron2d-key-key6) | Identifies digit six. |
| [`Key7 = 55`](#f-electron2d-key-key7) | Identifies digit seven. |
| [`Key8 = 56`](#f-electron2d-key-key8) | Identifies digit eight. |
| [`Key9 = 57`](#f-electron2d-key-key9) | Identifies digit nine. |
| [`Colon = 58`](#f-electron2d-key-colon) | Identifies `:`. |
| [`Semicolon = 59`](#f-electron2d-key-semicolon) | Identifies `;`. |
| [`Less = 60`](#f-electron2d-key-less) | Identifies `<`. |
| [`Equal = 61`](#f-electron2d-key-equal) | Identifies `=`. |
| [`Greater = 62`](#f-electron2d-key-greater) | Identifies `>`. |
| [`Question = 63`](#f-electron2d-key-question) | Identifies `?`. |
| [`At = 64`](#f-electron2d-key-at) | Identifies `@`. |
| [`A = 65`](#f-electron2d-key-a) | Identifies Latin A. |
| [`B = 66`](#f-electron2d-key-b) | Identifies Latin B. |
| [`C = 67`](#f-electron2d-key-c) | Identifies Latin C. |
| [`D = 68`](#f-electron2d-key-d) | Identifies Latin D. |
| [`E = 69`](#f-electron2d-key-e) | Identifies Latin E. |
| [`F = 70`](#f-electron2d-key-f) | Identifies Latin F. |
| [`G = 71`](#f-electron2d-key-g) | Identifies Latin G. |
| [`H = 72`](#f-electron2d-key-h) | Identifies Latin H. |
| [`I = 73`](#f-electron2d-key-i) | Identifies Latin I. |
| [`J = 74`](#f-electron2d-key-j) | Identifies Latin J. |
| [`K = 75`](#f-electron2d-key-k) | Identifies Latin K. |
| [`L = 76`](#f-electron2d-key-l) | Identifies Latin L. |
| [`M = 77`](#f-electron2d-key-m) | Identifies Latin M. |
| [`N = 78`](#f-electron2d-key-n) | Identifies Latin N. |
| [`O = 79`](#f-electron2d-key-o) | Identifies Latin O. |
| [`P = 80`](#f-electron2d-key-p) | Identifies Latin P. |
| [`Q = 81`](#f-electron2d-key-q) | Identifies Latin Q. |
| [`R = 82`](#f-electron2d-key-r) | Identifies Latin R. |
| [`S = 83`](#f-electron2d-key-s) | Identifies Latin S. |
| [`T = 84`](#f-electron2d-key-t) | Identifies Latin T. |
| [`U = 85`](#f-electron2d-key-u) | Identifies Latin U. |
| [`V = 86`](#f-electron2d-key-v) | Identifies Latin V. |
| [`W = 87`](#f-electron2d-key-w) | Identifies Latin W. |
| [`X = 88`](#f-electron2d-key-x) | Identifies Latin X. |
| [`Y = 89`](#f-electron2d-key-y) | Identifies Latin Y. |
| [`Z = 90`](#f-electron2d-key-z) | Identifies Latin Z. |
| [`BracketLeft = 91`](#f-electron2d-key-bracketleft) | Identifies `[`. |
| [`Backslash = 92`](#f-electron2d-key-backslash) | Identifies a backslash. |
| [`BracketRight = 93`](#f-electron2d-key-bracketright) | Identifies `]`. |
| [`AsciiCircumflex = 94`](#f-electron2d-key-asciicircumflex) | Identifies `^`. |
| [`Underscore = 95`](#f-electron2d-key-underscore) | Identifies `_`. |
| [`QuoteLeft = 96`](#f-electron2d-key-quoteleft) | Identifies a grave accent. |
| [`BraceLeft = 123`](#f-electron2d-key-braceleft) | Identifies `{`. |
| [`Bar = 124`](#f-electron2d-key-bar) | Identifies `\|`. |
| [`BraceRight = 125`](#f-electron2d-key-braceright) | Identifies `}`. |
| [`AsciiTilde = 126`](#f-electron2d-key-asciitilde) | Identifies `~`. |
| [`Yen = 165`](#f-electron2d-key-yen) | Identifies the yen sign. |
| [`Section = 167`](#f-electron2d-key-section) | Identifies the section sign. |

## Constant Descriptions

<a id="f-electron2d-key-none"></a>
### `None = 0`

Identifies no key.

<a id="f-electron2d-key-special"></a>
### `Special = 4194304`

Marks the beginning of the non-Unicode special-key range.

<a id="f-electron2d-key-escape"></a>
### `Escape = 4194305`

Identifies Escape.

<a id="f-electron2d-key-tab"></a>
### `Tab = 4194306`

Identifies Tab.

<a id="f-electron2d-key-backtab"></a>
### `Backtab = 4194307`

Identifies reverse Tab.

<a id="f-electron2d-key-backspace"></a>
### `Backspace = 4194308`

Identifies Backspace.

<a id="f-electron2d-key-enter"></a>
### `Enter = 4194309`

Identifies Enter or Return.

<a id="f-electron2d-key-keypadenter"></a>
### `KeypadEnter = 4194310`

Identifies the numeric-keypad Enter key.

<a id="f-electron2d-key-insert"></a>
### `Insert = 4194311`

Identifies Insert.

<a id="f-electron2d-key-delete"></a>
### `Delete = 4194312`

Identifies Delete.

<a id="f-electron2d-key-pause"></a>
### `Pause = 4194313`

Identifies Pause.

<a id="f-electron2d-key-print"></a>
### `Print = 4194314`

Identifies Print Screen.

<a id="f-electron2d-key-systemrequest"></a>
### `SystemRequest = 4194315`

Identifies System Request.

<a id="f-electron2d-key-clear"></a>
### `Clear = 4194316`

Identifies Clear.

<a id="f-electron2d-key-home"></a>
### `Home = 4194317`

Identifies Home.

<a id="f-electron2d-key-end"></a>
### `End = 4194318`

Identifies End.

<a id="f-electron2d-key-left"></a>
### `Left = 4194319`

Identifies the left arrow.

<a id="f-electron2d-key-up"></a>
### `Up = 4194320`

Identifies the up arrow.

<a id="f-electron2d-key-right"></a>
### `Right = 4194321`

Identifies the right arrow.

<a id="f-electron2d-key-down"></a>
### `Down = 4194322`

Identifies the down arrow.

<a id="f-electron2d-key-pageup"></a>
### `PageUp = 4194323`

Identifies Page Up.

<a id="f-electron2d-key-pagedown"></a>
### `PageDown = 4194324`

Identifies Page Down.

<a id="f-electron2d-key-shift"></a>
### `Shift = 4194325`

Identifies Shift.

<a id="f-electron2d-key-control"></a>
### `Control = 4194326`

Identifies Control.

<a id="f-electron2d-key-meta"></a>
### `Meta = 4194327`

Identifies Meta, Command, Windows, or Super.

<a id="f-electron2d-key-alt"></a>
### `Alt = 4194328`

Identifies Alt or Option.

<a id="f-electron2d-key-capslock"></a>
### `CapsLock = 4194329`

Identifies Caps Lock.

<a id="f-electron2d-key-numlock"></a>
### `NumLock = 4194330`

Identifies Num Lock.

<a id="f-electron2d-key-scrolllock"></a>
### `ScrollLock = 4194331`

Identifies Scroll Lock.

<a id="f-electron2d-key-f1"></a>
### `F1 = 4194332`

Identifies function key F1.

<a id="f-electron2d-key-f2"></a>
### `F2 = 4194333`

Identifies function key F2.

<a id="f-electron2d-key-f3"></a>
### `F3 = 4194334`

Identifies function key F3.

<a id="f-electron2d-key-f4"></a>
### `F4 = 4194335`

Identifies function key F4.

<a id="f-electron2d-key-f5"></a>
### `F5 = 4194336`

Identifies function key F5.

<a id="f-electron2d-key-f6"></a>
### `F6 = 4194337`

Identifies function key F6.

<a id="f-electron2d-key-f7"></a>
### `F7 = 4194338`

Identifies function key F7.

<a id="f-electron2d-key-f8"></a>
### `F8 = 4194339`

Identifies function key F8.

<a id="f-electron2d-key-f9"></a>
### `F9 = 4194340`

Identifies function key F9.

<a id="f-electron2d-key-f10"></a>
### `F10 = 4194341`

Identifies function key F10.

<a id="f-electron2d-key-f11"></a>
### `F11 = 4194342`

Identifies function key F11.

<a id="f-electron2d-key-f12"></a>
### `F12 = 4194343`

Identifies function key F12.

<a id="f-electron2d-key-f13"></a>
### `F13 = 4194344`

Identifies function key F13.

<a id="f-electron2d-key-f14"></a>
### `F14 = 4194345`

Identifies function key F14.

<a id="f-electron2d-key-f15"></a>
### `F15 = 4194346`

Identifies function key F15.

<a id="f-electron2d-key-f16"></a>
### `F16 = 4194347`

Identifies function key F16.

<a id="f-electron2d-key-f17"></a>
### `F17 = 4194348`

Identifies function key F17.

<a id="f-electron2d-key-f18"></a>
### `F18 = 4194349`

Identifies function key F18.

<a id="f-electron2d-key-f19"></a>
### `F19 = 4194350`

Identifies function key F19.

<a id="f-electron2d-key-f20"></a>
### `F20 = 4194351`

Identifies function key F20.

<a id="f-electron2d-key-f21"></a>
### `F21 = 4194352`

Identifies function key F21.

<a id="f-electron2d-key-f22"></a>
### `F22 = 4194353`

Identifies function key F22.

<a id="f-electron2d-key-f23"></a>
### `F23 = 4194354`

Identifies function key F23.

<a id="f-electron2d-key-f24"></a>
### `F24 = 4194355`

Identifies function key F24.

<a id="f-electron2d-key-f25"></a>
### `F25 = 4194356`

Identifies function key F25.

<a id="f-electron2d-key-f26"></a>
### `F26 = 4194357`

Identifies function key F26.

<a id="f-electron2d-key-f27"></a>
### `F27 = 4194358`

Identifies function key F27.

<a id="f-electron2d-key-f28"></a>
### `F28 = 4194359`

Identifies function key F28.

<a id="f-electron2d-key-f29"></a>
### `F29 = 4194360`

Identifies function key F29.

<a id="f-electron2d-key-f30"></a>
### `F30 = 4194361`

Identifies function key F30.

<a id="f-electron2d-key-f31"></a>
### `F31 = 4194362`

Identifies function key F31.

<a id="f-electron2d-key-f32"></a>
### `F32 = 4194363`

Identifies function key F32.

<a id="f-electron2d-key-f33"></a>
### `F33 = 4194364`

Identifies function key F33.

<a id="f-electron2d-key-f34"></a>
### `F34 = 4194365`

Identifies function key F34.

<a id="f-electron2d-key-f35"></a>
### `F35 = 4194366`

Identifies function key F35.

<a id="f-electron2d-key-menu"></a>
### `Menu = 4194370`

Identifies the context-menu key.

<a id="f-electron2d-key-hyper"></a>
### `Hyper = 4194371`

Identifies Hyper.

<a id="f-electron2d-key-help"></a>
### `Help = 4194373`

Identifies Help.

<a id="f-electron2d-key-back"></a>
### `Back = 4194376`

Identifies browser Back.

<a id="f-electron2d-key-forward"></a>
### `Forward = 4194377`

Identifies browser Forward.

<a id="f-electron2d-key-stop"></a>
### `Stop = 4194378`

Identifies browser Stop.

<a id="f-electron2d-key-refresh"></a>
### `Refresh = 4194379`

Identifies browser Refresh.

<a id="f-electron2d-key-volumedown"></a>
### `VolumeDown = 4194380`

Identifies volume down.

<a id="f-electron2d-key-volumemute"></a>
### `VolumeMute = 4194381`

Identifies volume mute.

<a id="f-electron2d-key-volumeup"></a>
### `VolumeUp = 4194382`

Identifies volume up.

<a id="f-electron2d-key-mediaplay"></a>
### `MediaPlay = 4194388`

Identifies media play.

<a id="f-electron2d-key-mediastop"></a>
### `MediaStop = 4194389`

Identifies media stop.

<a id="f-electron2d-key-mediaprevious"></a>
### `MediaPrevious = 4194390`

Identifies previous media.

<a id="f-electron2d-key-medianext"></a>
### `MediaNext = 4194391`

Identifies next media.

<a id="f-electron2d-key-mediarecord"></a>
### `MediaRecord = 4194392`

Identifies media record.

<a id="f-electron2d-key-homepage"></a>
### `HomePage = 4194393`

Identifies the home-page key.

<a id="f-electron2d-key-favorites"></a>
### `Favorites = 4194394`

Identifies favorites.

<a id="f-electron2d-key-search"></a>
### `Search = 4194395`

Identifies search.

<a id="f-electron2d-key-standby"></a>
### `Standby = 4194396`

Identifies standby.

<a id="f-electron2d-key-openurl"></a>
### `OpenUrl = 4194397`

Identifies open URL.

<a id="f-electron2d-key-launchmail"></a>
### `LaunchMail = 4194398`

Identifies launch mail.

<a id="f-electron2d-key-launchmedia"></a>
### `LaunchMedia = 4194399`

Identifies launch media.

<a id="f-electron2d-key-launch0"></a>
### `Launch0 = 4194400`

Identifies application-launch key zero.

<a id="f-electron2d-key-launch1"></a>
### `Launch1 = 4194401`

Identifies application-launch key one.

<a id="f-electron2d-key-launch2"></a>
### `Launch2 = 4194402`

Identifies application-launch key two.

<a id="f-electron2d-key-launch3"></a>
### `Launch3 = 4194403`

Identifies application-launch key three.

<a id="f-electron2d-key-launch4"></a>
### `Launch4 = 4194404`

Identifies application-launch key four.

<a id="f-electron2d-key-launch5"></a>
### `Launch5 = 4194405`

Identifies application-launch key five.

<a id="f-electron2d-key-launch6"></a>
### `Launch6 = 4194406`

Identifies application-launch key six.

<a id="f-electron2d-key-launch7"></a>
### `Launch7 = 4194407`

Identifies application-launch key seven.

<a id="f-electron2d-key-launch8"></a>
### `Launch8 = 4194408`

Identifies application-launch key eight.

<a id="f-electron2d-key-launch9"></a>
### `Launch9 = 4194409`

Identifies application-launch key nine.

<a id="f-electron2d-key-launcha"></a>
### `LaunchA = 4194410`

Identifies application-launch key A.

<a id="f-electron2d-key-launchb"></a>
### `LaunchB = 4194411`

Identifies application-launch key B.

<a id="f-electron2d-key-launchc"></a>
### `LaunchC = 4194412`

Identifies application-launch key C.

<a id="f-electron2d-key-launchd"></a>
### `LaunchD = 4194413`

Identifies application-launch key D.

<a id="f-electron2d-key-launche"></a>
### `LaunchE = 4194414`

Identifies application-launch key E.

<a id="f-electron2d-key-launchf"></a>
### `LaunchF = 4194415`

Identifies application-launch key F.

<a id="f-electron2d-key-globe"></a>
### `Globe = 4194416`

Identifies the globe key.

<a id="f-electron2d-key-keyboard"></a>
### `Keyboard = 4194417`

Identifies the on-screen-keyboard key.

<a id="f-electron2d-key-jiseisu"></a>
### `JisEisu = 4194418`

Identifies the Japanese alphanumeric key.

<a id="f-electron2d-key-jiskana"></a>
### `JisKana = 4194419`

Identifies the Japanese kana key.

<a id="f-electron2d-key-keypadmultiply"></a>
### `KeypadMultiply = 4194433`

Identifies numeric-keypad multiplication.

<a id="f-electron2d-key-keypaddivide"></a>
### `KeypadDivide = 4194434`

Identifies numeric-keypad division.

<a id="f-electron2d-key-keypadsubtract"></a>
### `KeypadSubtract = 4194435`

Identifies numeric-keypad subtraction.

<a id="f-electron2d-key-keypadperiod"></a>
### `KeypadPeriod = 4194436`

Identifies the numeric-keypad decimal separator.

<a id="f-electron2d-key-keypadadd"></a>
### `KeypadAdd = 4194437`

Identifies numeric-keypad addition.

<a id="f-electron2d-key-keypad0"></a>
### `Keypad0 = 4194438`

Identifies numeric-keypad zero.

<a id="f-electron2d-key-keypad1"></a>
### `Keypad1 = 4194439`

Identifies numeric-keypad one.

<a id="f-electron2d-key-keypad2"></a>
### `Keypad2 = 4194440`

Identifies numeric-keypad two.

<a id="f-electron2d-key-keypad3"></a>
### `Keypad3 = 4194441`

Identifies numeric-keypad three.

<a id="f-electron2d-key-keypad4"></a>
### `Keypad4 = 4194442`

Identifies numeric-keypad four.

<a id="f-electron2d-key-keypad5"></a>
### `Keypad5 = 4194443`

Identifies numeric-keypad five.

<a id="f-electron2d-key-keypad6"></a>
### `Keypad6 = 4194444`

Identifies numeric-keypad six.

<a id="f-electron2d-key-keypad7"></a>
### `Keypad7 = 4194445`

Identifies numeric-keypad seven.

<a id="f-electron2d-key-keypad8"></a>
### `Keypad8 = 4194446`

Identifies numeric-keypad eight.

<a id="f-electron2d-key-keypad9"></a>
### `Keypad9 = 4194447`

Identifies numeric-keypad nine.

<a id="f-electron2d-key-unknown"></a>
### `Unknown = 8388607`

Identifies an unknown key.

<a id="f-electron2d-key-space"></a>
### `Space = 32`

Identifies Space.

<a id="f-electron2d-key-exclamation"></a>
### `Exclamation = 33`

Identifies `!`.

<a id="f-electron2d-key-quotedouble"></a>
### `QuoteDouble = 34`

Identifies a double quote.

<a id="f-electron2d-key-numbersign"></a>
### `NumberSign = 35`

Identifies `#`.

<a id="f-electron2d-key-dollar"></a>
### `Dollar = 36`

Identifies `$`.

<a id="f-electron2d-key-percent"></a>
### `Percent = 37`

Identifies `%`.

<a id="f-electron2d-key-ampersand"></a>
### `Ampersand = 38`

Identifies `&`.

<a id="f-electron2d-key-apostrophe"></a>
### `Apostrophe = 39`

Identifies an apostrophe.

<a id="f-electron2d-key-parenthesisleft"></a>
### `ParenthesisLeft = 40`

Identifies `(`.

<a id="f-electron2d-key-parenthesisright"></a>
### `ParenthesisRight = 41`

Identifies `)`.

<a id="f-electron2d-key-asterisk"></a>
### `Asterisk = 42`

Identifies `*`.

<a id="f-electron2d-key-plus"></a>
### `Plus = 43`

Identifies `+`.

<a id="f-electron2d-key-comma"></a>
### `Comma = 44`

Identifies `,`.

<a id="f-electron2d-key-minus"></a>
### `Minus = 45`

Identifies `-`.

<a id="f-electron2d-key-period"></a>
### `Period = 46`

Identifies `.`.

<a id="f-electron2d-key-slash"></a>
### `Slash = 47`

Identifies `/`.

<a id="f-electron2d-key-key0"></a>
### `Key0 = 48`

Identifies digit zero.

<a id="f-electron2d-key-key1"></a>
### `Key1 = 49`

Identifies digit one.

<a id="f-electron2d-key-key2"></a>
### `Key2 = 50`

Identifies digit two.

<a id="f-electron2d-key-key3"></a>
### `Key3 = 51`

Identifies digit three.

<a id="f-electron2d-key-key4"></a>
### `Key4 = 52`

Identifies digit four.

<a id="f-electron2d-key-key5"></a>
### `Key5 = 53`

Identifies digit five.

<a id="f-electron2d-key-key6"></a>
### `Key6 = 54`

Identifies digit six.

<a id="f-electron2d-key-key7"></a>
### `Key7 = 55`

Identifies digit seven.

<a id="f-electron2d-key-key8"></a>
### `Key8 = 56`

Identifies digit eight.

<a id="f-electron2d-key-key9"></a>
### `Key9 = 57`

Identifies digit nine.

<a id="f-electron2d-key-colon"></a>
### `Colon = 58`

Identifies `:`.

<a id="f-electron2d-key-semicolon"></a>
### `Semicolon = 59`

Identifies `;`.

<a id="f-electron2d-key-less"></a>
### `Less = 60`

Identifies `<`.

<a id="f-electron2d-key-equal"></a>
### `Equal = 61`

Identifies `=`.

<a id="f-electron2d-key-greater"></a>
### `Greater = 62`

Identifies `>`.

<a id="f-electron2d-key-question"></a>
### `Question = 63`

Identifies `?`.

<a id="f-electron2d-key-at"></a>
### `At = 64`

Identifies `@`.

<a id="f-electron2d-key-a"></a>
### `A = 65`

Identifies Latin A.

<a id="f-electron2d-key-b"></a>
### `B = 66`

Identifies Latin B.

<a id="f-electron2d-key-c"></a>
### `C = 67`

Identifies Latin C.

<a id="f-electron2d-key-d"></a>
### `D = 68`

Identifies Latin D.

<a id="f-electron2d-key-e"></a>
### `E = 69`

Identifies Latin E.

<a id="f-electron2d-key-f"></a>
### `F = 70`

Identifies Latin F.

<a id="f-electron2d-key-g"></a>
### `G = 71`

Identifies Latin G.

<a id="f-electron2d-key-h"></a>
### `H = 72`

Identifies Latin H.

<a id="f-electron2d-key-i"></a>
### `I = 73`

Identifies Latin I.

<a id="f-electron2d-key-j"></a>
### `J = 74`

Identifies Latin J.

<a id="f-electron2d-key-k"></a>
### `K = 75`

Identifies Latin K.

<a id="f-electron2d-key-l"></a>
### `L = 76`

Identifies Latin L.

<a id="f-electron2d-key-m"></a>
### `M = 77`

Identifies Latin M.

<a id="f-electron2d-key-n"></a>
### `N = 78`

Identifies Latin N.

<a id="f-electron2d-key-o"></a>
### `O = 79`

Identifies Latin O.

<a id="f-electron2d-key-p"></a>
### `P = 80`

Identifies Latin P.

<a id="f-electron2d-key-q"></a>
### `Q = 81`

Identifies Latin Q.

<a id="f-electron2d-key-r"></a>
### `R = 82`

Identifies Latin R.

<a id="f-electron2d-key-s"></a>
### `S = 83`

Identifies Latin S.

<a id="f-electron2d-key-t"></a>
### `T = 84`

Identifies Latin T.

<a id="f-electron2d-key-u"></a>
### `U = 85`

Identifies Latin U.

<a id="f-electron2d-key-v"></a>
### `V = 86`

Identifies Latin V.

<a id="f-electron2d-key-w"></a>
### `W = 87`

Identifies Latin W.

<a id="f-electron2d-key-x"></a>
### `X = 88`

Identifies Latin X.

<a id="f-electron2d-key-y"></a>
### `Y = 89`

Identifies Latin Y.

<a id="f-electron2d-key-z"></a>
### `Z = 90`

Identifies Latin Z.

<a id="f-electron2d-key-bracketleft"></a>
### `BracketLeft = 91`

Identifies `[`.

<a id="f-electron2d-key-backslash"></a>
### `Backslash = 92`

Identifies a backslash.

<a id="f-electron2d-key-bracketright"></a>
### `BracketRight = 93`

Identifies `]`.

<a id="f-electron2d-key-asciicircumflex"></a>
### `AsciiCircumflex = 94`

Identifies `^`.

<a id="f-electron2d-key-underscore"></a>
### `Underscore = 95`

Identifies `_`.

<a id="f-electron2d-key-quoteleft"></a>
### `QuoteLeft = 96`

Identifies a grave accent.

<a id="f-electron2d-key-braceleft"></a>
### `BraceLeft = 123`

Identifies `{`.

<a id="f-electron2d-key-bar"></a>
### `Bar = 124`

Identifies `|`.

<a id="f-electron2d-key-braceright"></a>
### `BraceRight = 125`

Identifies `}`.

<a id="f-electron2d-key-asciitilde"></a>
### `AsciiTilde = 126`

Identifies `~`.

<a id="f-electron2d-key-yen"></a>
### `Yen = 165`

Identifies the yen sign.

<a id="f-electron2d-key-section"></a>
### `Section = 167`

Identifies the section sign.
