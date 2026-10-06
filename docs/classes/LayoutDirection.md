# LayoutDirection

Last updated: 2026-09-24

**Inherits:** —

- **Source:** [`src/Scene/GUI/LayoutDirection.cs`](../../src/Scene/GUI/LayoutDirection.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum LayoutDirection`

The shared direction policy for [`Control`](Control.md) and [`Window`](Window.md) rectangle layout. The numeric values match the reference API.

| Value | Number | Meaning |
| --- | ---: | --- |
| `Inherited` | 0 | Use the nearest ancestor Control or Window in the same translation domain, otherwise the application locale. |
| `ApplicationLocale` | 1 | Use the selected translation culture and a matching registered catalog. |
| `Locale` | 1 | Deprecated alias for `ApplicationLocale`. |
| `LTR` | 2 | Explicit left-to-right layout. |
| `RTL` | 3 | Explicit right-to-left layout. |
| `SystemLocale` | 4 | Use the current managed UI culture as the available system-locale source, with a matching catalog. |
| `Max` | 5 | Nonselectable sentinel. |

Explicit and inherited directions mirror live rectangles and preserve physical `Position` and `Size` writes. Locale modes use .NET culture direction and current in-memory catalogs; root/forced project policies, reference locale aliases and automatic refresh after a global culture change remain coverage gaps. See the [Control coverage page](../coverage/classes/Control.md).
