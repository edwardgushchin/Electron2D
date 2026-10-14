# UndoRedo.MergeMode

Last updated: 2026-10-15

**Namespace:** `Electron2D`. **Declaration:** `public enum UndoRedo.MergeMode`. **Source:** [UndoRedo.cs](../../src/Core/History/UndoRedo.cs). **Component:** [Undo history](../components/undo-history.md).

Controls matching recent action merging in [UndoRedo](UndoRedo.md).

| Value | Identity | Contract |
| --- | ---: | --- |
| `Disable` | 0 | Append an independent action. |
| `Ends` | 1 | First undo state, last do state and explicitly forced operations. |
| `All` | 2 | Append all operations; commit only newly added do suffix. |

Matching names and backward-undo policy must be within a strictly shorter-than-800ms monotonic interval. Undefined values reject before mutation. The domain belongs to action history and is distinct from resource copying, scene packing and animation blending.
