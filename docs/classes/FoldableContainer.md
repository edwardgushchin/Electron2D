# FoldableContainer

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public partial class FoldableContainer : Container`.

**Source:** [state](../../src/Scene/GUI/FoldableContainer.cs), [layout/input/draw](../../src/Scene/GUI/FoldableContainer.Layout.cs), [storage](../../src/Scene/GUI/FoldableContainer.Storage.cs).

**Inherits:** [Container](Container.md).

## Description

A collapsible panel with an interactive themed title. Its ordinary non-top-level controls share the available content rectangle. Deferred folding hides these controls; expansion shows them again, including controls previously hidden by callers. Internal title controls stay visible. TitlePosition uses shared [BarPosition](BarPosition.md) Top/Bottom; bottom styles are vertically reflected. Layout mirrors RTL and combines header/content margins, title text and trailing title controls. Minimum and desired sizes follow actual content and title state; inherited size flags and maximum bounds apply.

The title uses translated text, explicit or domain language, inherited/automatic bidirectional shaping, alignment and overrun policies. Six-category branch/project/default themes supply colors, fonts, arrows, focus and title/body styles. Native font/icon resources remain borrowed. Empty or unavailable text font contributes no text geometry. Exact pointer press in the title or ui_accept activates; tooltip is restricted to the title and folded hit testing excludes the content area. Focus has a visible style.

Folded assignments are silent. Fold(), Expand() and activation emit FoldingChanged with the actual folded state, including equal requests or refusal to fold the last expanded group member. [FoldableGroup](FoldableGroup.md) folds peers before Expanded, which observes the requesting member's precommit state. Required state/layout work continues after callbacks fail; same-target reentry terminates and later group selection supersedes an older request. Attached access/mutation use the scene owner; mutation rejects capture/disposal.

AddTitleBarControl adopts a live control as an internal front child. Existing registration, cycles and conflicting names reject. RemoveTitleBarControl detaches it without disposal. Only title controls explicitly owned by the captured root are saved; their typed relative references restore internal membership and ordering after parenting. Derived node types supply their own exact scene factory.

## Example

```csharp
var group = new FoldableGroup();
var settings = new FoldableContainer("Settings") { FoldableGroup = group };
var details = new FoldableContainer("Details") { FoldableGroup = group };
settings.AddChild(new Panel { CustomMinimumSize = new Vector2(160, 80) });
settings.AddTitleBarControl(new Button("Help"));
```

## API summary

| Complete signature | Default/contract |
| --- | --- |
| `public FoldableContainer()` | Expanded, MouseFilter.Stop, FocusMode.All. |
| `public FoldableContainer(string title)` | Initial untranslated title. |
| `public bool Folded { get; set; }` | False; silent state assignment. |
| `public FoldableGroup? FoldableGroup { get; set; }` | Null; borrowed selection resource. |
| `public string Title { get; set; }` | Empty; translated for shaping/drawing. |
| `public string Language { get; set; }` | Empty; domain/tool locale fallback. |
| `public HorizontalAlignment TitleAlignment { get; set; }` | Left; Left/Center/Right accepted. |
| `public BarPosition TitlePosition { get; set; }` | Top; Max rejects. |
| `public TextDirection TitleTextDirection { get; set; }` | Auto; typed BIDI policy. |
| `public TextOverrunBehavior TitleTextOverrunBehavior { get; set; }` | NoTrimming. |
| `public event Action<bool>? FoldingChanged` | Actual state after explicit/input request. |
| `public void Fold()` / `public void Expand()` | Request and notify actual state. |
| `public void AddTitleBarControl(Control control)` | Adopt internal header control. |
| `public void RemoveTitleBarControl(Control control)` | Detach without disposal. |

Protected minimum/size-flag, input/hit/tooltip, notification, descriptor/factory and disposal overrides connect inherited contracts. Factory restoration uses only validated reserved title-control descriptors, never arbitrary member reflection.

## Verification

FoldableTests exercises group ordering/failure/reentry, callback removal during title measurement, pointer/action, RTL/bottom layout, child/title visibility, owner/capture guards and exact scene-local groups/title references in a fresh process. Linux Wayland FoldableNativeTests checks actual header/content/focus geometry and queued native keyboard input on GPU/Compatibility; 64 warmed alternating group/layout/render frames allocate zero managed bytes on the owner thread. First-use geometry remains an allocating preparation path. Native allocations, other platforms and owner acceptance remain separate gates. [ADR 0081](../decisions/rendering.md#adr-0081) owns layout; [coverage](../coverage/classes/FoldableContainer.md) accounts the public family.
