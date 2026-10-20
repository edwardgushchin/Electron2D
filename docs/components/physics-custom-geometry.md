# Custom collision geometry

Last updated: 2026-10-20

`ShapeGeometry.Custom` describes a caller-defined collision format with copied canonical bytes, finite local bounds, exact unit-density mass/central inertia/center, copied diagnostic line pairs and a typed standalone collision algorithm. `CustomShape` retains that immutable value, emits revision changes on replacement and preserves its RID. `PhysicsServer.CustomShapeCreate` requires initialized data; ShapeGetData returns an independent wrapper, while ShapeSetData replaces the server-owned wrapper and rebuilds users.

The format identity is a stable schema/version name of at most 256 characters. Canonical payload is at most one MiB and remains opaque to the engine. Authors validate their format and use deterministic encoding. This is specific shape data, not a general parameter bag. Callback objects are retained only locally. Diagnostic segments represent drawing, not a collision approximation; the numerical world implementation interprets the exact format independently.

Standalone Shape collision dispatches a custom owner's typed algorithm in either argument order. Nested distinct pairs have separate retained scratch; identical/reversed recursive pairs reject. Counts and finite contact points validate before output copy, and reversal swaps contact ownership. Existing pair rules and engine units still apply. Built-in CPU/GPU attachment compilation rejects custom data before native shape destruction. Add/replace/enable and raw world transfer preflight preserve existing identity/configuration, including same-world private checkpoints after rejected changes. Disabled custom slots may remain authored without entering a built-in solver.

Mass authoring combines exact custom density samples with built-in filled geometry using the same normalized weighting and parallel-axis calculation. Detached geometry-dependent force preparation consumes the exact custom mass samples. Automatic server getters preserve ADR 0073 cached-resolution semantics. Registered numerical implementations receive full custom resources and retain control over their actual solver mass application.

Portable authoring compatibility hashes format, canonical bytes, bounds, mass and diagnostic geometry. It never serializes callbacks or solver handles. Incompatible shape edits reject before correction mutation; private checkpoints reject changed resource identities/revisions. The portable format and all existing built-in hashes remain unchanged when there is no custom geometry.

The separate public-only backend consumer exercises an analytic annulus with a real empty hole: standalone and selected point/ray/shape/motion queries, inner-wall momentum/contact data, Area observations, server mutation, exact mass, local/portable restore, error guards and warmed full frames. The bounded fixture supports annulus/circle and annulus/annulus numerical contacts; it is not a production generic solver. Diagnostic windows use both renderers and check actual pixels independently of physics. Missing file codecs/editor authoring for arbitrary custom formats, complete extension-operation acceptance, native/foreign platforms and final GPU performance remain open.

Built-in authoring preflight borrows the world against numerical writes as well as lifetime changes while invoking a procedural geometry supplier. Common scene/raw Body/Area input preparation acquires the same protection before reading cold geometry, including a registered implementation whose selected rebuild hook has not started. See [geometry input borrowing](physics-backends.md#geometry-input-borrowing). Its hook cannot release the body being validated, and the supplier failure leaves the current body/checkpoint intact. Detached transfer preflight also borrows the collider identity, so supplier callbacks cannot release or recursively transfer it. Disposed disabled resources keep their existing nonparticipating semantics.

[Geometric authoring preflight](physics-shapes.md) now protects all built-in setters,
CustomShape and procedural authors before private data changes. Registered/CPU/GPU
checks cover 16 paths, immutable data/RID preservation, ordinary callback/query
edits and warmed full frames. Arbitrary private subclass writes require the
protected authoring protocol; full numerical-extension acceptance remains open.

The registered annulus/circle consumer now evaluates resource sweeps independently.
For center segments A and B, collision truth requires their minimum separation
at most outerRadius+circleRadius and their maximum endpoint separation at least
innerRadius-circleRadius (clamped to zero). The continuous range between these
extrema covers the available independent center pairs. The annulus sweep is the
outer capsule minus the intersection of the endpoint inner disks; that lens is
empty once the annulus displacement reaches twice its inner radius. Contacts use
outer-capsule or inner-lens support boundaries and the circle's capsule boundary,
with first/second ownership reversed by the adapter.

The --custom profile verifies registered and CPU selection; ELECTRON2D_CUSTOM_GPU=1
adds explicitly selected GPU resource pairs. Each variant verifies twelve exact cases (equal/perpendicular/reversed
motion, retained/closed holes, outer/inner touching and misses), 512 cases seeded
43127 for contact-boundary validation, and 128 warmed standalone/selected cycles
at zero owner/all-thread managed bytes. Boundary checks allow .0001 scene units
for float projection; truth in the twelve cases is exact. This fixes the consumer's
synchronized-relative-path mismatch without changing the production API or solver.
Arbitrary custom geometry, complete numerical-extension
and final performance/native/platform acceptance remain open.


[Shape-pair operand borrowing](physics-shape-pairs.md#operand-borrowing-2026-10-14)
now protects both resources through standalone and selected callbacks, including
inherited copying/reset and owned RID replacement/retirement. Nested reads and
error cleanup retain their existing contract; full numerical/foreign/hardware
acceptance remains open.

## Hollow pair contacts

The public-only `--hollow-pairs` profile extends the same canonical annulus format
to two hollow operands. Independently swept resource truth uses the center-distance
range between the two segments, with upper bound `outerA+outerB` and lower bound
`max(0, innerA-outerB, innerB-outerA)`. Contact candidates include either retained
inner lens and both outer capsule boundaries. Registered, CPU and GPU spaces
exercise resource dispatch; built-in numerical attachment still rejects this
opaque format.

Registered shape overlap, cast, contact and rest queries select the active inner
or outer constraint and return points on the owning surfaces. A hole can resolve
penetration only when the other outer disk fits inside it; coincident equal rings
therefore use outer surfaces. Body motion retains disabled slot indices and the
existing initial-overlap cast policy. Numerical contacts use the same geometry
for impulses, penetration correction, contact anchors and debug pairs.

Twelve exact independent-region cases and 512 cases seeded 59173 check both
argument orders, center-distance truth and both boundary owners. Surface distances
allow .0001 scene units for float projection; identity/counts remain exact.
Registered serial and separate numerical modes also exercise both query-source
roles, both approach directions, inner/outer response, signed contact impulses
and logical scene/raw slot ownership. After 128 warmup cycles, each resource,
query/full-frame and dynamic/full-frame bracket measures 128 cycles at strict
zero owner/all-thread managed bytes. This is bounded format/provider evidence;
arbitrary algorithms, complete extension/network/platform acceptance and hardware
performance remain open.
