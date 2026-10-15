# Node.ProcessThreadGroupMode

Last updated: 2026-10-15

Namespace: `Electron2D`; declaring owner: [`Node`](Node.md). Source: [Node.ProcessGroups.cs](../../src/Scene/Main/Node.ProcessGroups.cs).

```csharp
var worker = new Node { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
```

| Value | Number | Meaning |
| --- | --- | --- |
| Inherit | 0 | Inherits the nearest explicit ancestor or the default main group. |
| MainThread | 1 | Establishes a scene-owner group. |
| SubThread | 2 | Establishes a retained worker group joined at its stage barrier. |

The owning Node properties retain this exact value contract in packed and portable scenes. Unknown selectable values reject before mutation. See the [Node processing contract](Node.md#explicit-processing-groups) and [public conformance consumer](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ProcessGroups.cs); full inherited/physical worker access remains separately Partial.
