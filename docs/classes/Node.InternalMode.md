# Node.InternalMode

Last updated: 2026-09-30

**Declaration:** `public enum Node.InternalMode` · **Source:** [Node.InternalChildren.cs](../../src/Scene/Main/Node.InternalChildren.cs) · **Component:** [Scene hierarchy](../components/scene-hierarchy.md)

`AddChild(child, mode)` places implementation children before or after ordinary children. Internal children still enter the tree, process, receive notifications and input, and render. Ordinary `Children`, `ChildCount`, `GetChild`, `GetChildCount` and `GetChildren` omit them unless an explicit `includeInternal` argument is true. PackedScene captures ordinary children only.

| Value | Number | Effect |
| --- | ---: | --- |
| `Disabled` | 0 | Ordinary visible-to-query child. |
| `Front` | 1 | Internal child before ordinary children. |
| `Back` | 2 | Internal child after ordinary children. |

See [Node](Node.md) for indexing and movement rules and [ScrollContainer](ScrollContainer.md) for a current owner of internal children. [ScrollInternalNodeTests](../../tests/Electron2D.Tests/ScrollInternalNodeTests.cs) covers partition order, processing and packed-scene omission.
