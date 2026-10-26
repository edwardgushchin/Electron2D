# AccessibilityServer

Last updated: 2026-10-26

- Declaration: `public sealed partial class AccessibilityServer : ElectronObject`
- Source: [AccessibilityServer.cs](../../src/Servers/Accessibility/AccessibilityServer.cs)
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
| `public static void UpdateSetChecked(RID id, bool isChecked)` | [UpdateSetChecked](#updatesetchecked) |
| `public static void UpdateSetBounds(RID id, Rect2 rect)` | [UpdateSetBounds](#updatesetbounds) |
| `public static void UpdateSetTransform(RID id, Transform transform)` | [UpdateSetTransform](#updatesettransform) |
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

## Enumerations

| Type | Contract |
| --- | --- |
| [AccessibilityRole](AccessibilityServer.AccessibilityRole.md) | All 48 semantic role selectors and native role projections. |
| [AccessibilityFlags](AccessibilityServer.AccessibilityFlags.md) | Ten independent state/geometry selectors. |
| [AccessibilityAction](AccessibilityServer.AccessibilityAction.md) | Click/Focus/Blur, numeric requests, directed scalar selection and selected-text replacement; other payload families remain incomplete. |

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

Registers or replaces a borrowed compiled Action for Click, Focus, Blur, Decrement or Increment. The Action<double> overload accepts numeric SetValue only. The four-argument RID/scalar overload accepts SetTextSelection; the string overload accepts ReplaceSelectedText. Their producer and source rules are specified in [Native text](#native-text). Other payload signatures await their real native consumers. Native requests run during the display owner's event pump. Hidden, disabled, detached, departing, queued and disposed scene controls reject activation; callback exceptions return a native failure and aggregate after the pump.

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

[AccessibilityFoundationTests](../../tests/Electron2D.Tests/AccessibilityFoundationTests.cs), its managed/lifetime companions and the separate [AT-SPI client](../../tests/AccessibilityClient/probe.py) exercise public scene/native behavior. The current profile covers Accessible/Application/Component/Action/Value/Text/EditableText/Cache. Color values, table and remaining payload actions retain exact obligations in [coverage](../coverage/classes/AccessibilityServer.md). Warmed owner managed allocations, native heap allocation totals, hardware rendering, foreign platforms and human assistive-client acceptance are distinct gates.

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

Replaces the internal-link anchor. Empty otherID clears it. A real Hyperlink interface advertises one anchor and returns its target from GetObject(0). Other indices reject. Internal links have no external URI or originating Hypertext range: GetURI returns empty, StartIndex/EndIndex return -1. Clearing/retirement advertises no interface and reports zero anchors/IsValid=false on the already registered object.

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
