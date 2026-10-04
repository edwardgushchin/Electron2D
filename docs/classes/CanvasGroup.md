# CanvasGroup

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class CanvasGroup : Entity`.

**Source:** [CanvasGroup.cs](../../src/Scene/2D/CanvasGroup.cs).

**Inherits:** [Entity](Entity.md).

## Description

Fits submitted same-Z children into a native transparent backbuffer, then draws the owner as one object. Default SelfModulate opacity is applied once to the completed image, so overlapping children do not accumulate that opacity. Modulate retains ordinary inherited child/owner participation. FitMargin and ClearMargin are finite nonnegative screen pixels, initially ten; UseMipmaps starts false and generates GPU backbuffer levels for custom material LOD. A material replaces the built-in group shader. Recorded owner commands supply the drawable shape and bypass fit expansion; otherwise automatic fitting maps screen bounds through the owner inverse/forward transform and needs an invertible transform. Empty groups without submitted child content draw no invented rectangle. Other effective Z values, TopLevel and independent canvas branches retain separate drawing. GetConfigurationWarnings reports an attached ancestor group; nested same-Z rendering and writable backbuffer reads by group children reject explicitly. Compatibility executes baseline composition but rejects shaders/mipmaps, and software rejects its unsupported premultiplied group blend. Derived types can provide OnDraw owner geometry and must supply their own factory for scene reconstruction.

Attached reads and mutations use the scene owner; mutation also rejects scene capture. Disposed access rejects. In-memory PackedScene reconstruction has executable checks; file/editor authoring and owner visual acceptance are separate. [The composition component](../components/canvas-rendering.md#group-composition-and-screen-snapshots) records native lifetime, backend and allocation limits.

## Example

Requires the indicated authored drawable/material objects and a live Window. CanvasCompositionTests compiles and executes this public workflow.

```csharp
var group = new CanvasGroup
{
    Name = "fade_group",
    SelfModulate = new Color(1, 1, 1, 0.5f),
    FitMargin = 0,
};
group.AddChild(firstDrawable);
group.AddChild(secondDrawable);
window.AddChild(group);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public CanvasGroup()` | Creates a group with ten-pixel fit/clear margins and mipmaps disabled. |

## Constructor Descriptions

<a id="member-53af6435189b"></a>
### .ctor

`public CanvasGroup()`

Creates a group with ten-pixel fit/clear margins and mipmaps disabled.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Single ClearMargin { get; set; }` | Gets or sets the additional screen-pixel expansion of the transparent clearing rectangle. |
| `public System.Single FitMargin { get; set; }` | Gets or sets the screen-pixel expansion of the fitted drawable rectangle. |
| `public System.Boolean UseMipmaps { get; set; }` | Gets or sets whether native backbuffer mipmaps are generated before compositing. |

## Property Descriptions

<a id="member-22d95a7ffb69"></a>
### ClearMargin

`public System.Single ClearMargin { get; set; }`

Gets or sets the additional screen-pixel expansion of the transparent clearing rectangle.

Value: Ten initially; finite and nonnegative.

Remarks: The extra cleared border prevents stale backbuffer data from entering filtered group samples.

System.ArgumentOutOfRangeException: The margin is negative or nonfinite.

System.InvalidOperationException: Mutation is off-owner or during scene capture.

System.ObjectDisposedException: This node is disposed.

<a id="member-a80556c96ac0"></a>
### FitMargin

`public System.Single FitMargin { get; set; }`

Gets or sets the screen-pixel expansion of the fitted drawable rectangle.

Value: Ten initially; finite and nonnegative.

Remarks: Every assignment commits and requests redraw. Larger margins allow material effects outside child bounds.

System.ArgumentOutOfRangeException: The margin is negative or nonfinite.

System.InvalidOperationException: Mutation is off-owner or during scene capture.

System.ObjectDisposedException: This node is disposed.

<a id="member-f54f1b228a9e"></a>
### UseMipmaps

`public System.Boolean UseMipmaps { get; set; }`

Gets or sets whether native backbuffer mipmaps are generated before compositing.

Value: False initially.

Remarks: Generation is useful for explicit LOD sampling in a custom group shader. The compatibility profile rejects a submitted mipmapped group before native drawing rather than ignoring this policy.

System.InvalidOperationException: Mutation is off-owner or during scene capture.

System.ObjectDisposedException: This node is disposed.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Creates a reusable factory for packed-scene instances of this exact runtime node type. |
| `public override System.String[] GetConfigurationWarnings()` | Returns this node's current configuration warnings for tooling. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |

## Method Descriptions

<a id="member-f1fd048c036c"></a>
### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Creates a reusable factory for packed-scene instances of this exact runtime node type.

Returns: A non-null factory that creates a fresh node of the exact same runtime type.

Remarks: The base implementation supports only an exact Electron2D.Node. Derived node types that can be packed must return a static, non-capturing factory that remains valid after the source node is disposed and creates a live, detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore the instance state.

System.NotSupportedException: A derived node has not explicitly supplied an instancing factory.

<a id="member-1557e5789f0f"></a>
### GetConfigurationWarnings

`public override System.String[] GetConfigurationWarnings()`

Returns this node's current configuration warnings for tooling.

Returns: An empty array by default. Overrides return ordered warning messages and should include base warnings.

Remarks: This query does not cache results, emit events or require an edited scene. Attached queries run on the scene owner thread. Consumers may call it after NodeConfigurationWarningChanged to refresh their display.

System.InvalidOperationException: An attached query runs off the scene owner thread.

System.ObjectDisposedException: This node is disposed.

<a id="member-5225450789af"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.


## Verification and limits

[CanvasCompositionTests](../../tests/Electron2D.Tests/CanvasCompositionTests.cs) checks authoring, callbacks, typed packing/owner boundaries and actual native pixels. Linux Wayland GPU and hardware compatibility execute the baseline; GPU HLSL/GLSL additionally check custom screen reading and generated LOD. Stable-size warm active/idle intervals measure managed allocation; native allocator totals, other platforms, large scenes, editor inspector integration and human acceptance remain separate.

An ancestor Y-sort treats a CanvasGroup as one ordering boundary; the group performs its own child Y-sort internally. This keeps its capture/composite range atomic while other-Z descendants remain separate. CanvasCompositionTests verifies the ancestor and internal paths.
