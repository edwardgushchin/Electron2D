# SkeletonModificationCCDIK.Joint

Last updated: 2026-10-07

- Declaration: `private struct Joint`
- Source: [SkeletonModificationCCDIK.cs](../../src/Scene/Resources/SkeletonModificationCCDIK.cs)
- Owner: [SkeletonModificationCCDIK](SkeletonModificationCCDIK.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

Copied fixed joint configuration record with relative path, initial index -1 and a private [SkeletonIKBinding](SkeletonIKBinding.md). Resizing retains the prefix and constructs new default records; resource duplication decodes independent records and fresh bindings. RotateFromJoint/EnableConstraint/Invert default false, Local defaults true, Minimum=0 and Maximum=Tau. These flags/angles drive the concrete CCD pass and exact stored coordinate policy.

The versioned private _joints schema encodes configuration fields only, with complete bounded validation before replacement. Weak caches are transient and never serialized. Records are protected by the owning resource gate; prepared execution does not copy them into allocating collections. SkeletonIKTests verifies copied/fresh-process state and actual solve behavior.
