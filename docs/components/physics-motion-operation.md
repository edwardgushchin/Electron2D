# Body motion operation audit

Last updated: 2026-10-20

The selected `PhysicsServerBackend.TestBodyMotion` operation has a complete
operation-level audit under [ADR 0103](../decisions/physics-extensions.md#adr-0103)
and [ADR 0063](../decisions/physics.md#adr-0063). Its pinned counterpart is
`PhysicsServer2DExtension._body_test_motion`: a required const virtual operation
with a moving RID, supplied transform, displacement, margin, two policy flags,
borrowed result and boolean return. Neither the owning type nor member has
`deprecated` metadata. The C# projection adds explicit borrowed exclusion spans
and the validated typed result in place of the raw output pointer.

## Complete contract

| Contract | Implementation and exercised public evidence |
| --- | --- |
| Selected implementation and engine units | The static facade and `PhysicsBody.TestMove`/`MoveAndCollide` reach the world's actual CPU, resident GPU or registered owner. Queries preserve the live source pose and tick. `--motion-matrix` checks all 64 ordered built-in families, supplied rigid poses, local offsets and disabled logical gaps. |
| Complete displacement and first impact | Ordinary finite sources use translated regions; boundary/concave/ray exceptions retain their owning rules. `--adaptive-motion`, `--long-disk-motion`, `--oblique-disk-motion`, `--long-capsule-motion` and `--long-convex-motion` check near-start/middle/end, long rounded/flat cores, crossing versus finite-endpoint misses, and independent analytic roots. |
| Recovery, margins and final contact | `--motion-rest` checks every source family, zero/positive margins, initial correction, departure and final deepest contact. `--motion-selection` checks multiple logical source orders, current source isolation, all peer body modes, point velocity and a rotated custom center. A miss retains full requested displacement plus recovery and clears collision fields. |
| One-way and separation-ray policy | `--motion-oneway-pairs`, `--motion-oneway-entry` and `--directed-oneway` exercise rejected pair retention/reset/cap, source-resource aliases, accepted pieces, actual impact-side admission, tangent/receding corners and ray-source bypass. Source flags and local transformed directions remain effective per logical shape. |
| Moving platform timing | `--moving-oneway` checks all four peer roles, six finite source families, approach/recede/tangent/angular controls and the actual last-step duration. Private checkpoint and portable restoration preserve that duration; restoring a pre-first-step state removes the later allowance. Direct views, scene and raw callers observe the same restored timing. |
| Opaque custom geometry | `--custom-motion` and registered `--hollow-pairs` use actual annulus/circle and two-annulus geometry: empty holes, inner/outer entry, both source orders, initial-overlap recovery/flag controls, boundary points and complete selected steps. Built-in attachment of an unsupported opaque numerical format still rejects explicitly; resource-pair dispatch and registered numerical ownership are distinct. |
| Exclusions and const query scope | `--motion-exclusions` checks original borrowed RID/object-ID membership, changed input applying on the next call, scene/raw aliases, invalid/default IDs, exceptions, wrong-thread helpers, same-world recursion rejection and independent nested-world scopes. Hook/result failures clear only the active scope. |
| Result construction and atomic publication | `PhysicsTestMotionResult.SetMotion` and `SetCollision` provide complete typed output. Common validation checks requested receiver, live same-world enabled logical slots, masks, exceptions, exclusions, finite geometry and ordered fractions, and samples object/slot identity. Forged alternate scene/raw receivers and malformed status reject before replacing optional caller output. |
| Borrowed result lifetime and failures | `--motion-result-lifetime` checks exact reusable storage, owner-only access/filling, consumer disposal refusal, escaped/wrong-thread access, hook errors, no-result calls, next-valid-call reuse and retirement despite combined Finish/scratch failures. Caller-owned snapshots retain their independent lifetime. |

The operation does not step a second numerical backend. CPU owns its native
candidate/recovery/sweep geometry; GPU executes the resident motion kernel;
registered geometry belongs to the selected public hook. Common identity and
publication checks do not substitute a CPU numerical implementation. The public
consumer uses only exported Electron2D capabilities.

## Numerical and allocation evidence

The family matrix retains the eight-refinement fraction bound of
`3/512 + .000002`, with `.0002` root-rounding allowance. Long-sweep fixtures use
independent double Minkowski/rounded-edge or strip/endpoint oracles and preserve
their existing per-profile bounds. Custom analytic roots use `.000002` fraction
and `.0001` surface-distance tolerances. The hollow initial-overlap fixture independently expects `.476544` units after four 40% corrections of the .5-unit overlap plus .05 margin minus .0025 slack; `.0001` units covers float rounding. Disabling RecoveryAsCollision must preserve that displacement while clearing the collision. Identity, source/collider slots, flags,
unchanged caller storage and allocation counters remain exact. Detailed bounds
and reasons remain in the named fixtures and linked
[query evidence](physics-queries.md).

The final cohort comprises 93 CLR and 89 strict NativeAOT profiles: fourteen
shared selectors across CPU/registered/physical GPU and serial/separate numerical
modes, three serial exclusion profiles, two registered custom-motion profiles
per runtime, and four CLR root moving-platform/history companions. Exclusion
profiles have no separate-thread CLI selector and are recorded as serial.
Custom numerical formats are exercised by their registered implementation.
Every fixture retains its own complete warmup/measurement bracket and strict zero
owner/all-thread managed-byte assertion; resource-only brackets are not counted as
physical frames. Root companions use actual moving scene/raw bodies and restore
history through the common world boundary.

Evidence: bin/physics-motion-operation/2026-10-20/. Current input manifests,
portable-PDB checks, per-process payload hashes, actual NVIDIA/software-driver
mapping observations, telemetry, kernel observations and accepted profiles are
recorded in validation.json. The earlier cohorts in that directory used a frozen
pre-mesh payload; stale-payload-audit.json excludes them from current acceptance.
Current builds preserve the intervening main mesh commits.

## Coverage and remaining acceptance

This closes the `_body_test_motion` operation row. The prior cumulative reason
mixed already implemented geometry, recovery and borrowing requirements with
whole-backend/network/performance acceptance. All operation requirements above
have an executing path and corresponding evidence; no absent capability is
excluded or replaced by a declaration-only test.

The owning implementation classes retain their wider Partial status. Complete
registered scene/dynamics/constraint/CCD/replay acceptance is still a separate
numerical family audit; one provider's bounded opaque geometry does not certify
every application algorithm. The overall inherited API, ordinary full suite,
network scenarios, native allocator totals, new mass/window-FPS performance,
foreign platforms and hardware reliability retain their recorded gates.
No architectural decision, runtime code or public signature changes. The public
custom-motion fixture additionally exercises the previously skipped initial-overlap
body queries through raw, scene-RID and TestMove routes; its measured 128-frame
interval now contains 971 public queries. The resulting physics census is 1304/1369 Implemented
(95.25%), with 65 open declarations: 51 Partial, 11 Blocked and 3 Unimplemented;
92 declarations remain Excluded.
