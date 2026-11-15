# LayoutDirection

Last updated: 2026-10-12

**Inherits:** —

- **Source:** [`src/Scene/GUI/LayoutDirection.cs`](../../src/Scene/GUI/LayoutDirection.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum LayoutDirection`

The shared direction policy for [`Control`](Control.md) and [`Window`](Window.md) rectangle layout. The numeric values match the reference API.

| Value | Number | Meaning |
| --- | ---: | --- |
| `Inherited` | 0 | Use the nearest ancestor Control or Window in the same translation domain, otherwise the project root policy. |
| `ApplicationLocale` | 1 | Use the domain-selected translation culture with a matching catalog or fallback language. |
| `LTR` | 2 | Explicit left-to-right layout. |
| `RTL` | 3 | Explicit right-to-left layout. |
| `SystemLocale` | 4 | Use the current managed UI culture as the available system-locale source, with a matching catalog or fallback language. |
| `Max` | 5 | Nonselectable sentinel. |

Explicit and inherited directions mirror live rectangles and preserve physical `Position` and `Size` writes. Locale modes use .NET culture direction and current in-memory catalogs; the root/forced project policies execute through the shared Window/Control path. Global culture changes refresh the active Engine.MainLoop; independent trees and local catalog/domain edits require explicit notification. Runtime identities use CultureInfo; native OS locale acquisition beyond CurrentUICulture is outside that accepted boundary. Windows GPU/compatibility pixel checks cover root, forced and live culture-change layout; other host/package profiles and owner visual acceptance remain unverified. See the [Control coverage page](../coverage/classes/Control.md).
