# AccessibilityServer

Last updated: 2026-10-27

- Declaration: `public sealed partial class AccessibilityServer : ElectronObject`
- Source: [AccessibilityServer.cs](../../src/Servers/Accessibility/AccessibilityServer.cs), [scroll API](../../src/Servers/Accessibility/AccessibilityServer.Scroll.cs), [scroll sources](../../src/Servers/Accessibility/AccessibilityServer.ScrollSources.cs), [collection API](../../src/Servers/Accessibility/AccessibilityServer.Collections.cs), [collection sources](../../src/Servers/Accessibility/AccessibilityServer.CollectionSources.cs), [URL publication](../../src/Servers/Accessibility/AccessibilityServer.Links.cs), [producer metadata](../../src/Servers/Accessibility/AccessibilityServer.Metadata.cs)
- Inherits: [ElectronObject](ElectronObject.md); no derived types or public constructor.
- Component: [Native accessibility](../components/accessibility.md)

## Description

Static public service backed by one permanent retained object under [ADR 0095](../decisions/singleton-services.md#adr-0095). [ADR 0114](../decisions/accessibility.md#adr-0114) owns typed elements and native output. Linux uses optional system GIO/GLib and the real AT-SPI registry. Other platforms currently return unsupported; no desktop accessibility preference is changed.

Elements have session-unique, nonreused RIDs. Roots belong to native windows, scene elements to their Node, ordinary elements to their producer and sub-elements to their current semantic parent. Native output, native support and human screen-reader acceptance are separate facts. Methods other than the documented unavailable queries require a supported display and its owner. Invalid/stale/foreign RIDs throw KeyNotFoundException; absent native support throws NotSupportedException. Off-owner calls throw InvalidOperationException. Native failures propagate; no managed exception crosses an unmanaged callback boundary.

## Example

Set the application policy before opening its display; Engine.Run owns the scene lifetime:

```csharp
ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
var window = new Window { Size = new(320, 160) };
var button = new Button("Play") { AccessibilityDescription = "Start the game" };
window.AddChild(button);
button.Pressed += StartGame; // Application-owned compiled callback.
Engine.Run(window);
```

A custom producer can create an element, set its name/bounds, register an Action and attach it to GetWindowRoot(windowID). It must free producer-owned elements or retire their native window. Automatic scene producers receive NotificationAccessibilityUpdate to refine their borrowed element and can select an owned sub-element for focus.

## Methods

| Signature | Contract |
| --- | --- |
| `public static bool IsSupported()` | [IsSupported](#issupported) |
| `public static RID CreateElement(int windowID, AccessibilityRole role)` | [CreateElement](#createelement) |
| `public static RID CreateSubElement(RID parentRID, AccessibilityRole role, int insertPosition = -1)` | [CreateSubElement](#createsubelement) |
| `public static RID GetWindowRoot(int windowID)` | [GetWindowRoot](#getwindowroot) |
| `public static bool HasElement(RID id)` | [HasElement](#haselement) |
| `public static void FreeElement(RID id)` | [FreeElement](#freeelement) |
| `public static void UpdateAddChild(RID id, RID childID)` | [UpdateAddChild](#updateaddchild) |
| `public static void UpdateSetName(RID id, string name)` | [UpdateSetName](#updatesetname) |
| `public static void UpdateSetDescription(RID id, string description)` | [UpdateSetDescription](#updatesetdescription) |
| `public static void UpdateSetRole(RID id, AccessibilityRole role)` | [UpdateSetRole](#updatesetrole) |
| `public static void UpdateSetFlag(RID id, AccessibilityFlags flag, bool value)` | [UpdateSetFlag](#updatesetflag) |
| `public static void UpdateSetLive(RID id, AccessibilityLiveMode mode)` | [Live and popup output](#native-live-and-popups) |
| `public static void UpdateSetPopupType(RID id, AccessibilityPopupType popupType)` | [Live and popup output](#native-live-and-popups) |
| `public static void UpdateSetTooltip(RID id, string tooltip)` | [Live and popup output](#native-live-and-popups) |
| `public static void UpdateSetChecked(RID id, bool isChecked)` | [UpdateSetChecked](#updatesetchecked) |
| `public static void UpdateSetBounds(RID id, Rect2 rect)` | [UpdateSetBounds](#updatesetbounds) |
| `public static void UpdateSetTransform(RID id, Transform transform)` | [UpdateSetTransform](#updatesettransform) |
| `public static void UpdateAddAction(RID id, AccessibilityAction action, Action<int> callback)` | [Custom actions and accessible drag](#custom-actions-and-accessible-drag) |
| `public static void UpdateAddCustomAction(RID id, int actionID, string actionDescription)` | [Custom actions and accessible drag](#custom-actions-and-accessible-drag) |
| `public static void UpdateSetExtraInfo(RID id, string name)` | [Custom actions and accessible drag](#custom-actions-and-accessible-drag) |
| `public static void UpdateSetShortcut(RID id, string shortcut)` | [Contextual navigation](#contextual-navigation) |
| `public static void UpdateSetClassName(RID id, string className)` | [Contextual navigation](#contextual-navigation) |
| `public static void UpdateSetRoleDescription(RID id, string description)` | [Contextual navigation](#contextual-navigation) |
| `public static void UpdateAddAction(RID id, AccessibilityAction action, Action callback)` | [UpdateAddAction](#updateaddaction) |
| `public static void UpdateAddAction(RID id, AccessibilityAction action, Action<RID, int, RID, int> callback)` | [UpdateAddAction](#updateaddaction) |
| `public static void UpdateAddAction(RID id, AccessibilityAction action, Action<string> callback)` | [UpdateAddAction](#updateaddaction) |
| `public static void UpdateAddAction(RID id, AccessibilityAction action, Action<double> callback)` | [UpdateAddAction](#updateaddaction) |
| `public static void UpdateSetNumValue(RID id, double position)` | [Numeric values](#numeric-values) |
| `public static void UpdateSetNumRange(RID id, double min, double max)` | [Numeric values](#numeric-values) |
| `public static void UpdateSetNumStep(RID id, double step)` | [Numeric values](#numeric-values) |
| `public static void UpdateSetNumJump(RID id, double jump)` | [Numeric values](#numeric-values) |
| `public static void UpdateAddRelatedControls(RID id, RID relatedID)` | [UpdateAddRelatedControls](#updateaddrelatedcontrols) |
| `public static void UpdateAddRelatedDescribedBy(RID id, RID relatedID)` | [UpdateAddRelatedDescribedBy](#updateaddrelateddescribedby) |
| `public static void UpdateAddRelatedDetails(RID id, RID relatedID)` | [UpdateAddRelatedDetails](#updateaddrelateddetails) |
| `public static void UpdateAddRelatedFlowTo(RID id, RID relatedID)` | [UpdateAddRelatedFlowTo](#updateaddrelatedflowto) |
| `public static void UpdateAddRelatedLabeledBy(RID id, RID relatedID)` | [UpdateAddRelatedLabeledBy](#updateaddrelatedlabeledby) |
| `public static void UpdateAddRelatedRadioGroup(RID id, RID relatedID)` | [UpdateAddRelatedRadioGroup](#updateaddrelatedradiogroup) |
| `public static void UpdateSetMemberOf(RID id, RID groupID)` | [UpdateSetMemberOf](#updatesetmemberof) |
| `public static void UpdateSetErrorMessage(RID id, RID otherID)` | [UpdateSetErrorMessage](#updateseterrormessage) |
| `public static void ElementSetMeta<T>(RID id, T? meta)` | [Producer models](#producer-models) |
| `public static T? ElementGetMeta<T>(RID id)` | [Producer models](#producer-models) |
| `public static void UpdateSetURL(RID id, string url)` | [Native hyperlinks](#native-hyperlinks) |
| `public static void UpdateSetInPageLinkTarget(RID id, RID otherID)` | [UpdateSetInPageLinkTarget](#updatesetinpagelinktarget) |
| `public static void UpdateSetActiveDescendant(RID id, RID otherID)` | [UpdateSetActiveDescendant](#updatesetactivedescendant) |
| `public static void UpdateSetFocus(RID id)` | [UpdateSetFocus](#updatesetfocus) |
| `public static void SetWindowFocused(int windowID, bool focused)` | [SetWindowFocused](#setwindowfocused) |
| `public static void SetWindowRect(int windowID, Rect2 rectOut, Rect2 rectIn)` | [SetWindowRect](#setwindowrect) |
| `public static RID CreateSubTextEditElements(RID parentRID, RID shapedText, float minHeight, int insertPosition = -1, bool isLastLine = false)` | [CreateSubTextEditElements](#createsubtexteditelements) |
| `public static void UpdateSetValue(RID id, string value)` | [UpdateSetValue](#updatesetvalue) |
| `public static void UpdateSetTextSelection(RID id, RID textStartID, int startChar, RID textEndID, int endChar)` | [UpdateSetTextSelection](#updatesettextselection) |
| `public static void UpdateSetTextAlign(RID id, HorizontalAlignment align)` | [UpdateSetTextAlign](#updatesettextalign) |
| `public static void UpdateSetTextOrientation(RID id, bool vertical)` | [UpdateSetTextOrientation](#updatesettextorientation) |
| `public static void UpdateSetTextDecorations(RID id, bool underline, bool strikethrough, bool overline, Color? color = null)` | [UpdateSetTextDecorations](#updatesettextdecorations) |
| `public static void UpdateSetForegroundColor(RID id, Color color)` | [UpdateSetForegroundColor](#updatesetforegroundcolor) |
| `public static void UpdateSetBackgroundColor(RID id, Color color)` | [UpdateSetBackgroundColor](#updatesetbackgroundcolor) |
| `public static void UpdateSetLanguage(RID id, string language)` | [UpdateSetLanguage](#updatesetlanguage) |
| `public static void UpdateSetPlaceholder(RID id, string placeholder)` | [UpdateSetPlaceholder](#updatesetplaceholder) |
| `public static void UpdateSetNextOnLine(RID id, RID otherID)` | [UpdateSetNextOnLine](#updatesetnextonline) |
| `public static void UpdateSetPreviousOnLine(RID id, RID otherID)` | [UpdateSetPreviousOnLine](#updatesetpreviousonline) |
| `public static void UpdateSetScrollX(RID id, float position)` | [UpdateSetScrollX](#updatesetscrollx) |
| `public static void UpdateSetScrollXRange(RID id, float min, float max)` | [UpdateSetScrollXRange](#updatesetscrollxrange) |
| `public static void UpdateSetScrollY(RID id, float position)` | [UpdateSetScrollY](#updatesetscrolly) |
| `public static void UpdateSetScrollYRange(RID id, float min, float max)` | [UpdateSetScrollYRange](#updatesetscrollyrange) |
| `public static void UpdateAddAction(RID id, AccessibilityAction action, Action<AccessibilityScrollUnit> callback)` | [Scroll callbacks](#scroll-callbacks) |
| `public static void UpdateAddAction(RID id, AccessibilityAction action, Action<AccessibilityScrollHint> callback)` | [Scroll callbacks](#scroll-callbacks) |
| `public static void UpdateAddAction(RID id, AccessibilityAction action, Action<Vector2> callback)` | [Scroll callbacks](#scroll-callbacks) |

| `public static void UpdateSetListItemCount(RID id, int size)` | [Native collections](#native-collections) |
| `public static void UpdateSetListItemIndex(RID id, int index)` | [Native collections](#native-collections) |
| `public static void UpdateSetListItemLevel(RID id, int level)` | [Native collections](#native-collections) |
| `public static void UpdateSetTableRowCount(RID id, int count)` | [Native collections](#native-collections) |
| `public static void UpdateSetTableColumnCount(RID id, int count)` | [Native collections](#native-collections) |
| `public static void UpdateSetTableRowIndex(RID id, int index)` | [Native collections](#native-collections) |
| `public static void UpdateSetTableColumnIndex(RID id, int index)` | [Native collections](#native-collections) |
| `public static void UpdateSetListItemSelected(RID id, bool selected)` | [Native collections](#native-collections) |
| `public static void UpdateSetListItemExpanded(RID id, bool expanded)` | [Native collections](#native-collections) |
| `public static void UpdateSetListOrientation(RID id, bool vertical)` | [Native collections](#native-collections) |
| `public static void UpdateSetTableCellPosition(RID id, int rowIndex, int columnIndex)` | [Native collections](#native-collections) |
| `public static void UpdateSetTableCellSpan(RID id, int rowSpan, int columnSpan)` | [Native collections](#native-collections) |

## Enumerations

| Type | Contract |
| --- | --- |
| [AccessibilityRole](AccessibilityServer.AccessibilityRole.md) | All 48 semantic role selectors and native role projections. |
| [AccessibilityFlags](AccessibilityServer.AccessibilityFlags.md) | Ten independent state/geometry selectors. |
| [AccessibilityAction](AccessibilityServer.AccessibilityAction.md) | Activation/focus/expansion, numeric/text and typed scroll requests; other payload families remain incomplete. |
| [AccessibilityScrollUnit](AccessibilityServer.AccessibilityScrollUnit.md) | Item and page scroll amounts. |
| [AccessibilityScrollHint](AccessibilityServer.AccessibilityScrollHint.md) | Six preferred reveal edges. |

## Method descriptions

<a id="issupported"></a>
### IsSupported

`public static bool IsSupported()`

Returns true only for a connected native service. Without a display, with Disabled policy, with missing optional libraries/bus, or on an unimplemented platform it returns false. A supported display owner performs cold registration once per display lifetime; availability can later become false after a disconnect. This query alone does not enable accessibility-only GUI focus.

<a id="createelement"></a>
### CreateElement

`public static RID CreateElement(int windowID, AccessibilityRole role)`

Validates a live native window and typed role, then creates an unattached, producer-owned element. Call UpdateAddChild to publish it in a parent tree. The RID belongs exclusively to accessibility and cannot be freed through another service.

<a id="createsubelement"></a>
### CreateSubElement

`public static RID CreateSubElement(RID parentRID, AccessibilityRole role, int insertPosition = -1)`

Creates and attaches a parent-owned child. insertPosition is a zero-based slot from zero through the current child count; -1 appends. Retiring its current semantic parent recursively frees owned descendants. Reparenting transfers that semantic ownership. Invalid insertion fails before allocating an element.

<a id="getwindowroot"></a>
### GetWindowRoot

`public static RID GetWindowRoot(int windowID)`

Lazily returns the single borrowed root of a live native window. It remains owned by that window; callers cannot free or reparent it. Independent native windows have distinct roots, while embedded windows publish ordinary elements inside their containing native root.

<a id="haselement"></a>
### HasElement

`public static bool HasElement(RID id)`

Returns current membership for a RID, including borrowed scene/window elements. Empty, foreign and retired identities return false. This query does not initialize native output. With a display it requires its owner; without a display it returns false.

<a id="freeelement"></a>
### FreeElement

`public static void FreeElement(RID id)`

Retires a producer-owned identity. Parent-owned sub-elements retire recursively; independent children detach and remain live. Registered callbacks and source references are released. Borrowed window and scene-node identities reject caller disposal. Native notification failure is reported after all identities and registrations have been attempted.

<a id="updateaddchild"></a>
### UpdateAddChild

`public static void UpdateAddChild(RID id, RID childID)`

Attaches or reparents childID at the end of id's child list. Both elements must belong to the same native window, the child cannot be a window root, and the resulting tree must be acyclic. Repeated attachment to the last slot is silent. Native topology events preserve the old removal and new insertion indices.

<a id="updatesetname"></a>
### UpdateSetName

`public static void UpdateSetName(RID id, string name)`

Publishes the accessible name, including an empty name. Equal values emit nothing. Native strings use UTF-8; embedded NUL characters are represented by the replacement character at the native boundary.

<a id="updatesetdescription"></a>
### UpdateSetDescription

`public static void UpdateSetDescription(RID id, string description)`

Publishes a supplementary accessible description. Empty is valid; equal values emit nothing. The same native UTF-8 boundary as UpdateSetName applies.

<a id="updatesetrole"></a>
### UpdateSetRole

`public static void UpdateSetRole(RID id, AccessibilityRole role)`

Changes the typed semantic role and emits its native role property change. Invalid numeric values reject; equal values are silent. A role is a semantic hint: it does not synthesize editable text, numeric value or table protocols.

<a id="updatesetflag"></a>
### UpdateSetFlag

`public static void UpdateSetFlag(RID id, AccessibilityFlags flag, bool value)`

Sets one independently selected flag. AccessibilityFlags values are selectors, not a bitmask. Hidden and disabled state affect native output and action eligibility; clipping and touch passthrough affect semantic hit testing. Equal updates are silent.

<a id="updatesetchecked"></a>
### UpdateSetChecked

`public static void UpdateSetChecked(RID id, bool isChecked)`

Sets the checked state and emits its native state event on change. Automatic scene output derives it from a toggle button's committed ButtonPressed state.

<a id="updatesetbounds"></a>
### UpdateSetBounds

`public static void UpdateSetBounds(RID id, Rect2 rect)`

Sets nonnegative finite bounds in native-window client coordinates before the element transform. Rectangle endpoints and the composed transformed rectangle must remain finite. Equal updates are silent. Bounds do not change ordinary GUI layout.

<a id="updatesettransform"></a>
### UpdateSetTransform

`public static void UpdateSetTransform(RID id, Transform transform)`

Sets the affine transform applied to the element's bounds. The matrix and resulting rectangle/endpoints must be finite. Bounds remain window-relative; native parent-relative queries subtract the parent's transformed origin.

<a id="updateaddaction"></a>
### UpdateAddAction

`public static void UpdateAddAction(RID id, AccessibilityAction action, Action callback)`

Registers or replaces a borrowed compiled Action for Click, Focus, Blur, Collapse, Expand, Decrement, Increment, ScrollBackward, ScrollForward, ShowTooltip, HideTooltip or ShowContextMenu. The Action<double> overload accepts numeric SetValue only. The four-argument RID/scalar overload accepts SetTextSelection; the string overload accepts ReplaceSelectedText. Their producer and source rules are specified in [Native text](#native-text). Unit/hint/point/offset overloads are specified in [Native scrolling](#native-scrolling). ShowTooltip, HideTooltip and ShowContextMenu use the same no-argument overload and invoke producer-owned presentation. The integer callback accepts Custom; the remaining color value payload needs its native consumer. Native requests run during the display owner's event pump. Hidden, disabled, detached, departing, queued and disposed scene controls reject activation; callback exceptions return a native failure and aggregate after the pump.

<a id="updatesetfocus"></a>
### UpdateSetFocus

`public static void UpdateSetFocus(RID id)`

Publishes one semantic focus target per native window. Replacing the target clears the former one. Native focused state is gated by that window's active state; this publication does not itself move ordinary Control focus. Automatic controls instead use the existing GUI focus path and OnGetFocusedAccessibilityElement.

<a id="setwindowfocused"></a>
### SetWindowFocused

`public static void SetWindowFocused(int windowID, bool focused)`

Publishes the window's active state and updates its retained semantic focus target. Native automatic scene output follows the display's focus query. A custom producer must publish its actual native focus changes.

<a id="setwindowrect"></a>
### SetWindowRect

`public static void SetWindowRect(int windowID, Rect2 rectOut, Rect2 rectIn)`

Supplies separate outer/decorated and inner/client rectangles in screen coordinates. Both rectangles require finite endpoints and nonnegative dimensions. Screen queries for the window use rectOut; descendants add rectIn.Position to their window-relative bounds. Native automatic windows supply real bounds on backends exposing global positions. Wayland leaves screen origin unknown unless an external producer can supply it; window-relative queries continue to work.

## Lifecycle, verification and limits

Closing a native child window retires all its elements, including unattached ones. Display teardown removes native registrations, connection/context ownership, callbacks and RID membership even after callback or notification failure. Reopening creates a new bridge and new element identities. Protected `ValidateDisposal()` rejects disposal of the permanent service.

[AccessibilityFoundationTests](../../tests/Electron2D.Tests/AccessibilityFoundationTests.cs), its managed/lifetime companions and the separate [AT-SPI client](../../tests/AccessibilityClient/probe.py) exercise public scene/native behavior. The current profiles cover Accessible/Application/Component/Action/Value/Text/EditableText/Selection/Table/TableCell/Cache and native announcements. Color values and remaining braille/state metadata and color payload actions retain exact obligations in [coverage](../coverage/classes/AccessibilityServer.md). Warmed owner managed allocations, native heap allocation totals, hardware rendering, foreign platforms and human assistive-client acceptance are distinct gates.

<a id="numeric-values"></a>
### Numeric values

UpdateSetNumValue publishes the actual double and enables the native Value interface. It preserves IEEE values, including NaN already retained by a Range. UpdateSetNumRange requires finite min <= max; UpdateSetNumStep and UpdateSetNumJump require finite nonnegative amounts. Configuration can precede the first value; it never clamps or changes the producer itself. Repeated equal values, including NaN, suppress redundant output.

The Action<double> overload of UpdateAddAction accepts SetValue only. A native CurrentValue write passes a finite requested value to that callback on the display owner. The callback owns mutation and publication; successful invocation does not invent a result. No-argument registration accepts Click/Focus/Blur/Decrement/Increment and rejects SetValue. Positive jump plus numeric callback exposes page-decrement/page-increment native actions using the current published value. Zero jump removes them. Value Text formats the number on query; SpinBox uses its displayed affixes/numerals.

Automatic Range descendants publish values, authored bounds, positive Step or one, and Page as jump. Slider, ScrollBar, SpinBox and progress displays have their native roles. ProgressBar/TextureProgressBar and non-editable Slider/SpinBox reject writes and step/page actions. Hidden/disabled/detached/queued/retired controls also reject requests. Ordinary Value policy owns snapping, rounding, page caps, shared peers, protected hooks and ValueChanged. Native writes reject nonfinite requests before invoking a callback; callback failures propagate through the pump's aggregate policy. Source updates publish after ordinary scene processing; custom NotificationAccessibilityUpdate overrides survive until the corresponding source field changes.

## Semantic relationships

Non-owning relationships publish actual AT-SPI relations. Elements must be live and belong to the same native window. They do not transfer lifetime or change the semantic hierarchy. Invalid/stale/foreign identities throw KeyNotFoundException, cross-window targets throw ArgumentException, absent support throws NotSupportedException and off-owner access throws InvalidOperationException. Target retirement removes references and native reciprocal output. Repeated adds are idempotent. Native notification failures aggregate after graph changes commit; required retirement cleanup still runs. Non-hierarchical cycles are allowed. Automatic source links and manual producer links have separate provenance; changing source paths preserves a matching manual link. Forward output uses the current authored source order before additional manual targets; pure source reorder changes native output and emits a relation event.

<a id="updateaddrelatedcontrols"></a>
### UpdateAddRelatedControls

`public static void UpdateAddRelatedControls(RID id, RID relatedID)`

Adds a target controlled by this producer. Native output is ControllerFor and its reciprocal ControlledBy. Insertion order is retained and duplicate targets are ignored.

<a id="updateaddrelateddescribedby"></a>
### UpdateAddRelatedDescribedBy

`public static void UpdateAddRelatedDescribedBy(RID id, RID relatedID)`

Adds a describing target. Native output is DescribedBy and its reciprocal DescriptionFor. Insertion order is retained and duplicate targets are ignored.

<a id="updateaddrelateddetails"></a>
### UpdateAddRelatedDetails

`public static void UpdateAddRelatedDetails(RID id, RID relatedID)`

Adds supplementary detail content. Native output is Details and reciprocal DetailsFor. Insertion order is retained and duplicate targets are ignored.

<a id="updateaddrelatedflowto"></a>
### UpdateAddRelatedFlowTo

`public static void UpdateAddRelatedFlowTo(RID id, RID relatedID)`

Adds the next semantic reading-flow target. Native output is FlowsTo and reciprocal FlowsFrom. Insertion order is retained and duplicate targets are ignored.

<a id="updateaddrelatedlabeledby"></a>
### UpdateAddRelatedLabeledBy

`public static void UpdateAddRelatedLabeledBy(RID id, RID relatedID)`

Adds a target supplying a label. Native output is LabelledBy and reciprocal LabelFor; its name is not copied into this producer's Name. Insertion order is retained and duplicate targets are ignored.

<a id="updateaddrelatedradiogroup"></a>
### UpdateAddRelatedRadioGroup

`public static void UpdateAddRelatedRadioGroup(RID id, RID relatedID)`

Adds another radio peer through native MemberOf. Call for each directed peer pair; self-membership is not generated automatically. Insertion order is retained and duplicate targets are ignored.

<a id="updatesetmemberof"></a>
### UpdateSetMemberOf

`public static void UpdateSetMemberOf(RID id, RID groupID)`

Replaces the single explicit group target through native MemberOf. An empty groupID clears it. Radio-peer membership remains independent.

<a id="updateseterrormessage"></a>
### UpdateSetErrorMessage

`public static void UpdateSetErrorMessage(RID id, RID otherID)`

Replaces the target containing a validation error, publishing native ErrorMessage/ErrorFor and invalid-entry state. Empty otherID clears the error and state; freeing the target also clears them.

<a id="updatesetinpagelinktarget"></a>
### UpdateSetInPageLinkTarget

`public static void UpdateSetInPageLinkTarget(RID id, RID otherID)`

Replaces the non-owning internal target of the [native hyperlink](#native-hyperlinks). Empty otherID clears only this target; an independently published URL survives. GetObject(0) prefers the live internal target. Internal-only anchors return an empty URI and unknown Hypertext indices (-1). Target validation and retirement follow the semantic graph rules above.

<a id="updatesetactivedescendant"></a>
### UpdateSetActiveDescendant

`public static void UpdateSetActiveDescendant(RID id, RID otherID)`

Replaces the strict descendant selected when this composite owns semantic focus; a non-descendant or self throws ArgumentException. Empty otherID clears it. Native output includes ActiveDescendantChanged and manages-descendants; semantic focus follows active-descendant chains. Reparenting outside the owner or retiring a target clears it and returns focus to the surviving owner. This selects semantic focus and does not move the ordinary GUI focus owner.

<a id="native-text"></a>
## Native text and editing

LineEdit, TextEdit and CodeEdit publish actual text and copied semantic line identities. Native Text reads scalar offsets, character/range bounds, point hits, bounded ranges, ICU words/sentences, visual lines and paragraphs, run/inherited attributes, caret and selections. TextEdit retains indexed multiple-selection semantics. Requests run on the display owner, reuse ordinary edits/history/signals and enforce read-only and length policies. A nonselectable field remains editable. Password text is masked; native copying/cutting is rejected. Committed content excludes pending IME composition, while geometry follows its displayed insertion.

SetTextSelection uses `Action<RID, int, RID, int>` with anchor RID/offset then focus RID/offset. ReplaceSelectedText uses `Action<string>` and replaces the current selection. Generic callbacks must publish their resulting content and endpoints; native success means the callback ran, not that requested text bypassed producer policy. Wrong action signatures reject before registration. Sources use cached compiled callbacks. Native errors return wire errors and user exceptions aggregate after returning to the owner pump.

Source scrolling keeps selection intact; unavailable generic scrolling returns false. Unknown screen origin uses the existing unavailable screen-coordinate sentinel. Font size is projected to nominal protocol points (96 logical pixels per inch), independently of physical DPI qualification. Cold queries, snapshots and content edits may allocate; warmed unchanged publication and caret updates preserve the managed-owner frame allocation boundary.

<a id="createsubtexteditelements"></a>
### CreateSubTextEditElements

`public static RID CreateSubTextEditElements(RID parentRID, RID shapedText, float minHeight, int insertPosition = -1, bool isLastLine = false)`

Copies a borrowed shaped buffer into an attached parent-owned line. Empty RID creates an empty line; isLastLine=false appends LF. Finite minHeight is nonnegative; insertPosition follows CreateSubElement. The snapshot retains scalar rectangles, native font family/size/weight/style, language and direction independently of the original buffer and fonts. Use the returned line RID for scalar selection. Recreate it when its shape changes; bounds/transforms position its copied local geometry.

<a id="updatesetvalue"></a>
### UpdateSetValue

`public static void UpdateSetValue(RID id, string value)`

Publishes a nonnull actual string value through native valuetext attributes and text/value change events. Numeric Value.Text uses an explicitly supplied string before automatic number formatting. This setter never calls the edit callback or invents a glyph layout; shaped/source line children remain the text tree when present. Generic unshaped values are readable with unknown character geometry. Producers publish their actual result after requests.

<a id="updatesettextselection"></a>
### UpdateSetTextSelection

`public static void UpdateSetTextSelection(RID id, RID textStartID, int startChar, RID textEndID, int endChar)`

Publishes directed anchor/focus endpoints, including a collapsed caret. Both text lines must descend from id in the same window. Offsets are Unicode scalar positions from zero through the corresponding line length. Invalid identities, foreign/outside endpoints and out-of-range positions fail before mutation. Retiring or moving an endpoint outside the owner clears the selection.

<a id="updatesettextalign"></a>
### UpdateSetTextAlign

`public static void UpdateSetTextAlign(RID id, HorizontalAlignment align)`

Publishes a defined horizontal alignment through native justification attributes. The initial default is Left; descendants inherit authored attributes unless a closer run overrides them.

<a id="updatesettextorientation"></a>
### UpdateSetTextOrientation

`public static void UpdateSetTextOrientation(RID id, bool vertical)`

Publishes vertical text direction through native attributes. False preserves horizontal run LTR/RTL direction. This state setter does not reshape or rotate glyphs.

<a id="updatesettextdecorations"></a>
### UpdateSetTextDecorations

`public static void UpdateSetTextDecorations(RID id, bool underline, bool strikethrough, bool overline, Color? color = null)`

Publishes independent underline, strikethrough and overline flags and a finite decoration color. Null selects opaque black. Native readers receive styling attributes and an attribute-change event.

<a id="updatesetforegroundcolor"></a>
### UpdateSetForegroundColor

`public static void UpdateSetForegroundColor(RID id, Color color)`

Publishes a finite foreground color; native RGB attributes project clamped channels to 16-bit values. This semantic setter does not repaint the visual source.

<a id="updatesetbackgroundcolor"></a>
### UpdateSetBackgroundColor

`public static void UpdateSetBackgroundColor(RID id, Color color)`

Publishes a finite background color through the same native 16-bit RGB attribute projection. Authored values inherit through the semantic tree.

<a id="updatesetlanguage"></a>
### UpdateSetLanguage

`public static void UpdateSetLanguage(RID id, string language)`

Publishes a nonnull text language tag. Shaped runs retain their own language metadata; source fields otherwise use their authored Language. This does not change shaping or the application locale.

<a id="updatesetplaceholder"></a>
### UpdateSetPlaceholder

`public static void UpdateSetPlaceholder(RID id, string placeholder)`

Publishes nonnull placeholder-text independently of actual content. Automatic fields use their own PlaceholderText; a placeholder never becomes committed document text.

<a id="updatesetnextonline"></a>
### UpdateSetNextOnLine

`public static void UpdateSetNextOnLine(RID id, RID otherID)`

Sets a non-owning same-window next text neighbor; an empty RID clears it. Native visual-line boundary queries join adjacent linked fragments even when their local baselines differ. Literal LF remains a mandatory boundary. Links never reorder hierarchy or recurse; retiring either target removes its references and notifies affected text readers.

<a id="updatesetpreviousonline"></a>
### UpdateSetPreviousOnLine

`public static void UpdateSetPreviousOnLine(RID id, RID otherID)`

Sets a non-owning same-window previous text neighbor; an empty RID clears it. Native visual-line boundary queries join adjacent linked fragments even when their local baselines differ. Literal LF remains a mandatory boundary. Links never reorder hierarchy or recurse; retiring either target removes its references and notifies affected text readers.

## Native scrolling

Scroll state is independent of numeric Value and editable text. Producers publish actual finite local offsets with optional finite ordered bounds. Publication never moves content, clamps the authored offset or invokes callbacks. Native Accessible attributes expose `scroll-x`, `scroll-y` and the corresponding `-min`/`-max` keys only after publication; attribute/visible-data events announce changes. Unbounded GraphEdit panning publishes offsets without invented finite endpoints.

Native Action exposes item/page directions, no-argument sequential requests and `scroll-to-start`/`scroll-to-end` for known minimum/maximum offsets. These endpoint names refer to published coordinate bounds, including negative RTL ranges. A request calls the producer; the producer owns clamping, mutation and publication. The native Component protocol reveals descendants through all scrollable ancestors or moves their top-left toward a window-client point. Source and generic ancestors reuse the typed absolute-offset callback. Bounds constrain placement; oversized targets retain their size and receive the largest attainable exposure. A custom target reveal/point callback can consume the request directly.

Window/parent coordinates project to window-client coordinates before point dispatch. Unknown screen origin rejects screen-point requests rather than fabricating placement. Invalid coordinate/reveal selectors produce wire argument errors. Singular or unrepresentable automatic ancestor projections return false. Hidden, disabled, detached, queued or retired targets reject requests. Clipping alone does not disable interaction or text editing: native visible and showing states are distinct, and hit tests respect actual rendered clips. Geometry-free grouping elements do not hide their unclipped descendants. Internal scroll bars retain their own exposure outside a content viewport and do not pan that content when revealed. Independent canvas roots do not inherit their former content clip or scroll displacement.

ScrollContainer, ItemList, Tree, TextEdit/CodeEdit, RichTextLabel, LineEdit, TabBar/owned TabContainer strips and GraphEdit supply automatic scroll state/actions. Published source offsets use local rendered pixels: visual text rows, negative LineEdit translation, RTL tree/list coordinates and quantized tab prefixes are projected to this space. Item steps use row, tab, font or configured small-step metrics; pages use the current viewport. TextEdit retains its ordinary smooth-scroll target accumulation. Scrolling preserves caret, selection and GUI focus and remains available in a read-only editor. `LineEdit.GetScrollOffset()` reads its retained negative text translation without refitting the caret.

Default source callbacks are retained when the source binds; explicit overrides last until the source policy changes. Unchanged publication and warmed active scrolling reuse prepared state. Cold native queries, topology/layout changes and callback work may allocate. [The public scene](../../tests/Electron2D.Tests/AccessibilityScrollTests.cs) and [independent client](../../tests/AccessibilityClient/scroll.py) exercise this contract on both current software-native canvas backends; native heap, physical devices, foreign adapters, human reader and release-package acceptance are separate gates.

<a id="updatesetscrollx"></a>
### UpdateSetScrollX

`public static void UpdateSetScrollX(RID id, float position)`

Publishes a finite horizontal local offset. Equal prepared updates are silent. It does not clamp to the declared range. The producer must publish the coordinates it actually uses; this method does not mutate its GUI or invoke a registered action.

<a id="updatesetscrollxrange"></a>
### UpdateSetScrollXRange

`public static void UpdateSetScrollXRange(RID id, float min, float max)`

Publishes inclusive finite horizontal bounds with `min <= max`. It leaves the current offset untouched. With SetScrollOffset, known bounds participate in native endpoint actions and ancestor placement clamping.

<a id="updatesetscrolly"></a>
### UpdateSetScrollY

`public static void UpdateSetScrollY(RID id, float position)`

Publishes the actual finite vertical local offset with the same publication-only and equal-update behavior as UpdateSetScrollX.

<a id="updatesetscrollyrange"></a>
### UpdateSetScrollYRange

`public static void UpdateSetScrollYRange(RID id, float min, float max)`

Publishes inclusive finite vertical bounds with `min <= max`, independently of the horizontal axis and current value. Equal prepared ranges are silent.

<a id="scroll-callbacks"></a>
### UpdateAddAction: scroll callbacks

| Signature | Accepted selector and payload |
| --- | --- |
| `UpdateAddAction(RID id, AccessibilityAction action, Action<AccessibilityScrollUnit> callback)` | ScrollUp/Down/Left/Right: Item or Page. |
| `UpdateAddAction(RID id, AccessibilityAction action, Action<AccessibilityScrollHint> callback)` | ScrollIntoView: preferred target edge. Native anywhere placement chooses a supported edge for a custom callback. |
| `UpdateAddAction(RID id, AccessibilityAction action, Action<Vector2> callback)` | ScrollToPoint: finite window-client point; SetScrollOffset: finite absolute local offset. |

Callbacks are borrowed, replaced by another registration for that selector and released with the element. They execute on the display owner. Native failures reply with Failed and aggregate on that owner; callbacks may retire their producer and owned subtree. No exception crosses the unmanaged ABI. Null callbacks throw ArgumentNullException and mismatched selectors throw ArgumentOutOfRangeException. Like the four setters, these methods require a supported display, owner-thread access and a live same-service RID; unavailable support throws NotSupportedException, ownership violations InvalidOperationException and unknown/foreign/retired identities KeyNotFoundException. Nonfinite offsets and nonfinite/reversed ranges throw ArgumentOutOfRangeException before publication.

## Native collections

Collection setters publish actual producer state without changing selection, resizing a table or invoking callbacks. Equal updates are silent. Counts, positions, indices and levels are nonnegative integers; spans are positive integers. Public item indices and hierarchy levels start at zero; native posinset/level attributes start at one. Counts and cell coordinates are independent publications, so partially built tables retain their authored state. Invalid values throw ArgumentOutOfRangeException; support, thread and RID guards follow the service lifetime contract.

| Publication | Native consumer |
| --- | --- |
| UpdateSetListItemCount | Collection size and Selection discovery. Item indices inherit the parent's set size. |
| UpdateSetListItemIndex / Level | Position and hierarchy attributes. |
| UpdateSetListItemSelected | Selected/selectable states, Selection enumeration and change events. |
| UpdateSetListItemExpanded | Expandable/expanded states and change events. Collapse/Expand use no-argument UpdateAddAction callbacks. |
| UpdateSetListOrientation | Horizontal/vertical states. |
| UpdateSetTableRowCount / ColumnCount | Table dimensions and interface discovery. |
| UpdateSetTableRowIndex / ColumnIndex | Row/column attributes and RowHeader/ColumnHeader lookup. |
| UpdateSetTableCellPosition / Span | TableCell discovery, row/column/span queries, containing table and header cells. |

Native Selection uses actual direct-child indices, including any non-item siblings; selected-child indices address only the selected snapshot. Requests reuse registered Focus/Blur callbacks for custom items, whose producer owns mutation and publication. Automatic consumers reuse their ordinary public selection paths. Hidden, disabled, detached, retired and read-only owners reject mutation. Nonselectable source items reject new selection. SelectAll and table-axis requests preflight the source's single, row or multi selection policy. Snapshot dispatch tolerates callbacks that reparent or retire the collection; callback failures retain the original exception for the owner and reply Failed on the wire.

Table queries include dimensions, cells, actual accessible child indices, spans, headers, selected rows/columns and axis selection. Merged cells cover their published extent; overflow-safe arithmetic preserves int.MaxValue metadata. Selected-axis enumeration rejects a response exceeding 1,000,000 distinct candidate indices with a native argument error rather than allocating an unbounded message. This is a native response limit, not a public count/span limit. Unpublished caption/summary/header relations return null references. Nested tables own their own cells.

ItemList owns one transient item element per actual item. Focus selects that item through Select's ordinary single/current behavior and focuses the owner; native multi-selection uses additive Select. Activation raises ItemActivated. Programmatic ItemList selection remains silent. Tree owns header/cell elements, reports visible table rows, merged spans and ordinary TreeItem.Select/Deselect events, and exposes Collapse/Expand through Collapsed. Cells are direct tree children so table child indices remain usable; NODE_CHILD_OF/NODE_PARENT_OF relations retain hierarchy. Hidden roots are transparent. Folding and visibility retain identities while excluding hidden cells from table coordinates and interaction. TabBar owns tab elements, respects disabled/hidden tabs and DeselectEnabled and preserves CurrentTab during reveal. TabContainer's owned strip uses the same producer.

IDs follow actual item/tab/cell objects during reorder and same-window TreeItem transfer. Reconciliation retires removed records after all source owners have had a chance to claim transferred objects. Node/window retirement removes owned virtual records even after manual semantic reparenting. Scene duplication creates independent IDs; archives retain only source GUI state. Prepared idle and active selection publication reuses cached layouts, delegates and records; cold requests/topology/layout/native allocation remain separate.

[The public collection scene](../../tests/Electron2D.Tests/AccessibilityCollectionTests.cs) and [independent wire/libatspi client](../../tests/AccessibilityClient/collection.py) exercise native selection, table/cell discovery, spans/headers, expansion, scrolling, source events, identity transfer, retirement, invalid requests and failure/reopening. Native software-backend and source NativeAOT checks do not establish physical-device, human-reader, foreign-platform or release-package acceptance.

<a id="native-live-and-popups"></a>
## Native live and popup output

[AccessibilityLiveMode](AccessibilityServer.AccessibilityLiveMode.md) is Off=0, Polite=1, Assertive=2. UpdateSetLive publishes native live metadata and queues the current content when enabled. Text/name/numeric changes coalesce until source reconciliation or the owner pump finishes; the native Object.Announcement event carries priority 1/2 and string content. Off cancels pending output. Manual descendants without an explicit policy inherit the nearest region and announce their latest changed content under that region's identity. Explicit Off stops inheritance. Hidden, password-protected and retired elements never announce. Automatic captions use the displayed text even with an authored accessible name. Numeric output preserves the published value using invariant formatting. Native speech scheduling remains the assistive client's responsibility.

[AccessibilityPopupType](AccessibilityServer.AccessibilityPopupType.md) is Menu=0, List=1, Tree=2, Dialog=3. UpdateSetPopupType publishes has-popup and haspopup=menu/listbox/tree/dialog; it does not open anything. UpdateSetTooltip publishes tooltip and has-tooltip; empty text removes that capability. Null tooltip text and unknown enum values reject explicitly. These methods retain ordinary owner, availability and RID guards.

ShowTooltip, HideTooltip and ShowContextMenu are no-argument compiled actions with native names show-tooltip, hide-tooltip and show-context-menu. Control uses the real containing viewport tooltip host, including explicit requests on MouseFilter.Ignore controls. HideTooltip cancels presentation. Context-menu callbacks reuse ordinary LineEdit/TextEdit/RichTextLabel menu policy, editing, history and signals; read-only commands remain guarded. Disabling ContextMenuEnabled removes automatic popup capability and the action. Current manual overrides remain until the corresponding source policy changes.

PopupMenu publishes real item records with stable identities while the same source/native window survives. Items expose MenuItem, MenuItemCheckBox or MenuItemRadio roles, ordinal/count, current focus/selection, checked state, tooltip and submenu expansion. Disabled and separator entries cannot select or activate. Separators retain semantic records without a click action and do not count as selectable items. Selection changes ordinary focused item silently; Click invokes ordinary menu activation and signals without inventing checked/radio mutations. Bounds and scrolling use the actual internal rendered item canvas; semantic lifetime belongs to the PopupMenu element. OptionButton publishes a button with list popup capability and uses that same menu's ItemSelected path.

A native popup has its own application root; hiding destroys that surface and retires all associated item identities. Showing again creates fresh native identities. Embedded popups keep item identities across hide/show, reorder and label changes, and retire removed items. Runtime menus, callbacks and semantic identities stay out of duplication and scene archives. Control.AccessibilityLive and LineEdit.ContextMenuEnabled are stored authored properties. Cold topology, menu layout, native queries and text shaping may allocate; prepared live/name/numeric/selection changes retain the warmed managed frame boundary. Native heap totals and human reader acceptance are separate gates.

<a id="custom-actions-and-accessible-drag"></a>
## Custom actions and accessible drag

UpdateAddCustomAction publishes a signed 32-bit ID and description. Native Action entries follow existing standard actions and retain custom insertion order before generated numeric/scroll actions. Repeating an ID replaces its description without changing its ordinal. Equal descriptions do not republish; an empty description leaves the list unchanged. Null descriptions reject. GetActions/name/localized-name/description expose the supplied caption. UpdateAddAction with Custom and a compiled integer callback receives the exact ID, including signed extremes. A missing callback returns false; invalid ordinals return native argument errors. Metadata alone does not mutate application state.

Custom callbacks retain owner dispatch, hidden/disabled/stale guards and reentrant retirement. Callback exceptions return a native failure and aggregate on the owner; the old identity cannot invoke after retirement. The no-argument overload rejects Custom, and the integer overload rejects other selectors. Explicitly type scalar lambda parameters, such as `(int id) => Handle(id)` or `(double value) => SetValue(value)`, to distinguish custom IDs and numeric values.

UpdateSetExtraInfo stores context separately from the base name. Native Name, Cache and name-change events publish the base name plus one space plus context, with outer whitespace trimmed. Empty context removes the suffix. Embedded NUL becomes U+FFFD at the UTF-8 boundary; large labels use temporary native storage, while small composition uses stack bytes. Base name/caption/value remain the live-content fields; extra context does not replace an actual displayed caption. Context updates preserve the base name and existing description. Source defaults preserve explicit overrides until their corresponding field changes.

Control.AccessibilityDrag and AccessibilityDrop use the ordinary connected viewport drag lifecycle. Source preparation establishes borrowed payload/preview ownership before invoking the virtual or forwarded producer. Hooks receive `(float.PositiveInfinity, float.PositiveInfinity)` for a non-positional request. Public GetDragData/CanDropData/DropData convenience methods still reject nonfinite coordinates. Null data, source departure or producer failure clears prepared preview/description; original and cleanup failures are retained together. An already active drag rejects a second start. An assertive native announcement describes a successful grab when the application's bridge exists; the GUI operation also works without a bridge.

Accessible drop tests the requested control and eligible ancestors, preserving stopping filters and connected-section ownership. MouseFilter.Ignore does not exclude an explicit keyboard target. The sentinel remains non-positional through affine ancestors. Ordinary completion owns delivery, success, preview release and drag-end notifications, including target retirement or failure. Missing active data rejects explicitly. Read-only and intrinsic text type/capacity checks cannot be bypassed by a custom acceptance delegate.

LineEdit/TextEdit and inherited CodeEdit produce selections without a preceding pointer press and drop at their current caret. Unicode scalar offsets, ordinary history, move/copy modifier behavior, source validity and secret/read-only policy remain active. LineEdit preflights complete normalized capacity before accepting a pointer or accessible drop, then checks again after IME/focus preparation. This prevents deletion of a source suffix rejected by MaxLength. User control characters follow ordinary single-line normalization. A changed/rejecting target fails safely instead of reporting a partial successful transfer.

The permanent InputUIAccessibilityDragAndDrop setting is unbound initially. Configure its typed binding and reload InputMap before use. Unhandled GUI input invokes the operation on current focus after ordinary GUI callbacks; analog input uses the by-event transition gate. Cancel press ends the transaction; cancel release does not. Scene source feedback appends whether the current data can be dropped and describes the named drop/cancel commands. Description/locale changes prepare the two feedback strings; later acceptance toggles reuse them. Generic producers should QueueAccessibilityUpdate when their acceptance model changes. Built-in line text/caret/selection/limit changes queue their own refresh.

Native requests and topology, preview creation, text edits and description preparation are cold operations. Prepared name/context publication and accepted/rejected feedback transitions retain the warmed managed-frame boundary. Rendering and native allocator, physical-device, foreign-platform and human-reader acceptance remain separate.

## Contextual navigation

Control.OnAccessibilityGetContextualInfo supplies the standard native HelpText property. Control.OnGetAccessibilityContainerName describes an immediate child; its default asks the parent Control to describe this container. A non-Control parent ends that inheritance. Context is a suffix to the normal authored/displayed caption. Queued source/ancestor updates, topology and locale changes refresh retained fragments; unchanged frames never call the hooks. Custom producers must QueueAccessibilityUpdate when their context changes. Hook failures use ordinary owner-frame aggregation; retiring a source during a hook stops remaining publication. The strings and generations are transient and are not scene properties.

Native Name/Cache/name-change events compose base caption, container fragment and ExtraInfo with spaces and outer trim using the existing stack/native UTF-8 buffer. No per-frame managed concatenation is required. Explicit UpdateSetName replaces the complete base/container name, while ExtraInfo remains independent. It persists until the source caption or container fragment changes, then automatic output restores the current source name. Scene-copying preserves authored base metadata, not cached context or an element RID.

UpdateSetShortcut publishes human-readable direct shortcut text. Empty clears it; this method does not create an input binding. Action.GetKeyBinding and GetActions publish the standard `mnemonic;sequence;shortcut` triple, using the direct third field. Click receives it when present; otherwise it belongs to the first/default native action. Other actions return empty. Button-family sources read the actual borrowed Shortcut at query time, including in-place input-event edits and disposal. Menu items read their enabled Shortcut or accelerator; disabling item shortcuts clears their published binding. Formatting occurs only on cold native queries, so mutable borrowed events do not introduce frame polling allocations. Explicit service overrides, including an empty override and an override before the first query, persist until observed source text changes. User event-description failures return a native failure and aggregate on the owner; retirement cannot continue a query with a stale identity.

UpdateSetClassName publishes the native `class` attribute; automatic Control sources initially supply their runtime class name. UpdateSetRoleDescription publishes the author-localized `roledescription` attribute. It preserves the standard role enum, machine role name, actions and states. Empty strings remove either attribute, equal values are silent, and null rejects. These methods retain service owner, availability and live-RID guards. Explicit attribute overrides persist for the identity; a replacement source identity receives its own defaults.

[Accessible.xml](https://github.com/GNOME/at-spi2-core/blob/3e62300dae26649dc002f799c8f16bf792316b04/xml/Accessible.xml) and [Action.xml](https://github.com/GNOME/at-spi2-core/blob/3e62300dae26649dc002f799c8f16bf792316b04/xml/Action.xml) pin HelpText and the keybinding triple. [The GTK accessibility contract](https://gnome.pages.gitlab.gnome.org/gtk/gtk4/section-accessibility.html) describes contextual help and role descriptions. AccessibilityContextTests and its independent libatspi/GIO client exercise names/help, source overrides, real key activation, mutable/menu shortcuts, raw GetActions, invalid ordinals, Unicode/native-buffer boundaries, reparenting, callback retirement/failure and fresh reopening. Prepared idle/active owner-frame budgets, strict NativeAOT, rendering, human-reader, native-heap, physical-device, platform and package gates remain distinct.

## Native hyperlinks

UpdateSetURL(RID id, string url) publishes one external destination through AT-SPI Hyperlink. The supplied string is retained unchanged; native UTF-8 conversion uses the existing normalization for NUL and invalid Unicode. Empty clears the URL, equal values are silent and null rejects. Normal native-bridge, live-RID and display-owner guards apply. Publishing never opens or validates a resource, installs an input handler or changes the element's role or actions.

The anchor exists while either a nonempty URL or a live UpdateSetInPageLinkTarget target exists. GetURI(0) returns the URL, including empty for an internal-only anchor. GetObject(0) returns the internal target when present, otherwise the source accessible itself, following the [GTK external-link implementation](https://github.com/GNOME/gtk/blob/gtk-3-24/gtk/a11y/gtklinkbuttonaccessible.c). Other indices reject. StartIndex and EndIndex are -1 because this slice does not produce a containing Hypertext range. IsValid describes the retained anchor, without probing target reachability. Clearing one endpoint or retiring its internal target preserves the other. With neither endpoint, Accessible stops advertising Hyperlink; an existing interface handle reports zero anchors/IsValid=false and rejects GetURI/GetObject. Registration retires with the owning element.

NAnchors uses signed Int32 in both introspection and values. The pinned [libatspi client](https://github.com/GNOME/at-spi2-core/blob/3e62300dae26649dc002f799c8f16bf792316b04/atspi/atspi-hyperlink.c) requests `i` and the [provider adapter](https://github.com/GNOME/at-spi2-core/blob/3e62300dae26649dc002f799c8f16bf792316b04/atk-adaptor/adaptors/hyperlink-adaptor.c) returns Int32. Their XML annotation incorrectly says Int16; following it previously made libatspi read zero anchors. Native consumers expecting the earlier Int16 annotation must accept Int32.

LinkButton automatically publishes its stored URI and uses its existing Click pipeline. Explicit service URL overrides, including empty, persist until the observed source URI changes. The URI setter queues semantic refresh, including dependent custom contextual help. A replacement/reparented identity receives the current source value. Packed scenes and duplication preserve the authored URI while omitting native identities and cached overrides.

ELECTRON2D_TEST_ACCESSIBILITY_URL=managed checks null/availability guards and packed/duplicated URI ownership. compatibility/gpu execute [AccessibilityURLTests](../../tests/Electron2D.Tests/AccessibilityURLTests.cs) and an independent [libatspi/GIO client](../../tests/AccessibilityClient/url.py): real URI discovery, external/internal coexistence, count ABI, invalid indices, clearing, target/source retirement, reparenting, long/Unicode/native-normalized strings, override refresh, actual activation hooks, callback failure and fresh reopening. These tests never launch an external resource; the source hook records its actual configured URI. Prepared 64 idle/64 active owner-frame samples and a strict source NativeAOT consumer retain separate native-heap, hardware, foreign-platform, human-reader, OS handoff and release-package gates.


Remaining braille/state presentation has an explicit consumer dependency. The 2026-10-27 check found Orca's shared [role-description path](https://gitlab.gnome.org/GNOME/orca/-/blob/main/src/orca/ax_object.py) reading brailleroledescription, but its [braille-label path](https://gitlab.gnome.org/GNOME/orca/-/blob/main/src/orca/scripts/web/braille_generator.py) is restricted to web content. A working desktop braille-label reader and GUI propagation are required for the connected slice. State description still needs an actual native consumer/projection; the inspected [Unix adapter](https://github.com/AccessKit/accesskit/blob/f3026222559c7f5f46d8f25cb7404de68eb149f0/atspi-common/src/node.rs) does not supply one. These remain implementation obligations in coverage.

## Producer models

ElementSetMeta<T> associates one model or value with a live element. A registered action can capture its RID and use ElementGetMeta<T> on each invocation, so replacing the slot changes the model used by the existing native action. Empty slots return default(T); null clears regardless of the former type. Nullable value types distinguish absence from zero/default. Reference retrieval preserves identity and accepts compatible base/interface types. Value types box on assignment and copy on retrieval. Incompatible nonempty reads throw InvalidCastException; no numeric coercion or deep cloning occurs. Both methods enforce the ordinary native-owner, availability and live-RID guards, including no-op writes and empty reads.

The slot holds a strong reference until replacement, clearing or retirement. It never owns or disposes the caller's model. Parent-owned subelements, source detach, window/display shutdown and failure cleanup release their slots. A replacement scene identity starts empty; scene duplication/packing does not retain the native RID or model. Reparenting a live generic element preserves its slot. Metadata is producer-local and is not a native attribute, a stored scene property or an automatic name/value source.

```csharp
AccessibilityServer.ElementSetMeta(element, model);
AccessibilityServer.UpdateAddAction(element, AccessibilityServer.AccessibilityAction.Click, () =>
{
    var current = AccessibilityServer.ElementGetMeta<MyModel>(element);
    if (current is not null) current.Activate();
});
```

ELECTRON2D_TEST_ACCESSIBILITY_MODELS=managed checks unavailable-service guards. compatibility/gpu execute [AccessibilityModelTests](../../tests/Electron2D.Tests/AccessibilityModelTests.cs) with an independent [native action client](../../tests/AccessibilityClient/models.py): current/replaced/cleared models, CLR reference/value behavior, wrong-type/owner/identity rejection, weak-reference release, scene detach/reattach, reentrant retirement, callback failure and fresh reopening. Prepared 64 idle/64 active owner-frame samples cover reference-model reads/replacement. Value-type assignment may box. NativeAOT source execution, package delivery, native heap, physical hardware, foreign adapters and human-reader acceptance remain separate gates.
