# LinkButton

Last updated: 2026-10-23

**Namespace:** `Electron2D`. **Declaration:** `public partial class LinkButton : BaseButton`. **Inherits:** [BaseButton](BaseButton.md). **Inherited By:** —. **Source:** [API/storage](../../src/Scene/GUI/LinkButton.cs), [layout](../../src/Scene/GUI/LinkButton.Layout.cs). **Component:** [GUI buttons](../components/gui-buttons.md).

Displays a translated, shaped text link. Pointer, touch, action, shortcut and toggle semantics come from BaseButton. A nonempty `URI` opens through `OS.ShellOpen` before the inherited `Pressed` event. An empty URI still permits application callbacks. Setting URI never starts an application. Platform failures follow the inherited callback aggregation policy; remaining activation observers are attempted.

Fonts and focus styles are borrowed. Attached access requires the scene owner thread and authored mutation is forbidden during capture. Fonts are checked by identity and content generation; changes from workers defer invalidation to the owner. Detach/reentry and disposal retire pending callbacks and font subscriptions. A minimum-size query cannot consume the pending redraw obligation. Text shaping prepares storage; unchanged layouts and warmed recording reuse it.

The default `FocusMode.Accessibility` preserves the specialized focus policy: the current accessibility service is absent. Choose `FocusMode.All` for ordinary keyboard/controller navigation. The pointing-hand cursor becomes an arrow when disabled. The natural text line determines minimum size without padding or background; trimming releases minimum width. Layout RTL aligns the line at the right edge, independently of the paragraph's writing direction.

## Example

```csharp
var root = new Window { Size = new(640, 360) };
var link = new LinkButton("Documentation")
{
    Position = new(20, 20),
    URI = "https://example.com/docs",
    FocusMode = FocusMode.All,
    Underline = LinkButton.UnderlineMode.OnHover
};
root.AddChild(link);
Engine.Run(root);
```

The containing application supplies the host and chooses whether activating this URI is appropriate.

## Constructors

| Signature | Description |
| --- | --- |
| `public LinkButton()` | Empty link, accessibility focus, pointing cursor, always underline. |
| `public LinkButton(string text)` | Initial untranslated text; null throws `ArgumentNullException`. |

## Properties

| Signature | Default |
| --- | --- |
| `public string Text { get; set; }` | `""` |
| `public string URI { get; set; }` | `""` |
| `public UnderlineMode Underline { get; set; }` | `Always` |
| `public string Language { get; set; }` | `""` |
| `public TextDirection TextDirection { get; set; }` | `Auto` |
| `public TextOverrunBehavior TextOverrunBehavior { get; set; }` | `NoTrimming` |
| `public string EllipsisChar { get; set; }` | `"…"` |
| `public StructuredTextParser StructuredTextBIDIOverride { get; set; }` | `Default` |
| `public string[] StructuredTextBIDIOverrideOptions { get; set; }` | Independent empty array |

## Property Descriptions

### Text

Untranslated caption; translation uses `Atr` and `TranslationContext`. Equal writes refresh changed translation. Enter-tree and translation notifications refresh the prepared caption. Null is rejected. Native shaping uses the same FreeType/HarfBuzz and typed BiDi layout as other text controls.

### URI

A protocol URI or filesystem path. Empty suppresses the platform operation. Null is rejected at assignment; `OS.ShellOpen` validates nonempty identifiers and rejects NUL at activation. It normalizes ordinary paths. The identifier persists with the scene; callback delegates and activation state do not. Opening is a cold external operation, outside the repeated render budget.

### Underline

`Always` draws normal and disabled underlines too; `OnHover` draws hovered and pressed/shortcut-feedback states; `Never` suppresses all underlines. Unknown numeric values retain the active-state policy. The line uses font underline position/thickness (at least one pixel), theme spacing and the current text color.

### Language

Empty selects the translation-domain locale, culture, then tool locale. Null and NUL are rejected. Changes rebuild shaping.

### TextDirection

Accepts -1 through 3; undefined values throw `ArgumentOutOfRangeException`. Inherited follows layout direction; negative legacy automatic value uses Auto. Direction changes rebuild shaping.

### TextOverrunBehavior

NoTrimming preserves natural width. Other values fit at least one logical pixel of control width and return zero minimum width. Known values apply cluster/word/ellipsis rules; unknown values use ordinary clipping. Resize rebuilds a trimmed line.

### EllipsisChar

Retains only the first Unicode scalar, preserving supplementary characters. Empty uses the default ellipsis during layout. Null or malformed initial UTF-16 is rejected before changing state.

### StructuredTextBIDIOverride

Uses the existing typed default/URI/file/email/list/custom parser contract. Custom calls inherited `OnStructuredTextParser`; ranges must fit the logical scalar text and carry defined directions. A mutation from that callback leaves the old preparation dirty rather than committing it over newer authoring.

### StructuredTextBIDIOverrideOptions

Setter and getter copy arrays; null arrays/elements are rejected. List requires exactly one delimiter. Custom receives a read-only options snapshot. Scene packing stores the copied strings.

## Enumeration Descriptions

[`LinkButton.UnderlineMode`](LinkButton.UnderlineMode.md): `Always = 0`, `OnHover = 1`, `Never = 2`.

## Themes

The built-in type supplies `font`, `font_size`, `focus`, `outline_size = 0`, `underline_spacing = 2` and all seven font-color entries. Normal text is `.875` gray, hover/focus `.95` gray, pressed white, disabled black, outline black, hover-pressed theme entry black. Drawing uses pressed color for both pressed modes, matching the accepted link behavior; the hover-pressed entry remains queryable. Focus style overlays the control without adding minimum size. Inherited typed theme lookup/overrides support project themes and type variations.

## Protected extension points

| Signature | Contract |
| --- | --- |
| `protected override void OnPressed()` | Opens nonempty URI before inherited activation observers; call base when extending. |
| `protected override CursorShape OnGetCursorShape(Vector2 atPosition)` | Disabled arrow or configured cursor. |
| `protected override Vector2 OnGetMinimumSize()` | Actual shaped geometry; zero width for trimming. |
| `protected override void OnNotification(int what)` | Owner resource polling, text invalidation and canvas drawing. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Typed authored fields and specialized inherited defaults. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Exact built-in constructor; subclasses supply their own factory. |
| `protected override void Dispose(bool disposing)` | Detaches borrowed font callbacks without disposing the font. |

## Verification and limits

The fresh-process URI fixture registers a unique protocol for each run. Linux selects its isolated executable; macOS compiles and ad-hoc signs a temporary URL-handler application with a named URL type; Windows registers a command under the current user's unique protocol key. Native launch must deliver the exact stored URI. Capture files are published by rename after writing, and platform registration is removed before temporary files are deleted. The macOS workflow selects the LinkButton fixture before its complete suite; local Linux success alone does not establish the other native paths. A rejected macOS launch reports only its own generated bundle descriptor before cleanup.

LinkButtonTests covers concrete defaults, all stored fields, supplementary/malformed ellipsis, copying, real font geometry, RTL/clipping/BiDi/custom reentry, routed pointer/action/toggle/error states, worker edits, detach/reentry, borrowed disposal and fresh compressed big-endian file activation. Linux child isolation denies its session bus and supplies a private `xdg-open` fixture, proving the actual SDL URI handoff without opening the user's browser.

The native rendering fixture checks real glyph/underline/state/RTL-ellipsis pixels and native pointer edges, with warmed frame allocation measured separately. See the component for completed backend evidence. Accessibility host integration, routed real desktop/browser acceptance, native allocator counts and other platforms retain their owning gates.

The macOS fresh-process fixture owns a unique temporary app and URI scheme. It
validates the native plist, records both applet architectures and signs/registers
the app. Its [explicit scheme claims](https://developer.apple.com/documentation/coreservices/1443240-lscopyallhandlersforurlscheme)
must contain exactly the owned bundle before assigning and reading back that
scheme's default handler; a preferred-handler fallback alone is not treated as
an explicit foreign claim. Cleanup unregisters the app and deletes its files.
Native launch remains an open target gate: earlier valid-plist/universal-app
runs still returned LSOpenCFURLRef -10814, and the first preferred-handler guard
stopped run 39947670388. The revised claims check uses direct CoreFoundation/CoreServices calls with balanced CF/native library ownership. Native run 39950803043 reports zero registered claims after the CLI command, so the fixture now registers its actual file URL with LSRegisterURL before checking claims. Run 39949194585 rejected a JXA bridge value before native binding; that interpreter path has been removed. Target execution remains required.
