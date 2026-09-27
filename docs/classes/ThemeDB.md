# ThemeDB

Last updated: 2026-09-27

**Inherits:** [ElectronObject](ElectronObject.md) · **Inherited By:** —

**Declaration:** `public sealed class ThemeDB : ElectronObject` · **Source:** [ThemeDB.cs](../../src/Scene/Resources/ThemeDB.cs) · **Component:** [Typed themes](../components/themes.md)

## Description and example

The process-wide service for the current built-in theme and universal typed fallbacks. Consumers borrow the singleton and its built-in resources; they do not own their lifetime. It supplies real data for implemented controls while the complete GUI default catalog and project Theme-file loading remain separate dependencies.

```csharp
Theme builtIn = ThemeDB.Instance.GetDefaultTheme();
StyleBox? panelStyle = builtIn.GetStyleBox("panel", "Panel");
int gap = builtIn.GetConstant("separation", "BoxContainer"); // 4 initially
```

The returned Theme is mutable and shared. Its changes reach scene theme owners through their deferred invalidation path. Changing the default resource affects all consumers that resolve through it.

## API summary

| Signature | Contract/default |
| --- | --- |
| `public static ThemeDB Instance { get; }` | Lazy process singleton; borrowed by callers. |
| `public Theme GetDefaultTheme()` | Borrowed current built-in Theme. |
| `public float FallbackBaseScale { get; set; }` | Finite signed final fallback, initially 1. |
| `public Font? FallbackFont { get; set; }` | Embedded Open Sans SemiBold initially; explicit null allowed. |
| `public int FallbackFontSize { get; set; }` | Signed integer final fallback, initially 16. |
| `public Texture? FallbackIcon { get; set; }` | Lazy built-in 16×16 error icon; explicit null allowed. |
| `public StyleBox? FallbackStyleBox { get; set; }` | Built-in hollow error style; explicit null allowed. |
| `public event Action? FallbackChanged` | Emitted after a committed fallback replacement. |
| `protected override void Dispose(bool disposing)` | Releases service-owned defaults and event subscriptions. |

There is no public constructor or GetProjectTheme null stub.

## Property and method descriptions

<a id="instance"></a><a id="getdefaulttheme"></a>
**Singleton/default Theme:** creation is lazy and serialized; construction decodes the five slider SVG icons once through the existing image codec. Failure releases partial owned defaults and propagates. The fallback error icon remains separately lazy. Construction owns the embedded Open Sans SemiBold resource; its native face is loaded only by the first text query. GetDefaultTheme returns the same mutable Theme, rejecting a disposed service or default Theme. Its current built-in data is:

| Entry | Initial value |
| --- | --- |
| DefaultBaseScale / DefaultFontSize | 1 / 16. |
| DefaultFont | Service-owned embedded Open Sans SemiBold, initially also FallbackFont. |
| Label font/font_size | Null/-1 slots resolve the defaults. |
| Label styles | Empty `normal`; white alpha0.75 outline `focus` with margins4, radius3, detail5, borders2 and expansion2. |
| Label text colors/constants | White font, transparent shadow, black outline; shadow offsets1/1, outline0, shadow-outline1 and line-spacing3; absent paragraph spacing resolves constant fallback0. |
| `Panel/panel` and `PanelContainer/panel` styles | Separate StyleBoxFlat instances; BGColor `(0.1, 0.1, 0.1, 0.6)`, zero content margins, corner radius 3, detail 5. |
| BoxContainer/HBoxContainer/VBoxContainer `separation` | 4. |
| GridContainer `h_separation` / `v_separation` | 4 / 4. |
| HSlider/VSlider `slider`, `grabber_area`, `grabber_area_highlight` | Three shared StyleBoxFlat resources: content margins/radius 4, detail 6; track `(0.1,0.1,0.1,0.6)`, white fill alpha 0.4, white highlighted fill alpha 0.75. |
| HSlider/VSlider grabber icons | Shared 16×16 circles with normal/highlight/disabled alpha 0.75/1/0.37. |
| HSlider `tick` / VSlider `tick` | 4×8 horizontal-control tick and 8×4 vertical-control tick icons. |
| Slider `center_grabber`, `grabber_offset`, `tick_offset` | Zero for both concrete orientations. |

The default slider icons currently use scale one; nonunit default-theme/DPI construction and refresh remain incomplete. The missing standard data for other GUI families is a real coverage gap. Their complete first consumer slices must add the matching styles/colors/constants/icons, while the current Font slice supplies the embedded font and Label defaults. This table is not a claim of a complete upstream default theme.

<a id="fallbackbasescale"></a><a id="fallbackfontsize"></a>
**Scalar fallbacks:** used only after applicable themes provide no positive default/value. Base scale requires a finite value and otherwise preserves its sign; font size preserves signed integer state. Equal assignments are silent. Nonfinite scale throws ArgumentOutOfRangeException. These values do not load fonts or automatically rescale existing styles/constants.

<a id="fallbackfont"></a>
**Font:** the initial fallback and built-in Theme.DefaultFont share the embedded Open Sans SemiBold resource. Replacement borrows a live Font or stores explicit null; disposed assignments throw ObjectDisposedException and equal identities are silent. Replacing FallbackFont does not rewrite Theme.DefaultFont. Owner lookup reaches this universal fallback only after branch and built-in defaults provide no font. Existing resource identities are not silently replaced when externally disposed; drawing observes their lifetime guards. The service disposes only its original owned default assets, not user-supplied fonts.

<a id="fallbackicon"></a>
**Icon:** the first unoverridden read decodes the built-in 16×16 SVG through the existing image-codec path and creates a service-owned ImageTexture. A supplied Texture is borrowed, and assigning null before that first read intentionally suppresses lazy creation. Assigned disposed textures throw ObjectDisposedException. Explicitly disposing a stored borrowed texture does not transfer ownership or silently substitute another resource. Lazy decode inherits the SVG codec's availability/error contract. The default icon currently uses scale one; DPI/default-theme-scale-aware asset creation and refresh remain a precise Partial dependency, not a claim of nonunit scaling.

<a id="fallbackstylebox"></a>
**Style:** initially a hollow StyleBoxFlat with two-pixel borders, four-pixel content margins, detail one, DrawCenter=false and error-color background configuration. Setting another live style borrows it; null is an explicit empty fallback. Assigned disposed styles throw ObjectDisposedException. Consumers that require a concrete style, such as Panel drawing, fail explicitly if final lookup returns null.

<a id="fallbackchanged"></a>
**FallbackChanged:** equal initialized value/reference assignments are silent. A changed value commits before internal owner invalidation and then the public event; both required notification phases are attempted, and failures are combined after commitment. Event delivery is synchronous on the setter's thread. Scene theme owners enqueue their work instead of mutating nodes from a resource thread. Mutating the built-in Theme publishes its own change path rather than pretending a fallback value was replaced.

<a id="dispose"></a>
**Cleanup:** the service owns its built-in Theme, styles, icons and original embedded default font. It unsubscribes default-theme notifications, clears events and releases those owned resources during disposal. Caller-assigned fallback resources are never disposed by replacement or cleanup. Callers normally borrow the service for the process lifetime and must not dispose its shared defaults.

## Dependencies and verification

GetProjectTheme remains absent until a Theme resource-file loader and typed `gui/theme/custom` startup validation/ownership policy return actual loaded state. Font fallback and font objects execute in the FreeType/HarfBuzz Font resource/loading/shaping slice. Missing GUI default data remains Partial; font theme storage and lookup are executable.

[ThemeResourceTests](../../tests/Electron2D.Tests/ThemeResourceTests.cs) verifies typed values, placeholders, alias subscriptions, variations, merge/copy, guards and concurrency; [ThemeLookupTests](../../tests/Electron2D.Tests/ThemeLookupTests.cs) verifies owner priority, deferred/detached caches, batching, reentry, fallback policy and typed override packing. [PanelContainerTests](../../tests/Electron2D.Tests/PanelContainerTests.cs) verifies defaults, background draw order, content bounds, eligibility, failure continuation and sorting after failed theme callbacks. Resource updates and active lookup pass 64 warmed cycles with zero managed bytes. [ThemePanelRenderingTests](../../tests/Electron2D.Tests/ThemePanelRenderingTests.cs) verifies seven visual phases and 64 warmed notification/layout/recording/render frames with zero managed bytes from ProcessFrameStarted through FramePostDraw on Linux Wayland GPU and compatibility. Native allocator counts, large-GUI performance, nonunit default-icon scaling, other platforms and owner acceptance remain unverified. See [coverage](../coverage/classes/ThemeDB.md), [Theme](Theme.md) and [ADR 0083](../decisions/rendering.md#adr-0083). The built-in font and icons retain their [runtime attribution notice](../../licence/THIRD_PARTY_NOTICES.md).
