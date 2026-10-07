# SkeletonModificationJiggle.Joint

Last updated: 2026-10-07

- Declaration: `private struct Joint`
- Source: [SkeletonModificationJiggle.cs](../../src/Scene/Resources/SkeletonModificationJiggle.cs)
- Owner: [SkeletonModificationJiggle](SkeletonModificationJiggle.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

Fixed copied per-joint configuration: relative Path, Index=-1 and private SkeletonIKBinding, Override=false, Stiffness=3, Mass=.75, Damping=.75, UseGravity=false and Gravity=(0,6). New slots copy current owner defaults; scalar default edits propagate to nonoverriding records and per-joint override shields authored values. Private versioned _joints storage encodes only configuration; copying decodes independent records/new weak bindings. Gate-protected setters validate before changes. Prepared execution reads value records without allocation.

SkeletonJiggleTests exercises actual used values, history/reset/error commitment and prepared allocation. Native/foreign/large-rig acceptance remains separately scoped.
