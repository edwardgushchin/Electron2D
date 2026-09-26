# VisibleOnScreenEnabler

Last updated: 2026-09-26

**Inherits:** [VisibleOnScreenNotifier](VisibleOnScreenNotifier.md), Entity, CanvasItem, Node, ElectronObject

**Declaration:** `public class VisibleOnScreenEnabler : VisibleOnScreenNotifier` · **Source:** [VisibleOnScreenEnabler.cs](../../src/Scene/2D/VisibleOnScreenEnabler.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description

Uses its inherited screen rectangle to control a borrowed Node's ProcessMode. Entry weakly caches the target and disables it before the first render sample. Screen entry writes the selected enabled policy, screen exit writes Disabled; rendering remains independent of ordinary processing. Event callbacks observe committed screen state before the target policy update. The update is still attempted after a failing handler, unless removal/disposal has cleared the binding.

Cache identity survives target moves and does not automatically switch to a replacement at the same path. Entry or a changed nonempty path resolves again; an equal path does not. A disposed target is ignored. Empty paths, changed targets and exit clear the cache without restoring the old target's mode. Target setters retain their owner, capture and physics participation guards.

## Example

Partial snippet, enabler as a direct child of the AI node:

```csharp
var enabler = new VisibleOnScreenEnabler
{
    Rect = new Rect2(-32, -32, 64, 64),
    EnableNodePath = "..",
    EnableMode = ScreenEnableMode.Inherit
};
aiNode.AddChild(enabler);
```

## API summary

| Signature | Contract |
| --- | --- |
| `public VisibleOnScreenEnabler()` | Parent path, Inherit enabled policy, inherited notifier defaults. |
| `public ScreenEnableMode EnableMode { get; set; }` | Inherit=0, Always=1 or WhenPaused=2. |
| `public string EnableNodePath { get; set; }` | Relative target path, default ".."; empty disables targeting. |

## Property descriptions

<a id="enablemode"></a>
**EnableMode:** every assignment immediately updates a cached attached target from current screen state, including equal assignments. An undefined enum throws ArgumentOutOfRangeException before mutation. The off-screen mode is always Disabled. [ScreenEnableMode](ScreenEnableMode.md) preserves the three numeric identities.

<a id="enablenodepath"></a>
**EnableNodePath:** rejects null. Detached assignment stores configuration. Attached changed nonempty paths clear the old cache, resolve once and apply current screen state. An unresolved path throws InvalidOperationException after the path is committed and cache cleared; fix the path or reenter to resolve again. No old target mode is restored.

## Lifecycle, errors and verification

Inherits notifier render sampling, silent exit/reset, queued event errors and entity ownership. PackedScene stores rectangle/mode/path and exact type; it stores neither weak target nor screen state. Node.ProcessMode supplies enabled/disabled notifications and CollisionObject.DisableMode effects, so a default Remove body actually detaches/reenters physics. A MakeStatic policy retains its backend role according to the physics contract. Cached depth-specific Node snapshots preserve reentrant transitions without warmed dictionary allocations.

[ScreenVisibilityTests](../../tests/Electron2D.Tests/ScreenVisibilityTests.cs) checks all policies, path cache identity/errors, disposal, packing and nested snapshots. [Native tests](../../tests/Electron2D.Tests/ScreenVisibilityRenderingTests.cs) verify actual physics participation and 64 warmed active neutral-target transitions with zero managed bytes on both Linux Wayland backends. Explicit physics resource remove/reentry may allocate during those lifecycle transitions; no zero-allocation claim covers that resource creation. Native allocator counts, other platforms and owner acceptance remain unverified. See [ADR 0078](../decisions/rendering.md#adr-0078).
