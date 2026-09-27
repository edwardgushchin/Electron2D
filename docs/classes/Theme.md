# Theme

Last updated: 2026-09-27

**Inherits:** [Resource](Resource.md), ElectronObject · **Inherited By:** —

**Declaration:** `public partial class Theme : Resource` · **Source:** [Theme.cs](../../src/Scene/Resources/Theme.cs), [Theme.Types.cs](../../src/Scene/Resources/Theme.Types.cs), [Theme.Storage.cs](../../src/Scene/Resources/Theme.Storage.cs) · **Component:** [Typed themes](../components/themes.md)

## Description and example

Reusable typed GUI data addressed by category, item name and exact theme type. All six categories execute: Color, Constant, Font, FontSize, Icon and StyleBox. A Theme query does not perform scene inheritance or native-type fallback: [Control](Control.md) and [Window](Window.md) add that owner lookup layer. Themes borrow font/icon/style resources and forward their changes without owning their lifetime.

```csharp
using var style = new StyleBoxFlat { BGColor = Colors.Blue };
using var theme = new Theme();
theme.SetStyleBox("panel", "Panel", style);
theme.SetConstant("separation", "BoxContainer", 8);
using var panel = new Panel { Theme = theme, Size = new Vector2(120, 60) };
// Keep theme/style alive while the caller-owned panel hierarchy consumes them.
```

## API summary

| Signature | Contract/default |
| --- | --- |
| `public Theme()` | Empty category maps; defaults 0, null and -1. |
| `public float DefaultBaseScale { get; set; }` | Finite signed value, zero initially; positive activates the default. |
| `public Font? DefaultFont { get; set; }` | Borrowed font; null initially. Equal assignments are silent. |
| `public int DefaultFontSize { get; set; }` | Signed value, -1 initially; positive activates the default. |
| `public bool HasDefaultBaseScale()` | Tests positive DefaultBaseScale. |
| `public bool HasDefaultFont()` | Tests nonnull DefaultFont identity. |
| `public bool HasDefaultFontSize()` | Tests positive DefaultFontSize. |
| `public enum DataType` | [Color=0 through Max=6](Theme.DataType.md). |
| `public void SetColor(string name, string themeType, Color color)` | Stores one typed value; valid equal writes still notify. |
| `public virtual Color GetColor(string name, string themeType)` | Gets exact-type data with the category fallback below. |
| `public bool HasColor(string name, string themeType)` | Tests category presence, independent of database fallback. |
| `public void RenameColor(string oldName, string name, string themeType)` | Requires existing source and unused destination. |
| `public void ClearColor(string name, string themeType)` | Removes an existing stored slot. |
| `public string[] GetColorList(string themeType)` | Caller-owned item-name snapshot. |
| `public string[] GetColorTypeList()` | Caller-owned category type-name snapshot. |
| `public void SetConstant(string name, string themeType, int constant)` | Stores one typed value; valid equal writes still notify. |
| `public virtual int GetConstant(string name, string themeType)` | Gets exact-type data with the category fallback below. |
| `public bool HasConstant(string name, string themeType)` | Tests category presence, independent of database fallback. |
| `public void RenameConstant(string oldName, string name, string themeType)` | Requires existing source and unused destination. |
| `public void ClearConstant(string name, string themeType)` | Removes an existing stored slot. |
| `public string[] GetConstantList(string themeType)` | Caller-owned item-name snapshot. |
| `public string[] GetConstantTypeList()` | Caller-owned category type-name snapshot. |
| `public void SetFontSize(string name, string themeType, int fontSize)` | Stores one typed value; valid equal writes still notify. |
| `public virtual int GetFontSize(string name, string themeType)` | Gets exact-type data with the category fallback below. |
| `public bool HasFontSize(string name, string themeType)` | Tests category presence, independent of database fallback. |
| `public void RenameFontSize(string oldName, string name, string themeType)` | Requires existing source and unused destination. |
| `public void ClearFontSize(string name, string themeType)` | Removes an existing stored slot. |
| `public string[] GetFontSizeList(string themeType)` | Caller-owned item-name snapshot. |
| `public string[] GetFontSizeTypeList()` | Caller-owned category type-name snapshot. |
| `public void SetFont(string name, string themeType, Font? font)` | Stores one typed value; valid equal writes still notify. |
| `public virtual Font? GetFont(string name, string themeType)` | Gets exact-type data with the category fallback below. |
| `public bool HasFont(string name, string themeType)` | Tests category presence, independent of database fallback. |
| `public void RenameFont(string oldName, string name, string themeType)` | Requires existing source and unused destination. |
| `public void ClearFont(string name, string themeType)` | Removes an existing stored slot. |
| `public string[] GetFontList(string themeType)` | Caller-owned item-name snapshot. |
| `public string[] GetFontTypeList()` | Caller-owned category type-name snapshot. |
| `public void SetIcon(string name, string themeType, Texture? texture)` | Stores one typed value; valid equal writes still notify. |
| `public virtual Texture? GetIcon(string name, string themeType)` | Gets exact-type data with the category fallback below. |
| `public bool HasIcon(string name, string themeType)` | Tests category presence, independent of database fallback. |
| `public void RenameIcon(string oldName, string name, string themeType)` | Requires existing source and unused destination. |
| `public void ClearIcon(string name, string themeType)` | Removes an existing stored slot. |
| `public string[] GetIconList(string themeType)` | Caller-owned item-name snapshot. |
| `public string[] GetIconTypeList()` | Caller-owned category type-name snapshot. |
| `public void SetStyleBox(string name, string themeType, StyleBox? styleBox)` | Stores one typed value; valid equal writes still notify. |
| `public virtual StyleBox? GetStyleBox(string name, string themeType)` | Gets exact-type data with the category fallback below. |
| `public bool HasStyleBox(string name, string themeType)` | Tests category presence, independent of database fallback. |
| `public void RenameStyleBox(string oldName, string name, string themeType)` | Requires existing source and unused destination. |
| `public void ClearStyleBox(string name, string themeType)` | Removes an existing stored slot. |
| `public string[] GetStyleBoxList(string themeType)` | Caller-owned item-name snapshot. |
| `public string[] GetStyleBoxTypeList()` | Caller-owned category type-name snapshot. |
| `public bool HasThemeItem(DataType dataType, string name, string themeType)` | Presence dispatch for all six typed categories. |
| `public void RenameThemeItem(DataType dataType, string oldName, string name, string themeType)` | Typed category rename dispatch. |
| `public void ClearThemeItem(DataType dataType, string name, string themeType)` | Typed category removal dispatch. |
| `public string[] GetThemeItemList(DataType dataType, string themeType)` | Stored keys for one category/type. |
| `public string[] GetThemeItemTypeList(DataType dataType)` | Stored types for one category. |
| `public void AddType(string themeType)` | Ensures records in all six categories. |
| `public void RemoveType(string themeType)` | Removes category records and related variation links. |
| `public void RenameType(string oldThemeType, string themeType)` | Per-category move with collision preservation. |
| `public string[] GetTypeList()` | Unique category-record and variation-key types. |
| `public void SetTypeVariation(string themeType, string baseType)` | Sets a non-native variation's direct base. |
| `public bool IsTypeVariation(string themeType, string baseType)` | Tests only the direct relation. |
| `public string GetTypeVariationBase(string themeType)` | Direct base, or empty if absent. |
| `public string[] GetTypeVariationList(string baseType)` | Depth-first descendants, with repeat suppression. |
| `public void ClearTypeVariation(string themeType)` | Removes an existing direct variation link. |
| `public void MergeWith(Theme? other)` | Overlays nonempty item maps, variations, a nonnull default font and positive scalar defaults. |
| `public void Clear()` | Removes items and variations, retaining every configured default. |
| `protected override Resource CreateDuplicateInstance()` | Creates exact Theme duplicates. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies complete typed state through Resource graph policy. |
| `protected override void OnResetState()` | Clears typed items and variation state. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Describes font/scalar defaults and typed stored entries. |
| `protected override void Dispose(bool disposing)` | Releases subscriptions; never disposes borrowed items. |

## Property and query descriptions

<a id="defaultbasescale"></a><a id="hasdefaultbasescale"></a><a id="defaultfont"></a><a id="hasdefaultfont"></a><a id="defaultfontsize"></a><a id="hasdefaultfontsize"></a>
**Defaults:** positive scalar values and a nonnull DefaultFont participate in lookup. Equal scalar values/font identities are silent; other assignments emit Changed without a structural property-list notification. Nonfinite base scale throws ArgumentOutOfRangeException. DefaultBaseScale is a theme value, not an instruction to rescale every stored constant or style. DefaultFont is borrowed, rejects disposed assignments and forwards resource changes/disposal. Clearing theme items preserves it and its subscription.

<a id="getcolor"></a><a id="hascolor"></a><a id="getconstant"></a><a id="hasconstant"></a><a id="getfont"></a><a id="hasfont"></a><a id="getfontsize"></a><a id="hasfontsize"></a><a id="geticon"></a><a id="hasicon"></a><a id="getstylebox"></a><a id="hasstylebox"></a>
**Typed lookup and presence:** names/types are ordinal and case-sensitive. Get reads the exact type, without following variations. Queries require non-null strings but need not validate a new-key identifier. The six getters remain virtual; the built-in behavior is:

| Category | Missing/invalid stored value | Has behavior |
| --- | --- | --- |
| Color | Opaque black. | True for a stored key. |
| Constant | Zero. | True for a stored key. |
| Font | Local DefaultFont, otherwise ThemeDB.FallbackFont. | True for a nonnull entry or nonnull local default. |
| FontSize | Positive local DefaultFontSize, otherwise ThemeDB.FallbackFontSize. | True for a positive entry or positive local default. |
| Icon | ThemeDB.FallbackIcon for missing/null. | True for a non-null stored reference. |
| StyleBox | ThemeDB.FallbackStyleBox for missing/null. | True for a non-null stored reference. |

Null resource entries and nonpositive font sizes remain real stored slots and appear in lists, despite selecting fallbacks. An explicitly disposed borrowed resource retains identity: Has remains true and Get returns it; drawing rejects disposed resources. Database fallbacks do not make HasFont/HasIcon/HasStyleBox true for missing keys; a local DefaultFont is a defined font match.

## Mutation and list descriptions

<a id="setcolor"></a><a id="setconstant"></a><a id="setfont"></a><a id="setfontsize"></a><a id="seticon"></a><a id="setstylebox"></a>
**Set families:** new item names require one or more ASCII letters/digits/underscores; type keys allow the same characters and may be empty. Invalid new keys throw ArgumentException; null strings throw ArgumentNullException. SetColor requires finite components; resource setters reject disposed assigned references. Integer constants and font sizes retain their signed values.

Every valid item write emits Changed, including equal values/references. A new scalar key first emits PropertyListChanged. For resources, structural publication follows whether the previous slot held a non-null reference: overwriting a null slot, including null with null, emits both events. Events execute outside the resource lock. Required Changed delivery is attempted even if PropertyListChanged fails; failures propagate after committed state.

<a id="renamecolor"></a><a id="renameconstant"></a><a id="renamefont"></a><a id="clearfont"></a><a id="renamefontsize"></a><a id="renameicon"></a><a id="renamestylebox"></a><a id="clearcolor"></a><a id="clearconstant"></a><a id="clearfontsize"></a><a id="clearicon"></a><a id="clearstylebox"></a>
**Rename and Clear families:** rename requires an existing source slot and an unused destination in the same category/type; renaming to the same existing name is therefore invalid. Clear requires an existing slot, including null placeholders. Missing sources, occupied destinations and invalid new identifiers throw ArgumentException. Both publish PropertyListChanged before Changed. Resource removal detaches its shared subscription when the last alias is removed.

<a id="getcolorlist"></a><a id="getconstantlist"></a><a id="getfontlist"></a><a id="getfonttypelist"></a><a id="getfontsizelist"></a><a id="geticonlist"></a><a id="getstyleboxlist"></a><a id="getcolortypelist"></a><a id="getconstanttypelist"></a><a id="getfontsizetypelist"></a><a id="geticontypelist"></a><a id="getstyleboxtypelist"></a>
**Lists:** return independent string arrays, never a mutable view of storage. Item lists include raw placeholders; category type lists include empty category records. A missing type returns an empty item list. General map order is not a sorted-order guarantee.

<a id="hasthemeitem"></a><a id="renamethemeitem"></a><a id="clearthemeitem"></a><a id="getthemeitemlist"></a><a id="getthemeitemtypelist"></a>
**Category dispatch:** these methods share the corresponding typed semantics. All six categories execute; Max and undefined values throw ArgumentOutOfRangeException. Variant-valued GetThemeItem/SetThemeItem are represented by the six typed getter/setter families rather than a dynamic facade.

## Type and variation descriptions

<a id="addtype"></a><a id="removetype"></a><a id="renametype"></a><a id="gettypelist"></a>
**Type records:** AddType ensures a record in every supported category and publishes both notifications even if already present. GetTypeList unions category records with variation keys. RemoveType removes the named category records and direct/indirect variation links, while descendants keep their item maps. Resource-category removals, variation removals and the final notification are separate committed phases; later required phases are attempted after callback errors. RenameType handles categories independently: a destination collision leaves both records in that category unchanged. Related descendants are retargeted directly to the new type; an empty destination removes their variation links.

<a id="settypevariation"></a><a id="istypevariation"></a><a id="gettypevariationbase"></a><a id="gettypevariationlist"></a><a id="cleartypevariation"></a>
**Variation graph:** a variation key must be nonempty and must not name a native engine type; its base key must be nonempty. IsTypeVariation and GetTypeVariationBase read only the direct link. Equal SetTypeVariation writes move the variation to the end of that base's child order and notify. ClearTypeVariation requires an existing link but preserves item maps and descendant entries. GetTypeVariationList follows child insertion order depth-first and suppresses repeated nodes, so stored cycles terminate. Owner lookup separately rejects a cycle in its dependency chain with InvalidOperationException rather than looping indefinitely.

## Merge, copying and lifetime

<a id="mergewith"></a><a id="clear"></a>
**MergeWith:** null is a no-op. Otherwise it snapshots and validates the source, overlays stored items including null resources and nonpositive font sizes, updates variations, and copies a nonnull source DefaultFont and positive scalar defaults. A null source DefaultFont leaves the destination default unchanged. Empty category records are not imported. One structural/Changed pair follows the committed overlay. Clear releases subscriptions and removes all item/variation maps, retaining DefaultBaseScale, DefaultFont and DefaultFontSize, including the retained default font subscription; even an empty clear publishes both notifications.

<a id="createduplicateinstance"></a><a id="copycustomstateto"></a><a id="onresetstate"></a><a id="getpropertydescriptors"></a><a id="dispose"></a>
**Resource hooks:** duplicates preserve exact Theme type, empty category records, raw placeholders/defaults and variation child order. Shallow copies borrow the same resources; deep/scene-local copies follow the existing graph policy and preserve aliases. Dynamic stored entries are exposed through typed PropertyDescriptor instances, including raw nullable values and signed font sizes; no Variant values are introduced. Default reverts are zero, null and -1. Deep copies preserve aliases shared between DefaultFont and named font slots. Dispose detaches subscriptions and releases references without disposing caller-owned resources.

Each distinct borrowed resource has one change/disposal subscription even when several entries and DefaultFont alias it. Resource content changes forward Changed without a structural event. Built-in mutations/snapshots use an instance lock; user callbacks run outside it. Arbitrary cross-resource user code and custom getter overrides remain responsible for their own synchronization.

## Verification and limits

[ThemeResourceTests](../../tests/Electron2D.Tests/ThemeResourceTests.cs) verifies typed values, placeholders, alias subscriptions, variations, merge/copy, guards and concurrency; [ThemeLookupTests](../../tests/Electron2D.Tests/ThemeLookupTests.cs) verifies owner priority, deferred/detached caches, batching, reentry, fallback policy and typed override packing. [PanelContainerTests](../../tests/Electron2D.Tests/PanelContainerTests.cs) verifies defaults, background draw order, content bounds, eligibility, failure continuation and sorting after failed theme callbacks. Resource updates and active lookup pass 64 warmed cycles with zero managed bytes. [ThemePanelRenderingTests](../../tests/Electron2D.Tests/ThemePanelRenderingTests.cs) verifies seven visual phases and 64 warmed notification/layout/recording/render frames with zero managed bytes from ProcessFrameStarted through FramePostDraw on Linux Wayland GPU and compatibility. Native allocator counts, large-GUI performance, nonunit default-icon scaling, other platforms and owner acceptance remain unverified. [ThemeFontTests](../../tests/Electron2D.Tests/ThemeFontTests.cs) verifies the complete font category, default/slot aliases, raw null descriptors, copy/merge/clear subscriptions, disposal, owner lookup and packed font overrides; 64 warmed resource-change/deferred-lookup cycles allocate zero managed bytes. Theme file loading, editor authoring and full built-in GUI defaults are separate. See [coverage](../coverage/classes/Theme.md) and [ADR 0083](../decisions/rendering.md#adr-0083).
