# Common scene body contract

Last updated: 2026-10-26

This audit covers the complete declared PhysicsBody API: six methods and the inherited InputPickable default override. The pinned reference is 4.7.2 at `ed1daf0bf001b61586d9930840f2f1394092c079`; none of these declarations is deprecated. The opposite direction uses the compiled API snapshot, PhysicsBody source and its class page. The protected constructor and overridden scene/disposal hooks project existing construction and inherited lifecycle roles. They introduce no independent gameplay capability.

| Capability | Public contract | Executable evidence |
| --- | --- | --- |
| Movement | MoveAndCollide applies safe global travel, or returns a caller-owned snapshot without moving when testOnly is true | Shared body-contract consumer and complete PhysicsMotionTests |
| Supplied-pose test | TestMove leaves the scene unchanged, replaces a supplied output on a completed hit or miss, and preserves it on rejected input | Shared body-contract consumer; query selection, recovery and one-way suites |
| Gravity | GetGravity observes the last completed resolved field snapshot; detached and StaticBody roles report zero | All five body roles in the shared consumer; complete Area-field suite |
| Exceptions | Typed body add/remove, deduplication and independent enumeration preserve the existing scene/raw/joint union | Shared consumer and complete registered/CPU/GPU exception suite |
| Picking default | Every body starts with InputPickable=false and its inherited stored descriptor reverts to false | All five concrete body branches in the shared consumer; existing pointer-delivery suite |

The consumer uses only public Electron2D API. StaticBody, AnimatableBody, CharacterBody, RigidBody and PhysicalBone share transformed-parent motion, actual circle contacts, arbitrary shape-owner objects, stable collider/RID identities, default arguments, rejected inputs and off-owner guards. Test-only operations preserve the exact sampled scene pose and world tick. Caller-owned collision results survive later queries and exception edits. General CollisionObject, Entity, CanvasItem, Node and ElectronObject declarations remain audited on their own rows.

Direct server motion profiles preserve their separate policy. A static profile publishes committed motion before the next public point query. Once its initial pose is established, a kinematic profile retains a later authored target until the next physical step. The consumer checks both boundaries on every concrete body branch. It does not turn a kinematic target into a teleport to satisfy an immediate-query assertion. CharacterBody's ordinary scene profile retains its own immediate-query preparation under ADR 0067.

First-touch checks use the accepted eight-refinement bracket: a displacement of eight units has a safe endpoint in `[4 - 3*8/512, 4]`, with 0.0001 floating slack. The analytic two-circle contact is at four units. Travel plus remainder retains the complete displacement within 0.001; scene/global conversions use 0.001 slack. Identity, counts, exception membership, untouched output and test-only pose/tick checks are exact. These bounds do not require bitwise agreement between numerical implementations.

The active allocation interval follows 128 warm full frames with 128 measured full frames, reused motion output and unobstructed zero-displacement calls. Owner and all-thread managed byte limits are zero. Allocating convenience results and exception arrays are exercised outside that interval. This small common-contract scene is not a massive-scene throughput or real-window FPS measurement.

The candidate comparison selected this common class because all seven member rows already had executable implementations while its type-level audit remained open. Live Object script binding needs an owner/script identity and lifetime contract; native DPI retains its independently reproduced FIFO presentation failure. Neither dependency is removed by completing this declared body family.

Receipts and source/payload identities are retained in `bin/physics-body-contract/2026-10-26/`. Complete ordinary-suite, network, native allocator, foreign-platform, physical-input, real-window and target performance acceptance remain separate from this type audit.

## Qualification

All twelve shared-body profiles pass: CLR and strict full-trim NativeAOT, each on CPU with unavailable graphics ICDs, NVIDIA GPU without fallback, and an independent registered provider, in owner and separate numerical modes. Their 1536 measured full frames retain strict 0/0 owner/all-thread managed bytes. Process maps identify NVIDIA on GPU profiles and reject software drivers; NativeAOT profiles reject CoreCLR. Frozen payload hashes remain unchanged, and 5442 consumed source files match before/after both build modes at source base `6330728c5370e4ba66127961ea8ef8fa336c5560` plus the new consumer. These are functional profiles; their cold process durations are not throughput measurements.

The initial fixture's 0.02-unit first-touch assertion was narrower than the accepted refinement bracket. Its canonical-pose comparison also ignored parent-composition roundoff. Both fixture errors were corrected before qualification; test-only calls now preserve the exact sampled pose. A later assertion incorrectly expected an established direct kinematic target to teleport before a step. The final consumer explicitly verifies its deferred publication, alongside the static-profile immediate query. No numerical tolerance, motion policy or runtime implementation was changed to obtain a pass.

The PhysicsBody class row becomes Implemented after this declared-family audit. Its seven existing member states are unchanged. Physics coverage advances from 1336/1369 (97.59%) to **1337/1369 (97.66%)**, with **32 open** (18 Partial, 11 Blocked, 3 Unimplemented) and 92 Excluded. This is confirmation of existing API plus an integrated regression, not a newly added runtime capability or completion of the full physics goal.
