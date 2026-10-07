# GraphSlotMetadata

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.GraphSlotMetadata`. **Source:** [source](../../src/Scene/GUI/GraphNode.cs). **Component:** [Graph authoring](../components/graph-authoring.md).

## Description

Exact typed borrowed runtime slot payload. TryGet succeeds only for the original generic type, including stored nulls. It is not serialized and does not create a universal object/value protocol.

## Members

| Declaration | Contract |
| --- | --- |
| [`public System.Boolean TryGet<T>(out T value)`](#member-1884eabb1f5d) | Reads a payload only under its originally assigned type. |

## Example

```csharp
using var node = new GraphNode();
node.SetSlotMetadataLeft<string?>(0, null);
bool hasTypedNull = node.GetSlotMetadataLeft(0)!.TryGet<string?>(out var value);
```

## Lifetime and verification

Attached reads and mutations use the scene owner thread. Own mutations reject active port/connection drawing; borrowed resources remain caller-owned. GraphNode and GraphFrame titlebars and title labels, and GraphEdit menu/layers cannot be disposed independently. GraphElement/GraphEdit disposal releases owned children and event subscriptions. See the component for executable checks, preparation boundaries and remaining dependencies.

## Member details

<a id="member-1884eabb1f5d"></a>
### `TryGet(ref T)`

```csharp
public System.Boolean TryGet<T>(out T value)
```

Reads a payload only under its originally assigned type.

Whether the type matches, including stored nulls.

- `value`: Borrowed payload on success.
