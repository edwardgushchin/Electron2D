# OneWayContactData

Last updated: 2026-10-18

Immutable logical passing direction and contact margin in scene units supplied with a shape slot. It contains no native fixture handle.

See [registered backend contract](../components/physics-registered-adapter.md) for ownership, construction, verification and remaining acceptance.

Scene/raw slot projection caches unchanged resolved values. Rotated passing
direction or changed margin publishes a fresh immutable record; an implementation
retaining an earlier value observes no mutation. The [shape-edit profile](../components/physics-queries.md#failed-raw-shape-edits-and-warmed-reuse)
measures this authoring publication separately from strict zero-allocation physical steps.

Changed body one-way slot policies are captured during authoring and reused by
the next numerical preparation. The [one-way input profile](../components/physics-shapes.md#one-way-input-preparation)
checks edits without an intervening query, retained immutable values and strict
zero-allocation physical frames; changed authoring measured 32 bytes per
logical slot on Linux x64. Area sensing policy is unchanged.
