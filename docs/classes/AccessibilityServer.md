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
| `public static void UpdateAddAction(RID id, AccessibilityAction action, Action<double> callback)` | [UpdateAddAction](#updateaddaction) |
| `public static void UpdateSetNumValue(RID id, double position)` | [Numeric values](#numeric-values) |
| `public static void UpdateSetNumRange(RID id, double min, double max)` | [Numeric values](#numeric-values) |
| `public static void UpdateSetNumStep(RID id, double step)` | [Numeric values](#numeric-values) |
| `public static void UpdateSetNumJump(RID id, double jump)` | [Numeric values](#numeric-values) |
| `public static void UpdateSetFocus(RID id)` | [UpdateSetFocus](#updatesetfocus) |
| `public static void SetWindowFocused(int windowID, bool focused)` | [SetWindowFocused](#setwindowfocused) |
| `public static void SetWindowRect(int windowID, Rect2 rectOut, Rect2 rectIn)` | [SetWindowRect](#setwindowrect) |

## Enumerations

| Type | Contract |
| --- | --- |
| [AccessibilityRole](AccessibilityServer.AccessibilityRole.md) | All 48 semantic role selectors and native role projections. |
| [AccessibilityFlags](AccessibilityServer.AccessibilityFlags.md) | Ten independent state/geometry selectors. |
| [AccessibilityAction](AccessibilityServer.AccessibilityAction.md) | Click, Focus, Blur, Decrement, Increment and numeric SetValue; broader payloads remain incomplete. |

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

Registers or replaces a borrowed compiled Action for Click, Focus, Blur, Decrement or Increment. The Action<double> overload accepts numeric SetValue only. Other payload signatures await their real native consumers. Native requests run during the display owner's event pump. Hidden, disabled, detached, departing, queued and disposed scene controls reject activation; callback exceptions return a native failure and aggregate after the pump.

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

[AccessibilityFoundationTests](../../tests/Electron2D.Tests/AccessibilityFoundationTests.cs), its managed/lifetime companions and the separate [AT-SPI client](../../tests/AccessibilityClient/probe.py) exercise public scene/native behavior. The current profile covers Accessible/Application/Component/Action/Value/Cache. Native text, color values, table, richer relationships and remaining payload actions retain exact obligations in [coverage](../coverage/classes/AccessibilityServer.md). Warmed owner managed allocations, native heap allocation totals, hardware rendering, foreign platforms and human assistive-client acceptance are distinct gates.

<a id="numeric-values"></a>
### Numeric values

UpdateSetNumValue publishes the actual double and enables the native Value interface. It preserves IEEE values, including NaN already retained by a Range. UpdateSetNumRange requires finite min <= max; UpdateSetNumStep and UpdateSetNumJump require finite nonnegative amounts. Configuration can precede the first value; it never clamps or changes the producer itself. Repeated equal values, including NaN, suppress redundant output.

The Action<double> overload of UpdateAddAction accepts SetValue only. A native CurrentValue write passes a finite requested value to that callback on the display owner. The callback owns mutation and publication; successful invocation does not invent a result. No-argument registration accepts Click/Focus/Blur/Decrement/Increment and rejects SetValue. Positive jump plus numeric callback exposes page-decrement/page-increment native actions using the current published value. Zero jump removes them. Value Text formats the number on query; SpinBox uses its displayed affixes/numerals.

Automatic Range descendants publish values, authored bounds, positive Step or one, and Page as jump. Slider, ScrollBar, SpinBox and progress displays have their native roles. ProgressBar/TextureProgressBar and non-editable Slider/SpinBox reject writes and step/page actions. Hidden/disabled/detached/queued/retired controls also reject requests. Ordinary Value policy owns snapping, rounding, page caps, shared peers, protected hooks and ValueChanged. Native writes reject nonfinite requests before invoking a callback; callback failures propagate through the pump's aggregate policy. Source updates publish after ordinary scene processing; custom NotificationAccessibilityUpdate overrides survive until the corresponding source field changes.
