# ButtonGroup

Last updated: 2026-09-27

**Declaration:** `public class ButtonGroup : Resource` · **Source:** [ButtonGroup.cs](../../src/Scene/Resources/ButtonGroup.cs)

**Inherits:** [Resource](Resource.md).

Coordinates radio selection among [BaseButton](BaseButton.md) controls. Nodes register by assigning ButtonGroup, but the resource does not own them. Membership is weak and runtime-only. Disposed/collected nodes are removed. The resource defaults ResourceLocalToScene=true so separate scene instances have independent selection while preserving the shared group identity of buttons within one instance.

```csharp
using var group = new ButtonGroup { AllowUnpress = true };
var button = new Button { ToggleMode = true, ButtonGroup = group };
button.ButtonPressed = true;
BaseButton? selected = group.GetPressedButton();
```

## API summary

| Signature | Contract |
| --- | --- |
| `public ButtonGroup()` | Scene-local, no members, AllowUnpress=false. |
| `public bool AllowUnpress { get; set; }` | Allows user activation to clear the selected button. |
| `public BaseButton[] GetButtons()` | Caller-owned array of borrowed live members. |
| `public BaseButton? GetPressedButton()` | A selected member, or null. |
| `public event Action<BaseButton>? Pressed` | Activated member, after peer unpress and before its local toggle signal. |
| `protected override Resource CreateDuplicateInstance()` | Exact group factory; subclasses provide their own. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies AllowUnpress without copying members. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores AllowUnpress and the true local-scene default. |
| `protected override void Dispose(bool disposing)` | Clears weak membership and subscribers. |

## Constructor and property descriptions

### ButtonGroup

Creates a group without selecting any button. Assigning buttons does not force selection; it only changes membership and visual radio decoration.

### AllowUnpress

`public bool AllowUnpress { get; set; }` starts false. When false, user activation of a toggle member forces it selected after inversion; activation still emits true. When true, activating the selected button may clear all selection. Assignment is silent, emits no Changed event and does not change existing members. Explicit ButtonPressed=false and SetPressedNoSignal bypass the user-unpress restriction.

## Method descriptions

### GetButtons

`public BaseButton[] GetButtons()` returns a fresh array containing borrowed live member nodes. Later membership changes do not mutate the array. Ordering is unspecified. Disposed group access throws ObjectDisposedException.

### GetPressedButton

`public BaseButton? GetPressedButton()` returns a member whose ButtonPressed is true, or null. Multiple members may be selected through silent setup; selection among them is unspecified. Attached node state queries require that node's scene owner thread. Disposed group access throws ObjectDisposedException.

### CreateDuplicateInstance

`protected override Resource CreateDuplicateInstance()` creates the exact built-in group. A derived resource must override the standard Resource factory to preserve its runtime type.

### CopyCustomStateTo

`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` copies AllowUnpress. The inherited resource protocol handles names, scene-local state and aliases. Member references, subscribers and iteration storage are never copied.

### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` appends stored AllowUnpress and replaces the inherited ResourceLocalToScene descriptor so reverting it restores true.

### Dispose

`protected override void Dispose(bool disposing)` clears membership and event subscribers, then performs inherited resource cleanup. It does not dispose member nodes. Buttons still borrowing the disposed resource reject operations requiring live group state; they can clear or replace that reference.

## Event descriptions

### Pressed

`public event Action<BaseButton>? Pressed` receives the borrowed activating button after peers are unpressed, before its toggle hook/event and Pressed hook/event. Programmatically turning ButtonPressed on also emits the group event, but not the button's Pressed event. A permitted user unpress still emits this group event. SetPressedNoSignal never does.

## Invariants and verification

Activation validates mutable peers before committing selection. Captured membership generations prevent callbacks from acting on buttons removed/reassigned during the operation; nested newer activation cancels the older group traversal. Callback errors are collected so valid peer/local phases still complete. Prepared group activation reuses snapshot buffers. Public snapshots and first-use capacity growth allocate.

[BaseButtonTests](../../tests/Electron2D.Tests/BaseButtonTests.cs) covers selection ordering, AllowUnpress, silent writes, membership snapshots, disposal, copied/scene-local groups, reentrant callbacks, failure continuation and prepared input allocations. Final executed results and backend limits belong to the completed slice report.
