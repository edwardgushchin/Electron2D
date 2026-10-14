# FoldableGroup

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public class FoldableGroup : Resource`.

**Source:** [FoldableGroup.cs](../../src/Scene/Resources/FoldableGroup.cs). **Inherits:** [Resource](Resource.md).

Coordinates [FoldableContainer](FoldableContainer.md) expansion without owning its members. Weak registrations and reusable guarded snapshots tolerate removal/disposal/reassignment during callbacks. Assignment reconciles a container against the existing selected member without emitting group expansion. ResourceLocalToScene starts true; duplication carries configuration with no members, and separate scene instances get separate groups while preserving aliases within each instance.

| Complete signature | Contract |
| --- | --- |
| `public FoldableGroup()` | Scene-local; disallows folding every member. |
| `public bool AllowFoldingAll { get; set; }` | False initially; disabling with all folded expands a member. |
| `public FoldableContainer[] GetContainers()` | Caller-owned snapshot of borrowed live members; order unspecified. |
| `public FoldableContainer? GetExpandedContainer()` | Borrowed expanded member, or null if none. |
| `public event Action<FoldableContainer>? Expanded` | Peers are folded; source is not yet committed expanded. |

Attached members require their scene owner for group operations. No callbacks run under the membership lock. Failures propagate after mandatory fold commitment; later reentrant selection supersedes older work. Resource disposal clears membership/events without disposing containers. XML, managed/fresh-process/native tests and [coverage](../coverage/classes/FoldableGroup.md) describe current behavior, without other-platform or owner acceptance claims.
