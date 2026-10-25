# Direct-space query contract audit

Last updated: 2026-10-25

This audit covers the complete declared API of PhysicsDirectSpaceState and PhysicsDirectSpaceStateExtension, including inherited query dispatch, their parameter/result families and actual selected CPU/GPU and registered implementations. General ElectronObject declarations remain audited on their own coverage rows. Type-level completion here does not certify the complete physics goal, an arbitrary third-party solver, foreign platforms or target-GPU performance.

The pinned coverage input declares six base methods and seven extension methods at reference commit `ed1daf0bf001b61586d9930840f2f1394092c079`. Neither family declares a deprecated member. The compiled engine snapshot, base/extension class pages and owning ADRs 0063/0103 supply the opposite direction of the audit. Additional array/span overloads preserve one query contract; they do not create additional reference declarations.

| Query family | Public representation | Extension hook | Required behavior |
| --- | --- | --- | --- |
| Ray | Nullable PhysicsRayResult | IntersectRayCore | Nearest eligible logical shape, inside policy, finite global point/normal and sampled association |
| Point | PhysicsPointResult array or caller span | IntersectPointCore | Exact canvas/layer/body/Area/exclusion filters, unique RID/index order, cap and unchanged unused output |
| Shape overlap | PhysicsShapeResult array or caller span | IntersectShapeCore | Resource/RID geometry, pose/motion/margin, complete logical slots and stable original indices |
| Motion cast | Safe/unsafe float tuple | CastMotionCore | Ordered fractions in [0,1], first geometric contact, whole logical initial-overlap suppression, (1,1) on a miss |
| Contact pairs | Vector2 array or caller span | CollideShapeCore | Complete query/collider pairs, cap in pairs, finite points and unchanged odd/unused output |
| Rest | Nullable PhysicsRestInfo | GetRestInfoCore | Deepest eligible contact, normal, shape/association identity and actual collider point velocity |

The seventh extension operation, IsBodyExcludedFromQuery, exposes only the innermost synchronous exclusion set and restores the outer set after nested success or failure. Hook input values are sampled; output spans and scratch are borrowed. Validation completes before copying into caller storage. Named backend factories provide retained cached views; caller-created extensions retain their separate identity and never acquire world ownership. Disposal/recreation cannot revive an expired world or body attachment.

## Procedural input sampling

A reproduced CPU failure lost an overlap after a procedural query-shape supplier edited its caller's query parameters. CPU and GPU paths read some values before that supplier and other values afterward. CPU queries now capture pose, motion, margin and all filters before evaluating the query geometry, then retain the sampled motion alongside prepared scratch. GPU queries retain the equivalent local values before selecting their query-geometry lease. Nested cold suppliers finish before outer scratch is prepared.

Registered implementations receive the existing caller-owned parameter objects. They must sample the required values before calling their procedural geometry supplier. The public-only independent provider now follows that rule with a value input; no query-object copies, public aliases or new engine signatures are introduced. Caller-created direct-space extension hooks already receive sampled scalar/value inputs. Ordinary world preparation retains its existing ordering before query-geometry sampling.

The query-geometry fixture tests all eight mutable input channels individually and together: query shape, pose, motion, margin, collision mask, body policy, Area policy and exclusions. Its 108 cases combine nine edits with twelve selected/custom array/span/cast/rest routes. Each current invocation must retain its original physical result, while the next invocation observes the edited geometry and filters. It also preserves the original fifteen cold routes and ninety rejected world reentries. Twelve additional routes issue nonempty nested selected and caller-created shape/contact queries, then verify both successful outer results and supplier failure/retry with unchanged caller storage. The first GPU run reproduced extra outer hits; the independent provider reproduced too many contact pairs. GPU result lists and provider scratch are now cleared after procedural/nested preparation, immediately before publishing the current results. Complete moving-frame zero-allocation checks remain unchanged.

## Verification boundary

Qualification receipts are kept in `bin/physics-space-contract/2026-10-25/`. The final source identity is checked before and after build/publish; executable payloads are frozen and hashed. CPU profiles prohibit a graphics ICD. GPU profiles require NVIDIA process mappings and reject software devices. NativeAOT profiles reject CoreCLR mappings. No historical receipt or a successful diagnostic alone establishes a current pass.

The selected verification cohort covers the complete six-query contract; named factory dispatch; all represented shape-family pairs and special resource displacement; adaptive and large-motion brackets; explicit query inheritance, nesting, output validation and shape/RID borrowing. Each profile records its own actual warmup, workload and strict owner/all-thread allocation counters. Existing numerical bounds remain: circle first contact at 0.2 must lie inside an adaptive bracket no wider than 3/512 with 0.000001 float slack; the independent extension's analytic 0.375 cast/contact examples retain 0.0001 tolerance; identities, counts, filters and preserved output use exact checks. Other fixture-specific geometric bounds remain with those fixtures.

The final cohort passes all 56 frozen profiles: 38 CLR and 18 strict full-trim NativeAOT, with zero warmed owner/all-thread managed bytes. CLR includes query geometry (6), all-family special displacement (6), adaptive motion (6), long disk motion (6), hollow pairs (6), registered factories (2), the complete shared CPU/GPU query contract (2), and independent extension consumers (4). NativeAOT includes query geometry (6), all-family special displacement (6), registered factories (2) and independent extension consumers (4). Each six-profile family covers CPU/no-device, NVIDIA GPU and registered providers in ordinary and separate numerical modes. Each independent extension consumer executes all six queries, its full lifetime/output/shape-borrow controls and both numerical modes on CPU/GPU.

Build/publish source manifests match before and after all four final payloads. A later parallel change to ItemListInputTests is outside the consumed query fixtures; runtime and query-fixture source hashes remain unchanged. Scoped formatting, Release runtime, compiled API/coverage checks, wiki generator tests, generation and generation check pass before the local commit. No generated wiki output belongs to the engine commit.

Both declared type-family audits are complete; their class rows become Implemented. The thirteen method rows were already Implemented and retain their states. The physics census is now 1309/1369 (95.62%), with 46 Partial, 11 Blocked, 3 Unimplemented and 92 Excluded: sixty declarations remain open. Full ordinary-suite, cross-process network, native/driver allocation, real-window FPS and foreign-platform acceptance remain separate requirements of the physics goal.
