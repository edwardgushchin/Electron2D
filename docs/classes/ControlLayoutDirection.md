# ControlLayoutDirection

Last updated: 2026-09-24

**Inherits:** —

- **Source:** [`src/Scene/GUI/ControlLayoutDirection.cs`](../../src/Scene/GUI/ControlLayoutDirection.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum ControlLayoutDirection`

The direction policy for [`Control`](Control.md) rectangle layout. The numeric values match the reference API.

| Value | Number | Meaning |
| --- | ---: | --- |
| `Inherited` | 0 | Use the nearest ancestor Control in the same translation domain, otherwise the application locale. |
| `ApplicationLocale` | 1 | Use the selected translation culture and a matching registered catalog. |
| `Locale` | 1 | Deprecated alias for `ApplicationLocale`. |
| `LTR` | 2 | Explicit left-to-right layout. |
| `RTL` | 3 | Explicit right-to-left layout. |
| `SystemLocale` | 4 | Use the current managed UI culture as the available system-locale source, with a matching catalog. |
| `Max` | 5 | Nonselectable sentinel. |

Explicit and inherited directions mirror live rectangles and preserve physical `Position` and `Size` writes. Locale modes use .NET culture direction and current in-memory catalogs; root/forced project policies, window inheritance, reference locale aliases and automatic refresh after a global culture change remain coverage gaps. See the [Control coverage page](../coverage/classes/Control.md).
