# Range

Last updated: 2026-09-27

**Inherits:** [Control](Control.md), CanvasItem, Node, ElectronObject · **Inherited By:** [TextureProgressBar](TextureProgressBar.md)

**Declaration:** `public abstract class Range : Control` · **Source:** [Range.cs](../../src/Scene/GUI/Range.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description

A numeric control base with double storage and shared value/configuration policy. It has no default visual content; subclasses consume OnValueChanged and retained drawing. Min/max/step/page/allow/exponential fields may be shared through weak runtime groups. Rounded is local to the writer. Mutations validate attached owner/capture affinity across shared peers before committing; callback failure does not roll back committed state.

Ordinary shared notifications run only for attached owners in host insertion order; explicit Share notifications also run on a detached target. All owners read committed shared state. Each value delivery runs the protected hook before ValueChanged, then redraw; SetValueNoSignal still runs the hook/redraw. Errors continue required/later work and aggregate. Reentrant share/unshare/disposal invalidates stale queued owner delivery. Cached snapshots are separate for callback nesting depths and cleared after delivery.

## Example

Partial snippet using concrete range controls:

```csharp
var first = new TextureProgressBar { Step = 0.1, MaxValue = 10 };
var second = new TextureProgressBar();
first.Share(second);
first.Value = 3.3;
second.Unshare();
```

## API summary

| Signature | Contract/default |
| --- | --- |
| `protected Range()` | Initializes default numeric policy and vertical ShrinkBegin. |
| `public double MinValue { get; set; }` | Finite, 0. Raises max if needed. |
| `public double MaxValue { get; set; }` | Finite, 100; clamps to min. |
| `public double Value { get; set; }` | 0; snaps, rounds and clamps. NaN retained. |
| `public double Step { get; set; }` | Finite, 0.01; positive enables snapping. |
| `public double Page { get; set; }` | Finite, 0; clamps to [0,max-min]. |
| `public double Ratio { get; set; }` | Clamped linear/exponential representation. |
| `public bool AllowGreater { get; set; }` | False; permits storage above max-page. |
| `public bool AllowLesser { get; set; }` | False; permits storage below min. |
| `public bool ExpEdit { get; set; }` | False; logarithmic ratio for nonnegative min. |
| `public bool Rounded { get; set; }` | Local false; integer midpoint away from zero. |
| `public void SetValueNoSignal(double value)` | Value calculation plus hook/redraw, no ValueChanged. |
| `public void Share(Range with)` | Moves one target into this shared state. |
| `public void Unshare()` | Copies into independent state, no events. |
| `public event Action? Changed` | Config min/max/step/page changes. |
| `public event Action<double>? ValueChanged` | Changed shared value after hook. |
| `protected virtual void OnValueChanged(double newValue)` | Typed consumer hook, including silent updates. |
| `public override string[] GetConfigurationWarnings()` | Reports ExpEdit with negative minimum. |

## Property descriptions

<a id="minvalue"></a><a id="maxvalue"></a><a id="page"></a>
**MinValue / MaxValue / Page:** config changes clamp page and resnap value first, then Changed. Input is finite; min raises max, max cannot fall below min. Page reduces the normal upper cap to max-page.

<a id="value"></a><a id="step"></a><a id="rounded"></a>
**Value / Step / Rounded:** positive step snaps relative to min, except an excessively large min uses zero offset. Decimal arithmetic preserves representable decimal intent without allocation; binary fallback handles outside values. Rounded then applies midpoint-away integer rounding. Upper/lower clamps follow allow flags. Step and Rounded edits do not immediately resnap; only Step emits Changed. Repeated NaN Value assignment suppresses duplicate ValueChanged; NoSignal NaN redraw semantics remain distinct.

<a id="allowgreater"></a><a id="allowlesser"></a><a id="expedit"></a><a id="ratio"></a>
**AllowGreater / AllowLesser / ExpEdit / Ratio:** policy edits do not resnap or emit Changed. Ratio clamps Value for representation, independently of allow flags. Linear assignment rounds its percentage step before ordinary calculation; exponential assignment uses log2/pow2 and min zero's special exponent. Approximately equal bounds return one. Page still caps the assigned final value. Nonfinite Value can be stored; a concrete renderer rejects a nonfinite Ratio rather than reporting a valid frame.

## Method and event descriptions

<a id="setvaluenosignal"></a>
**SetValueNoSignal:** same calculation; changed attached owners run OnValueChanged and redraw without ValueChanged.

<a id="share"></a><a id="unshare"></a>
**Share / Unshare:** share only moves the specified target; its old peers remain in their group. Typed Range input prevents wrong-kind Node calls. Share notifies Changed then hook/ValueChanged on the target even when detached; equal-group sharing still notifies. Unshare copies all shared fields, retaining local Rounded, with no events. Both enforce ordinary lifetime/owner/capture constraints.

<a id="changed"></a><a id="valuechanged"></a><a id="onvaluechanged"></a>
**Changed / ValueChanged / OnValueChanged:** committed current state is observed at delivery. Hook runs before the value event; failures are aggregated after later required notifications/owners. Subscribers follow ordinary multicast semantics within one event.

## Lifecycle, errors and verification

Groups store weak owners and release links on disposal; snapshots retain owners only during delivery. PackedScene stores config independently, with snapping policies restored before Value, and omits sharing links. Finite config guard errors occur before mutation; disposed/capture/off-owner errors remain authoritative. [RangeProgressTests](../../tests/Electron2D.Tests/RangeProgressTests.cs) verifies timing/values/errors/reentry/packing and zero bytes for 64 warmed shared updates. Inherited SizeFlagsVertical defaults to ShrinkBegin, with a matching stored descriptor and actual Container consumption under [ADR 0081](../decisions/rendering.md#adr-0081). [Coverage](../coverage/classes/Range.md) marks this dependency Implemented. Native/platform/accessibility/owner limits are recorded in [ADR 0080](../decisions/rendering.md#adr-0080).
