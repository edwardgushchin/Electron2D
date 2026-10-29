# SystemFont

Last updated: 2026-10-20

**Namespace:** Electron2D · **Declaration:** public partial class SystemFont · **Inherits:** [Font](Font.md) · **Inherited By:** — · **Source:** [SystemFont.cs](../../src/Scene/Resources/SystemFont.cs) · **Component:** [System fonts](../components/system-fonts.md)


## Antialiasing

`public FontAntialiasing Antialiasing { get; set; }` defaults to Gray and executes native Mono/Gray/LCD coverage. Source changes retire caches after active readers; copy/storage and configured variations preserve the preference. The text owner's captured LCD layout and backend capability determine component sampling. See [complete behavior and verification](../components/text.md#native-font-antialiasing-and-lcd-layout).

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
| `public bool MultichannelSignedDistanceField { get; set; }` | False; native or explicitly authored distance glyphs. |
| `public int MSDFSize { get; set; }` | 48; canonical source resolution. |
| `public int MSDFPixelRange { get; set; }` | 16; encoded distance interval. |
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

CoreText/DirectWrite providers and other missing service APIs remain exact [coverage](../coverage/classes/SystemFont.md) dependencies. Native allocator counts, foreign/device/editor and owner acceptance remain unverified. Unsupported provider profiles use the documented theme behavior; they do not claim native installed discovery. [ADR 0046](../decisions/rendering.md#adr-0046) and [ADR 0095](../decisions/singleton-services.md#adr-0095) own resource/native and retained static service boundaries.

## Native strike, autohinter and MIP policy

| Signature | Contract/default |
| --- | --- |
| `public bool DisableEmbeddedBitmaps { get; set; }` | True; bypasses native monochrome strikes. Color strikes remain available; strokes use outlines. |
| `public bool ForceAutohinter { get; set; }` | False; sets native automatic-hinting raster flags independently of unhinted shaping advances. |
| `public bool GenerateMIPMaps { get; set; }` | False; publishes complete MIP chains for ordinary glyph images and indexed pages. Sampling requires a MIP filter. |

### DisableEmbeddedBitmaps

Selects scalable monochrome outlines when true. A matching embedded monochrome strike can provide actual bitmap coverage when false; color strikes remain available in both states. Native outlines still supply stroke rasterization. Authored bitmap records and pages are preserved independently of this native-source policy.

### ForceAutohinter

Adds FreeType's forced automatic-hinting load flag to native raster requests. It does not replace the accepted unhinted shaping metric contract. Explicit no-hinting mode keeps its native semantics. FontVariation and owned system fallback sources inherit the selected policy.

### GenerateMIPMaps

Creates complete MIP levels before publishing glyph textures, including oversampled/phase rasters and copied indexed pages. A source policy change invalidates dependent layout and retires earlier native data/textures after active reads. Previously recorded immutable glyph pixels remain valid. GPU sampling consumes the generated levels; compatibility sampling still accepts nearest/linear base-level filters and rejects its existing MIP-filter capability gate.

All three properties reject disposed resources. Equal assignments are silent. Notifications occur after committed publication outside state locks; observer exceptions retain committed policy. Stored descriptors and duplication carry policy. Defaults and verification are shared with [the text component](../components/text.md#native-raster-policy-verification).

Installed matching and unresolved theme realization apply the preferences to independent owned sources. Changes retire realized data after active reads and invalidate dependent variations. Duplication/archives preserve requested preferences; loading rematches the current host. ResetState restores defaults.

The primary realized face supplies OpenType metadata through `GetSupportedFeatureList()` and `GetOTNameStrings()`. The feature snapshot contains numeric tags and typed optional labels, boolean/integer selection and hidden flags. GSUB is read before GPOS, whose duplicate descriptions win. Name strings are copied by native language spelling and standard role; unknown IDs retain `unknown_N` keys. These cold snapshots allocate independently and omit fallback metadata or authored family overrides. Feature labels retain the tool locale sampled during native realization; source replacement or cache retirement rebuilds metadata.

## MSDF property descriptions

### MultichannelSignedDistanceField

`public bool MultichannelSignedDistanceField { get; set; }` defaults to false. True generates native multichannel signed distances with true distance in alpha, then draws them through the shared GPU canvas. One source image serves different drawn sizes and shader outlines. Oversampling and subpixel phase preferences remain stored but do not alter this mode. Native generation requires a scalable outline face; caller-authored canonical pages can explicitly contain encoded distances. Owned color or non-scalable automatic fallbacks retain ordinary image sampling. Compatibility rejects actual MSDF glyph submission and cleans up the host.

### MSDFSize

`public int MSDFSize { get; set; }` defaults to 48 and chooses the canonical source pixel size. Realized source images normalize to this size and zero outline; FontFile exposes the associated indexed source-pixel API. Drawing multiplies size and baseline bearings by drawn size divided by MSDFSize. Authored ascent, descent, underline metrics, advances, kerning and cache scale project their canonical values to the requested size.

### MSDFPixelRange

`public int MSDFPixelRange { get; set; }` defaults to 16 and supplies the encoded distance interval to both generation and sampling. Select sufficient range for the desired outline within the source rectangle. Integer values remain stored preferences; source size/range outside 1..16384 fail when canonical cache/generation is used, and oversized generated images fail before publication under the 64 MiB image-byte budget.

All three properties use the existing committed publication and reader retirement contract, preserve authored entries, propagate to configured variations and owned native sources, and invalidate dependent text. Equal assignments are silent; disposed resources reject access. Duplicate/archive/reset preserve requested preferences and rematch fresh native ownership. Borrowed fallback resources retain their independent preferences.

```csharp
using var font = new SystemFont
{
    FontNames = ["sans-serif"],
    MultichannelSignedDistanceField = true,
    MSDFSize = 48,
    MSDFPixelRange = 16
};
// Borrow font through LabelSettings or the canvas Font drawing API on the GPU profile.
```

FontMSDFTests verifies publication, geometry, canonical caches, active source snapshots, configured and variable variants, authored distance pages, mipmaps, actual isolated color fallback, storage and native failure boundaries. The independent stock FreeType/msdfgen oracle compares 216 exact images/bounds, including quadratic and cubic outlines, hinting, range, size, emboldening and shear. Linux Wayland pixel hosts exercise scalar/string/variation/outline/clip/Label consumers at three source resolutions, MSAA and Compatibility capability cleanup, with 64 active/64 idle zero-managed-byte prepared intervals. Native allocator totals, foreign execution and owner acceptance remain unverified.

Type 1 PFB/PFA sources share native shaping and raster/MSDF consumers. The common file reader resolves optional PFM/AFM metrics and preserves their byte snapshot in variations and reopened native faces. Linux catalog discovery admits scalable Type 1 sources. See [source-pair behavior and verification](../components/text.md#type-1-outline-and-auxiliary-metric-sources).

## Runtime source identity

GetRID/GetRIDs realize the current matched/theme source and expose its private native cache. TextServer queries and edits reach that actual backend, independently of stored matching preferences. Native face-count queries describe the actual selected file; the resource's logical/theme face-count policy is unchanged. Matching or theme/base source changes retire the previous cache and its borrowed views, including changes detected through dependency generations after a failed observer. Source projections keep the owning SystemFont alive. Automatic fallback identities are reported by glyph/run snapshots after realization.

Native MSDF generation uses the [shared allocation domain](../coverage/performance-producers.md#msdf-owned-allocation-domain-2026-10-29), including short-lived generator buffers and failed partial edges. Output pixels are copied before native release; cached public reads do not rerun generation.
