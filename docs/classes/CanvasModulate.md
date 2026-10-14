# CanvasModulate

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public class CanvasModulate : Entity`.

**Source:** [CanvasModulate.cs](../../src/Scene/2D/CanvasModulate.cs).

**Inherits:** [Entity](Entity.md).

## Description

Applies a color tint to the entire logical scene canvas, including unrelated drawable siblings. It draws no geometry; its position, rotation and scale do not restrict the tinted area. One visible node controls a canvas. Other visible modulators wait with their authored colors until the selected node hides or leaves. Games should use one visible modulator per canvas and should not depend on which duplicate wins. GetConfigurationWarnings reports multiple visible nodes.

Visibility follows ordinary Entity/CanvasLayer membership. CanvasLayer and independent World canvases remain separate; viewports sharing a World share modulation. Reparenting, TopLevel canvas rebinding, World replacement and disposal reconcile selection. The last departure restores opaque white. Detached nodes retain Color without changing a canvas; manually delivered canvas entry/exit notifications cannot activate or remove actual membership.

Color starts opaque white and accepts every finite component. Each assignment commits, including equal values. Active assignments update submission without requesting sibling redraw; hidden or waiting assignments only update authored state. Attached reads require the scene owner; mutations also reject capture and disposed state. Weak registrations preserve scene lifetime. Typed PackedScene and `.e2dscene` files restore the exact type and Color, without persisting selection or canvas identities.

Server CanvasSetModulate writes the same live color immediately. Active node Color assignment or selection changes can overwrite it. Native session cleanup restores the selected scene color while releasing server overrides. Existing material/light behavior applies: ordinary modulation works on current GPU/compatibility backends; GPU lighting consumes ambient modulation and unshaded materials bypass it. Compatibility lighting/shader prerequisites remain unchanged.

## Example

```csharp
var night = new CanvasModulate
{
    Name = "NightTint",
    Color = new Color(0.2f, 0.25f, 0.4f),
};
window.AddChild(night);
// Sibling sprites in this canvas now use the tint.
night.Hide(); // The canvas returns to white if there is no other visible modulator.
```

## API summary

| Complete C# signature | Contract |
| --- | --- |
| `public CanvasModulate()` | Visible detached node with opaque white Color. |
| `public Color Color { get; set; }` | Finite whole-canvas tint when selected. |
| `public override string[] GetConfigurationWarnings()` | Inherited warnings plus a duplicate-visible-modulator warning. |
| `protected override void OnNotification(int what)` | Reconciles actual canvas, visibility and World changes before inherited delivery; callback failures preserve committed state. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds the stored typed Color descriptor with opaque white default. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Exact built-in reconstruction; derived types supply their own factory. |

## Verification and limits

[CanvasModulateTests](../../tests/Electron2D.Tests/CanvasModulateTests.cs) exercises selection, layer/shared-world isolation, visibility, reparenting, World replacement, manual-notification rejection, disposal, warning failures, owner/capture guards and saved scenes in a fresh process, plus automatic/explicit root-window placement preservation. Native profiles inspect color phases, layer isolation, server override and GPU lighting/unshaded behavior, with 64 warmed owner frames. These checks cover the local Linux environment; native allocator counts, other platforms and human acceptance remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/CanvasModulate.md).
