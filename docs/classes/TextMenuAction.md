# TextMenuAction

Last updated: 2026-10-07

**Source:** [LineEdit.Menu.cs](../../src/Scene/GUI/LineEdit.Menu.cs). **Owners:** [LineEdit](LineEdit.md) and [TextEdit](TextEdit.md).

Shared typed editing commands under ADR 0051. Values 7 and 13 belong to popup submenu presentation; the count sentinel is omitted. EmojiAndSymbols requires the currently absent native picker.

| Name | Value | Contract |
| --- | ---: | --- |
| `Clear` | 3 | Clears the field. |
| `Copy` | 1 | Copies selected text. |
| `Cut` | 0 | Cuts selected text. |
| `DirectionAuto` | 9 | Detects text direction. |
| `DirectionInherited` | 8 | Uses inherited text direction. |
| `DirectionLTR` | 10 | Uses left-to-right text direction. |
| `DirectionRTL` | 11 | Uses right-to-left text direction. |
| `DisplayUCC` | 12 | Toggles Unicode control-character display. |
| `EmojiAndSymbols` | 30 | Requests the currently unavailable native symbol picker. |
| `InsertALM` | 21 | Inserts an Arabic letter mark. |
| `InsertFSI` | 24 | Inserts a first-strong isolate. |
| `InsertLRE` | 16 | Inserts a left-to-right embedding control. |
| `InsertLRI` | 22 | Inserts a left-to-right isolate. |
| `InsertLRM` | 14 | Inserts a left-to-right mark. |
| `InsertLRO` | 18 | Inserts a left-to-right override control. |
| `InsertPDF` | 20 | Inserts a pop-directional-formatting control. |
| `InsertPDI` | 25 | Inserts a pop-directional-isolate control. |
| `InsertRLE` | 17 | Inserts a right-to-left embedding control. |
| `InsertRLI` | 23 | Inserts a right-to-left isolate. |
| `InsertRLM` | 15 | Inserts a right-to-left mark. |
| `InsertRLO` | 19 | Inserts a right-to-left override control. |
| `InsertSHY` | 29 | Inserts a soft hyphen. |
| `InsertWJ` | 28 | Inserts a word joiner. |
| `InsertZWJ` | 26 | Inserts a zero-width joiner. |
| `InsertZWNJ` | 27 | Inserts a zero-width nonjoiner. |
| `Paste` | 2 | Pastes clipboard text. |
| `Redo` | 6 | Restores the following text state. |
| `SelectAll` | 4 | Selects all text. |
| `Undo` | 5 | Restores the preceding text state. |
