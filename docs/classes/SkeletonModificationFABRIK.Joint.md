# SkeletonModificationFABRIK.Joint

Last updated: 2026-10-07

- Declaration: `private struct Joint`
- Source: [SkeletonModificationFABRIK.cs](../../src/Scene/Resources/SkeletonModificationFABRIK.cs)
- Owner: [SkeletonModificationFABRIK](SkeletonModificationFABRIK.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

Copied fixed joint configuration record with relative path, initial index -1 and a private [SkeletonIKBinding](SkeletonIKBinding.md). Resizing retains the prefix and constructs new default records; resource duplication decodes independent records and fresh bindings. Magnet defaults zero and TargetRotation false. Magnets bias backward reaching; target orientation is active for the final slot. The root remains anchored and other slots retain orientation settings for structural editing.

The versioned private _joints schema encodes configuration fields only, with complete bounded validation before replacement. Weak caches are transient and never serialized. Records are protected by the owning resource gate; prepared execution does not copy them into allocating collections. SkeletonIKTests verifies copied/fresh-process state and actual solve behavior.
