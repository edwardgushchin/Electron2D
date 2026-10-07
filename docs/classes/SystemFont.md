# SystemFont

Last updated: 2026-10-07

**Namespace:** Electron2D · **Declaration:** public class SystemFont · **Inherits:** [Font](Font.md) · **Inherited By:** — · **Source:** [SystemFont.cs](../../src/Scene/Resources/SystemFont.cs) · **Component:** [System fonts](../components/system-fonts.md)

## Description

Resolves preferred installed families, weight, width and italic style to owned ordinary FreeType/HarfBuzz data. Current Linux discovery uses optional host Fontconfig through the retained OS service; names and paths are cold queries. Unavailable catalogs or unresolved sources use the borrowed ThemeDB fallback, with weak change/disposal subscriptions and independent native realization. The theme resource itself is never disposed by this font.

Matching examines source collection faces, retains equally best logical indices and applies supported weight/width/italic variable axes when an exact style was not found. GetFaceCount exposes that logical selected subset; theme fallback reports zero. Font.FindVariation maps logical face indices to native source faces, preserves requested system default coordinates, and gives explicit user coordinates precedence. Invalid logical indices fail during realization. Settings changes invalidate owned data and retained variants, retire active readers safely, and notify Changed outside locks. Equal writes are silent. Resource disposal releases owned faces and subscriptions, preserving explicit borrowed fallbacks.

Explicit Font.Fallbacks retain precedence over automatic platform fallback. AllowSystemFallback permits additional owned native sources for missing text/graphemes; primary support queries remain observations of explicit sources. Locale and actual character coverage drive Linux matching; OS script metadata remains accepted separately. Cold discovery, file loading and first layout/glyph preparation allocate; prepared measurement/rendering reuse sources, indices, shapes and texture caches.

Archives store requested names and settings, rather than resolved machine paths or native handles. Fresh loading rematches that host. Resource duplicates copy settings/fallback graph policy and rebuild independent native ownership. ResetState returns default names/style/settings with Disabled subpixel positioning, retaining inherited Font lifecycle policy.

## Example

```csharp
using var font = new SystemFont
{
    FontNames = ["sans-serif"],
    FontWeight = 700,
    AllowSystemFallback = true
};
var label = new Label { Text = "System text / سلام" };
label.AddThemeFontOverride("font", font);
// Keep font alive while the label borrows it; add label to the application's scene.
```

## API summary

| Signature | Contract |
| --- | --- |
| `public class Electron2D.SystemFont` | Owned source resource over installed matching. |
| `public SystemFont()` | See member description. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | See member description. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | See member description. |
| `protected override System.Void Dispose(System.Boolean disposing)` | See member description. |
| `public override System.Int32 GetFaceCount()` | See member description. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | See member description. |
| `protected override System.Void OnResetState()` | See member description. |
| `public System.Boolean AllowSystemFallback { get; set; }` | See member description. |
| `public System.Boolean FontItalic { get; set; }` | See member description. |
| `public System.String[] FontNames { get; set; }` | See member description. |
| `public System.Int32 FontStretch { get; set; }` | See member description. |
| `public System.Int32 FontWeight { get; set; }` | See member description. |
| `public Electron2D.FontHinting Hinting { get; set; }` | See member description. |
| `public System.Boolean KeepRoundingRemainders { get; set; }` | See member description. |
| `public System.Boolean ModulateColorGlyphs { get; set; }` | See member description. |
| `public System.Single Oversampling { get; set; }` | See member description. |
| `public Electron2D.FontSubpixelPositioning SubpixelPositioning { get; set; }` | See member description. |

## Member descriptions

<a id="ctor"></a>
`public SystemFont()`

Creates unresolved names, weight 400, stretch 100, nonitalic, fallback enabled, Light hinting, Auto subpixel, remainder preservation, no RGB color modulation and zero oversampling. No native discovery runs until actual source use. Disposed resources reject access.

<a id="copycustomstateto"></a>
`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Copies all requested settings and applies inherited shallow/deep fallback graph policy; no native identity is shared. Disposed resources reject access.

<a id="createduplicateinstance"></a>
`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates an exact SystemFont; unhandled derived factories are rejected by inherited Resource policy. Disposed resources reject access.

<a id="dispose"></a>
`protected override System.Void Dispose(System.Boolean disposing)`

Unsubscribes theme/source handlers and retires owned native data; borrowed explicit/theme resources remain caller-owned. Disposed resources reject access.

<a id="getfacecount"></a>
`public override System.Int32 GetFaceCount()`

Returns the equally best selected logical collection-face count, or zero for the borrowed theme profile. Retained FontVariation exposes the same logical count and remaps actual source face indices. Disposed resources reject access.

<a id="getpropertydescriptors"></a>
`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Provides inherited fields plus ten typed stored requested settings. Resolved paths/data are not stored. Disposed resources reject access.

<a id="onresetstate"></a>
`protected override System.Void OnResetState()`

Clears names/style and restores source policies, using Disabled subpixel positioning; inherited font caches and Resource reset still execute. Disposed resources reject access.

<a id="allowsystemfallback"></a>
`public System.Boolean AllowSystemFallback { get; set; }`

True initially. Missing glyphs/graphemes may resolve additional owned sources through platform coverage/locale; explicit fallbacks retain priority. Disabling clears prepared automatic lookup while retaining active old readers. Disposed resources reject access.

<a id="fontitalic"></a>
`public System.Boolean FontItalic { get; set; }`

False initially; prefers italic/oblique style and can apply a supported ital design axis. Disposed resources reject access.

<a id="fontnames"></a>
`public System.String[] FontNames { get; set; }`

Independent copied ordered family array, empty initially; null/NUL and arrays above 1024 names or names above 65536 UTF16 units fail. Empty entries are skipped. Platform matching can substitute the nearest family; absence uses theme fallback. Disposed resources reject access.

<a id="fontstretch"></a>
`public System.Int32 FontStretch { get; set; }`

Preferred width percentage, 100 initially; assignments clamp to 50..200 and may apply the wdth design axis. Disposed resources reject access.

<a id="fontweight"></a>
`public System.Int32 FontWeight { get; set; }`

Preferred weight, 400 initially; assignments clamp to 100..999. Matching chooses collection metadata/style and may apply the wght design axis. GetFontWeight remains the selected intrinsic metadata query. Disposed resources reject access.

<a id="hinting"></a>
`public Electron2D.FontHinting Hinting { get; set; }`

Light initially. Controls native raster fitting independently of shaping advances; unknown values retain the existing normal fallback policy. Disposed resources reject access.

<a id="keeproundingremainders"></a>
`public System.Boolean KeepRoundingRemainders { get; set; }`

True initially. Existing unhinted shaping/rounding retains whole-pixel residuals; subpixel output preserves fractional advances. Disposed resources reject access.

<a id="modulatecolorglyphs"></a>
`public System.Boolean ModulateColorGlyphs { get; set; }`

False initially. Ordinary intrinsic color glyphs preserve RGB unless explicitly enabled; requested alpha always applies. Disposed resources reject access.

<a id="oversampling"></a>
`public System.Single Oversampling { get; set; }`

Finite signed factor, zero initially. Explicit positive per-draw factors take precedence; nonpositive source values keep ordinary sizing. Nonfinite input fails before publication. Disposed resources reject access.

<a id="subpixelpositioning"></a>
`public Electron2D.FontSubpixelPositioning SubpixelPositioning { get; set; }`

Auto initially. Existing half/quarter/threshold policy controls raster phases; unknown values use whole pixels. Reset uses Disabled. Disposed resources reject access.

## Verification and limitations

[SystemFontTests](../../tests/Electron2D.Tests/SystemFontTests.cs) exercises host matching, independent variable/collection/empty Fontconfig child catalogs, logical face/default coordinate mapping, explicit override precedence, active fallback retirement, theme replacement, copied preferences, resource duplication/fresh process and prepared measurement. Native canvas/Label/RichTextLabel and automatic Arabic fallback produce verified pixels and zero managed bytes over 64 prepared intervals on current Linux GPU/compatibility.

CoreText/DirectWrite providers, extra antialiasing/embedded-bitmap/autohinter/mipmap and MSDF policies remain exact [coverage](../coverage/classes/SystemFont.md) dependencies. Native allocator counts, foreign/device/editor and owner acceptance remain unverified. Unsupported provider profiles use the documented theme behavior; they do not claim native installed discovery. [ADR 0046](../decisions/rendering.md#adr-0046) and [ADR 0095](../decisions/singleton-services.md#adr-0095) own resource/native and retained static service boundaries.
