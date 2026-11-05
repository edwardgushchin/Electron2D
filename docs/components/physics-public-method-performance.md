# Public physics method measurements

Last updated: 2026-11-05

The ordinary [consumer](../../tests/PhysicsQueryPerformance.Consumer/README.md)
times public Electron2D calls on Windows x64, Ryzen 7 5700X and RTX 3090 Ti with
NVIDIA 621.14 / Vulkan and self-contained .NET 10.0.14. It has no runtime friend
access, private engine reflection, backend dependency or internal profiler.
The complete scene and direct-space query pairs are in the
[full-path report](physics-current-performance.md#public-api-comparison-after-driver-recovery-2026-11-05).

## Scope and interpretation

The tables below contain **475 paired rows**: 455 declared public method overloads
and 20 direct-body property accessors. Nine direct-space overloads have separate
hit/miss tables at three populations, bringing the sampled non-accessor methods
to **464**. The preliminary 436-method inventory has 432 measured entries and the
four explicit open entries below; the additional methods cover immutable geometry,
checkpoints and portable snapshots. This is a timing inventory, not an API
implementation or whole-product acceptance percentage.
These four timing omissions are independent of the inherited implementation
audit's four shared Partial declarations; no implementation row changes state.

| Open public entry | Current measurement limit | Continuation |
| --- | --- | --- |
| `Shape.Draw` | Appends renderer commands; no rendered-diagnostic timing in this headless physics profile | Measure in the rendered diagnostic workflow with its rendering owner and completion boundary |
| `PhysicsServerManager.RegisterServer` | Process-wide extension registration; no successful custom factory registration benchmark | Measure registration with an executable public consumer implementation, separately from built-in solving |
| `PhysicsServerManager.SetDefaultServer` | Future-world selection; not timed by the explicit CPU/GPU world profiles | Measure process-wide selection independently from existing-world performance |
| `PhysicsDirectSpaceStateExtension.IsBodyExcludedFromQuery` | Helper for a caller-defined query implementation; no active extension-query context in these built-in fixtures | Measure with a public consumer extension and positive/negative active exclusion controls |

Only physical declarations are included. General inherited Object/Node/resource
methods stay in their owning subsystem. Constructors, protected implementation
hooks, other property/event accessors, failure paths and every possible input
variant are not claimed as covered. No private solver-stage times are part of
this comparison.

Each row reports unadjusted **microseconds per call**, CPU/GPU mean ratio, batch
p95, owner/process managed bytes per call, and a separate empty-loop baseline in
nanoseconds. A ratio above one favors the GPU-selected context for this sample.
Percentiles describe batch means, not individual-call tails. Baselines are not
subtracted. Small differences near loop/timer overhead do not establish a speedup.
This is one fresh process per profile/backend on an active desktop, without quiet
admission or independent-run confidence intervals. It does not qualify maximum
throughput, other platforms, NativeAOT or rendered window FPS.

Prepared delegates invoke the public methods directly; public metadata is read
before the clock. Each overload has a fresh world/scene. Ordinary cases warm 32
and measure 64 batches of 32 calls. `SpaceCreate` instead warms once and measures
eight single calls while a peer world/device remains live. Preparation, physical
checks, reporting and caller-owned cleanup are outside timing. Explicit releases
time live prepared resources. Allocation results describe managed allocations,
not retained/native/device memory; allocation-bearing factories and copied arrays
retain their ordinary contract.

Setters and requests measure host return and exclude later simulation. Getters
observe completed cached state. Node motion samples use a settled floor;
`MoveAndCollide` is test-only, while `TestMove` has reusable output. The raw server
motion/collision inputs require positive results. Direct-body contacts are real,
with details warmed before timing; server-owned bodies have no scene object
association. Skeleton commands repeat retained requests. Parameterless
`SpaceCreate()` selects CPU in both contexts. Standalone resource collisions,
immutable geometry and query/result helpers use shared host code; their context
ratios are not GPU kernel speedups. Snapshot capture/apply uses one prepared local
world and reusable buffers, without transport or separate-process networking.
Do not sum these independent method samples into a frame estimate: use the
complete public `SceneTree.PhysicsFrame` measurement.

## Frozen evidence

The final cohort has ten method profiles and ten scene/query processes; all exit zero. Their source copies, binary/native
hashes, sampling details, per-row allocations, raw percentiles and external
whole-process host observations are retained in
`bin/physics-public-comparison/2026-11-05/qualified/`. All twenty use one frozen host;
the runtime DLL SHA-256 is
`15445206f48d1c057d2d71ca85bb8f4ca3bdbe10e24965d4763745c020420da7`.
The consumer DLL SHA-256 is
`b6afb1a606e80d57788d9eb039899d31fd20e2cbf3660eff5182d45da8f0d658`.
The native payload has one OpenSSL 4 provider pair; older OpenSSL 3 files are
outside its load path. This bounded native cohort does not qualify the whole
current release bundle. The [driver report](physics-current-performance.md#hardware-and-installation)
retains the active-driver checks and unresolved late installer registry error.

<!-- Measured tables follow; values come from the frozen JSON cohort above. -->

## PhysicsServer

| Public member | Mean CPU / GPU, us | CPU/GPU | p95 CPU / GPU, us | Managed CPU / GPU, owner:process B/call | Empty loop CPU / GPU, ns |
| --- | ---: | ---: | ---: | ---: | ---: |
| `SpaceGetBackend(RID)` | 0.0187 / 0.0181 | 1.032 | 0.0187 / 0.0187 | 0:0 / 0:0 | 18.65 / 18.60 |
| `SpaceGetRequestedBackend(RID)` | 0.0172 / 0.0170 | 1.011 | 0.0187 / 0.0187 | 0:0 / 0:0 | 2.20 / 2.00 |
| `BodyGetMode(RID)` | 0.0671 / 0.0651 | 1.032 | 0.0688 / 0.0656 | 0:0 / 0:0 | 2.20 / 2.15 |
| `AreaGetGravityPoint(RID)` | 0.0639 / 0.0574 | 1.112 | 0.0844 / 0.0594 | 0:0 / 0:0 | 3.37 / 2.20 |
| `BodyCollideShape(RID, Int32, RID, Transform, Vector2, Span`1[Vector2], Int32 ByRef)` | 1.4372 / 1.3519 | 1.063 | 1.5344 / 1.3531 | 0:0 / 0:0 | 2.20 / 2.34 |
| `BodyGetCanSleep(RID)` | 0.0715 / 0.0810 | 0.883 | 0.0719 / 0.0875 | 0:0 / 0:0 | 2.10 / 2.64 |
| `BodyGetSleeping(RID)` | 0.1015 / 0.1062 | 0.955 | 0.1031 / 0.1062 | 0:0 / 0:0 | 2.34 / 2.29 |
| `BodyIsOmittingForceIntegration(RID)` | 0.0717 / 0.0678 | 1.058 | 0.0719 / 0.0688 | 0:0 / 0:0 | 2.20 / 2.20 |
| `BodyTestMotion(RID, PhysicsTestMotionParameters, PhysicsTestMotionResult)` | 1.7450 / 137.5186 | 0.013 | 2.1938 / 177.1813 | 0:0 / 0:0 | 2.29 / 2.83 |
| `JointIsDisabledCollisionsBetweenBodies(RID)` | 0.1133 / 0.1076 | 1.054 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.15 / 1.95 |
| `PinJointGetAngularLimitEnabled(RID)` | 0.1434 / 0.1091 | 1.314 | 0.1437 / 0.1094 | 0:0 / 0:0 | 2.64 / 2.29 |
| `PinJointGetMotorEnabled(RID)` | 0.1146 / 0.1090 | 1.052 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.05 / 1.95 |
| `ShapeCollide(RID, RID, Transform, Vector2, RID, Transform, Vector2, Span`1[Vector2], Int32 ByRef)` | 0.8185 / 0.8136 | 1.006 | 0.8219 / 0.8125 | 0:0 / 0:0 | 2.00 / 2.39 |
| `SpaceIsActive(RID)` | 0.0275 / 0.0257 | 1.068 | 0.0281 / 0.0281 | 0:0 / 0:0 | 2.00 / 2.20 |
| `SpaceIsFlushingQueries(RID)` | 0.0189 / 0.0186 | 1.016 | 0.0187 / 0.0187 | 0:0 / 0:0 | 2.15 / 2.15 |
| `BodyGetAngularDampMode(RID)` | 0.0699 / 0.0678 | 1.031 | 0.0719 / 0.0688 | 0:0 / 0:0 | 2.05 / 2.20 |
| `BodyGetLinearDampMode(RID)` | 0.0724 / 0.0843 | 0.859 | 0.0719 / 0.0688 | 0:0 / 0:0 | 2.05 / 1.95 |
| `BodyGetContinuousCollisionDetectionMode(RID)` | 0.0817 / 0.0629 | 1.300 | 0.0813 / 0.0625 | 0:0 / 0:0 | 2.59 / 2.15 |
| `SpaceCreateCheckpoint(RID)` | 5.5099 / 126.3416 | 0.044 | 5.3719 / 180.2094 | 20163.6:20163.6 / 35528:35528 | 2.25 / 2.69 |
| `BodyGetDirectState(RID)` | 0.1327 / 0.1322 | 1.004 | 0.1344 / 0.1313 | 0:0 / 0:0 | 2.34 / 2.29 |
| `SpaceGetDirectState(RID)` | 0.0556 / 0.0420 | 1.323 | 0.0563 / 0.0437 | 0:0 / 0:0 | 2.64 / 2.00 |
| `AreaCreate()` | 0.0536 / 0.0666 | 0.804 | 0.0594 / 0.0781 | 440:440 / 440:440 | 2.10 / 2.20 |
| `AreaGetShape(RID, Int32)` | 0.0512 / 0.0492 | 1.042 | 0.0531 / 0.0500 | 0:0 / 0:0 | 2.34 / 2.00 |
| `AreaGetSpace(RID)` | 0.0487 / 0.0548 | 0.889 | 0.0469 / 0.0563 | 0:0 / 0:0 | 2.29 / 2.64 |
| `BodyCreate()` | 0.0549 / 0.0610 | 0.900 | 0.0781 / 0.0719 | 376:376 / 376:376 | 2.44 / 2.15 |
| `BodyGetShape(RID, Int32)` | 0.0521 / 0.0502 | 1.039 | 0.0531 / 0.0500 | 0:0 / 0:0 | 2.29 / 2.34 |
| `BodyGetSpace(RID)` | 0.0474 / 0.0448 | 1.058 | 0.0469 / 0.0469 | 0:0 / 0:0 | 2.05 / 2.29 |
| `CapsuleShapeCreate()` | 0.1446 / 0.1583 | 0.913 | 0.2031 / 0.2125 | 536:536 / 536:536 | 2.05 / 2.15 |
| `CircleShapeCreate()` | 0.1286 / 0.1735 | 0.741 | 0.1437 / 0.2969 | 536:536 / 536:536 | 2.15 / 2.05 |
| `ConcavePolygonShapeCreate()` | 0.1542 / 0.1334 | 1.156 | 0.1750 / 0.1469 | 536:536 / 536:536 | 2.20 / 2.29 |
| `ConvexPolygonShapeCreate()` | 0.1284 / 0.1350 | 0.951 | 0.1406 / 0.1812 | 536:536 / 536:536 | 2.00 / 2.10 |
| `CustomShapeCreate(ShapeGeometry)` | 0.1348 / 0.1438 | 0.938 | 0.1531 / 0.1594 | 536:536 / 536:536 | 2.20 / 2.00 |
| `JointCreate()` | 0.0376 / 0.0382 | 0.986 | 0.0406 / 0.0406 | 224:224 / 224:224 | 2.25 / 2.20 |
| `RectangleShapeCreate()` | 0.1304 / 0.1405 | 0.928 | 0.1437 / 0.1625 | 536:536 / 536:536 | 2.10 / 2.05 |
| `SegmentShapeCreate()` | 0.1413 / 0.1298 | 1.088 | 0.1875 / 0.1406 | 544:544 / 544:544 | 2.34 / 2.10 |
| `SeparationRayShapeCreate()` | 0.1276 / 0.1321 | 0.966 | 0.1375 / 0.1469 | 536:536 / 536:536 | 1.90 / 2.20 |
| `SpaceCreate()` | 56.7500 / 66.7375 | 0.850 | 74.1000 / 121.4000 | 67530:67530 / 67530:67530 | 50.00 / 12.50 |
| `SpaceCreate(Backend, Boolean)` | 57.5000 / 1407.4000 | 0.041 | 79.2000 / 1460.0000 | 67530:67530 / 150966:150966 | 12.50 / 62.50 |
| `SpaceCreate(String, Boolean)` | 56.6000 / 1381.3000 | 0.041 | 73.1000 / 1478.5000 | 67506:67506 / 150942:150942 | 37.50 / 37.50 |
| `WorldBoundaryShapeCreate()` | 0.1399 / 0.1466 | 0.954 | 0.1906 / 0.1656 | 544:544 / 544:544 | 2.34 / 2.29 |
| `ShapeGetData(RID)` | 1.1292 / 1.1554 | 0.977 | 1.1594 / 1.2344 | 1768.12:1768.12 / 1768.12:1768.12 | 2.20 / 2.54 |
| `AreaGetShapeTransform(RID, Int32)` | 0.0466 / 0.0447 | 1.041 | 0.0469 / 0.0469 | 0:0 / 0:0 | 2.05 / 2.25 |
| `AreaGetTransform(RID)` | 0.0478 / 0.0458 | 1.043 | 0.0500 / 0.0469 | 0:0 / 0:0 | 2.20 / 2.15 |
| `BodyGetShapeTransform(RID, Int32)` | 0.0481 / 0.0456 | 1.055 | 0.0500 / 0.0469 | 0:0 / 0:0 | 2.00 / 2.20 |
| `BodyGetTransform(RID)` | 0.1305 / 0.0958 | 1.363 | 0.1500 / 0.0969 | 0:0 / 0:0 | 2.73 / 2.29 |
| `AreaGetGravityVector(RID)` | 0.0652 / 0.0719 | 0.906 | 0.0906 / 0.0719 | 0:0 / 0:0 | 2.25 / 2.73 |
| `BodyGetCenterOfMass(RID)` | 0.0908 / 0.0869 | 1.046 | 0.0906 / 0.0875 | 0:0 / 0:0 | 2.05 / 2.05 |
| `BodyGetConstantForce(RID)` | 0.0695 / 0.0675 | 1.030 | 0.0719 / 0.0688 | 0:0 / 0:0 | 2.29 / 2.15 |
| `BodyGetLinearVelocity(RID)` | 0.1154 / 0.0992 | 1.163 | 0.1156 / 0.1000 | 0:0 / 0:0 | 2.00 / 2.20 |
| `AreaGetPriority(RID)` | 0.0604 / 0.0575 | 1.050 | 0.0625 / 0.0594 | 0:0 / 0:0 | 2.05 / 2.39 |
| `AreaGetShapeCount(RID)` | 0.0453 / 0.0432 | 1.049 | 0.0469 / 0.0437 | 0:0 / 0:0 | 2.15 / 2.15 |
| `BodyGetMaxContactsReported(RID)` | 0.0709 / 0.0675 | 1.051 | 0.0719 / 0.0688 | 0:0 / 0:0 | 2.29 / 2.25 |
| `BodyGetShapeCount(RID)` | 0.0586 / 0.0442 | 1.327 | 0.0594 / 0.0469 | 0:0 / 0:0 | 2.69 / 2.25 |
| `GetProcessInfo(ProcessInfo)` | 0.0187 / 0.0185 | 1.011 | 0.0187 / 0.0187 | 0:0 / 0:0 | 2.25 / 1.95 |
| `SpaceGetSolverIterations(RID)` | 0.0278 / 0.0270 | 1.031 | 0.0281 / 0.0375 | 0:0 / 0:0 | 2.00 / 3.32 |
| `JointGetType(RID)` | 0.1130 / 0.1074 | 1.052 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.29 / 2.29 |
| `ShapeGetType(RID)` | 0.0359 / 0.0354 | 1.015 | 0.0375 / 0.0375 | 0:0 / 0:0 | 1.86 / 2.39 |
| `AreaGetAngularDamp(RID)` | 0.0602 / 0.0575 | 1.047 | 0.0625 / 0.0594 | 0:0 / 0:0 | 2.05 / 2.20 |
| `AreaGetGravity(RID)` | 0.0604 / 0.0572 | 1.056 | 0.0625 / 0.0594 | 0:0 / 0:0 | 1.95 / 2.29 |
| `AreaGetGravityPointUnitDistance(RID)` | 0.0724 / 0.0572 | 1.266 | 0.0625 / 0.0594 | 0:0 / 0:0 | 2.05 / 2.10 |
| `AreaGetLinearDamp(RID)` | 0.0600 / 0.0572 | 1.050 | 0.0625 / 0.0594 | 0:0 / 0:0 | 2.29 / 2.34 |
| `BodyGetAngularDamp(RID)` | 0.0696 / 0.0675 | 1.030 | 0.0719 / 0.0688 | 0:0 / 0:0 | 1.95 / 2.15 |
| `BodyGetAngularVelocity(RID)` | 0.1133 / 0.1276 | 0.888 | 0.1156 / 0.1906 | 0:0 / 0:0 | 2.15 / 2.25 |
| `BodyGetBounce(RID)` | 0.0864 / 0.0666 | 1.297 | 0.0875 / 0.0688 | 0:0 / 0:0 | 2.69 / 2.44 |
| `BodyGetCollisionPriority(RID)` | 0.0456 / 0.0431 | 1.059 | 0.0469 / 0.0437 | 0:0 / 0:0 | 1.95 / 2.10 |
| `BodyGetConstantTorque(RID)` | 0.0697 / 0.0672 | 1.036 | 0.0719 / 0.0688 | 0:0 / 0:0 | 2.29 / 2.15 |
| `BodyGetContactsReportedDepthThreshold(RID)` | 0.0650 / 0.0629 | 1.033 | 0.0656 / 0.0625 | 0:0 / 0:0 | 2.00 / 2.05 |
| `BodyGetFriction(RID)` | 0.0692 / 0.0667 | 1.038 | 0.0688 / 0.0688 | 0:0 / 0:0 | 2.05 / 2.10 |
| `BodyGetGravityScale(RID)` | 0.0702 / 0.0672 | 1.044 | 0.0719 / 0.0688 | 0:0 / 0:0 | 2.00 / 2.15 |
| `BodyGetInertia(RID)` | 0.0922 / 0.0878 | 1.050 | 0.0938 / 0.0875 | 0:0 / 0:0 | 2.15 / 2.49 |
| `BodyGetLinearDamp(RID)` | 0.0871 / 0.0787 | 1.107 | 0.0875 / 0.0844 | 0:0 / 0:0 | 2.69 / 2.15 |
| `BodyGetMass(RID)` | 0.0699 / 0.0671 | 1.041 | 0.0719 / 0.0688 | 0:0 / 0:0 | 2.25 / 2.15 |
| `DampedSpringJointGetDamping(RID)` | 0.1147 / 0.1089 | 1.054 | 0.1156 / 0.1094 | 0:0 / 0:0 | 1.90 / 2.25 |
| `DampedSpringJointGetRestLength(RID)` | 0.1349 / 0.1103 | 1.223 | 0.1437 / 0.1125 | 0:0 / 0:0 | 2.20 / 2.10 |
| `DampedSpringJointGetStiffness(RID)` | 0.1178 / 0.1086 | 1.084 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.29 / 2.15 |
| `JointGetBias(RID)` | 0.1130 / 0.1073 | 1.053 | 0.1125 / 0.1094 | 0:0 / 0:0 | 1.90 / 2.15 |
| `JointGetMaxBias(RID)` | 0.1133 / 0.1073 | 1.056 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.34 / 2.49 |
| `JointGetMaxForce(RID)` | 0.1132 / 0.1075 | 1.053 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.20 / 2.05 |
| `PinJointGetAngularLimitLower(RID)` | 0.1144 / 0.1085 | 1.054 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.00 / 2.05 |
| `PinJointGetAngularLimitUpper(RID)` | 0.1147 / 0.1087 | 1.055 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.29 / 2.15 |
| `PinJointGetMotorMaxTorque(RID)` | 0.1146 / 0.1088 | 1.054 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.29 / 2.25 |
| `PinJointGetMotorTargetVelocity(RID)` | 0.1144 / 0.1087 | 1.052 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.34 / 2.49 |
| `PinJointGetSoftness(RID)` | 0.1144 / 0.1088 | 1.051 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.10 / 2.10 |
| `SpaceGetBodyAngularVelocitySleepThreshold(RID)` | 0.0280 / 0.0261 | 1.071 | 0.0281 / 0.0281 | 0:0 / 0:0 | 2.10 / 2.29 |
| `SpaceGetBodyLinearVelocitySleepThreshold(RID)` | 0.0279 / 0.0331 | 0.844 | 0.0281 / 0.0344 | 0:0 / 0:0 | 2.20 / 2.59 |
| `SpaceGetBodyTimeToSleep(RID)` | 0.0279 / 0.0264 | 1.055 | 0.0281 / 0.0281 | 0:0 / 0:0 | 2.10 / 2.44 |
| `SpaceGetConstraintDefaultBias(RID)` | 0.0279 / 0.0265 | 1.054 | 0.0281 / 0.0281 | 0:0 / 0:0 | 2.05 / 2.20 |
| `SpaceGetContactDefaultBias(RID)` | 0.0280 / 0.0262 | 1.069 | 0.0281 / 0.0281 | 0:0 / 0:0 | 1.95 / 2.20 |
| `SpaceGetContactMaxAllowedPenetration(RID)` | 0.0279 / 0.0261 | 1.069 | 0.0281 / 0.0281 | 0:0 / 0:0 | 2.00 / 2.10 |
| `SpaceGetContactMaxSeparation(RID)` | 0.0279 / 0.0261 | 1.069 | 0.0281 / 0.0281 | 0:0 / 0:0 | 2.00 / 2.44 |
| `SpaceGetContactRecycleRadius(RID)` | 0.0278 / 0.0262 | 1.061 | 0.0281 / 0.0281 | 0:0 / 0:0 | 2.20 / 2.20 |
| `AreaGetAngularDampSpaceOverride(RID)` | 0.1803 / 0.0575 | 3.134 | 0.0750 / 0.0594 | 0:0 / 0:0 | 2.64 / 2.20 |
| `AreaGetGravitySpaceOverride(RID)` | 0.0796 / 0.0572 | 1.392 | 0.1000 / 0.0594 | 0:0 / 0:0 | 2.10 / 2.10 |
| `AreaGetLinearDampSpaceOverride(RID)` | 0.0603 / 0.0572 | 1.055 | 0.0625 / 0.0594 | 0:0 / 0:0 | 2.25 / 2.25 |
| `SpaceGetBackendFallbackReason(RID)` | 0.0188 / 0.0185 | 1.016 | 0.0187 / 0.0187 | 0:0 / 0:0 | 2.20 / 2.15 |
| `SpaceGetBackendName(RID)` | 0.0181 / 0.0176 | 1.028 | 0.0187 / 0.0187 | 0:0 / 0:0 | 2.29 / 2.25 |
| `SpaceGetRequestedBackendName(RID)` | 0.0180 / 0.0223 | 0.809 | 0.0187 / 0.0219 | 0:0 / 0:0 | 2.20 / 2.64 |
| `AreaGetCollisionLayer(RID)` | 0.0443 / 0.0416 | 1.065 | 0.0469 / 0.0437 | 0:0 / 0:0 | 2.10 / 2.20 |
| `AreaGetCollisionMask(RID)` | 0.0440 / 0.0417 | 1.055 | 0.0437 / 0.0437 | 0:0 / 0:0 | 2.20 / 2.05 |
| `BodyGetCollisionLayer(RID)` | 0.0454 / 0.0433 | 1.050 | 0.0469 / 0.0437 | 0:0 / 0:0 | 2.05 / 2.05 |
| `BodyGetCollisionMask(RID)` | 0.0455 / 0.0432 | 1.052 | 0.0469 / 0.0437 | 0:0 / 0:0 | 2.29 / 2.20 |
| `AreaGetCanvasInstanceID(RID)` | 0.0443 / 0.0421 | 1.052 | 0.0469 / 0.0437 | 0:0 / 0:0 | 2.00 / 2.10 |
| `AreaGetObjectInstanceID(RID)` | 0.0468 / 0.0430 | 1.089 | 0.0625 / 0.0437 | 0:0 / 0:0 | 2.00 / 2.10 |
| `BodyGetCanvasInstanceID(RID)` | 0.0452 / 0.0431 | 1.050 | 0.0469 / 0.0437 | 0:0 / 0:0 | 2.05 / 2.15 |
| `BodyGetObjectInstanceID(RID)` | 0.0465 / 0.0441 | 1.054 | 0.0469 / 0.0437 | 0:0 / 0:0 | 2.25 / 2.15 |
| `SpaceGetTick(RID)` | 0.0275 / 0.0257 | 1.068 | 0.0281 / 0.0281 | 0:0 / 0:0 | 1.90 / 2.20 |
| `AreaAddShape(RID, RID, Nullable`1[Transform], Boolean)` | 0.8410 / 0.6434 | 1.307 | 0.9469 / 0.9375 | 2016:2016 / 1168:1168 | 2.15 / 2.20 |
| `AreaAttachCanvasInstanceID(RID, UInt64)` | 0.0559 / 0.0662 | 0.844 | 0.0531 / 0.0688 | 0:0 / 0:0 | 2.34 / 2.10 |
| `AreaAttachObject(RID, ElectronObject)` | 0.0952 / 0.1061 | 0.898 | 0.0969 / 0.1062 | 0:0 / 0:0 | 2.00 / 2.15 |
| `AreaClearShapes(RID)` | 0.2737 / 0.1806 | 1.516 | 0.2844 / 0.1938 | 536:536 / 536:536 | 2.44 / 2.29 |
| `AreaRemoveShape(RID, Int32)` | 0.2273 / 0.1312 | 1.732 | 0.2313 / 0.1344 | 0:0 / 0:0 | 2.49 / 2.25 |
| `AreaSetAngularDamp(RID, Single)` | 0.1936 / 0.1799 | 1.076 | 0.1906 / 0.1812 | 0:0 / 0:0 | 2.34 / 2.20 |
| `AreaSetAngularDampSpaceOverride(RID, SpaceOverride)` | 0.1887 / 0.1798 | 1.049 | 0.1844 / 0.1812 | 0:0 / 0:0 | 2.10 / 2.20 |
| `AreaSetAreaMonitorCallback(RID, Action`5[PhysicsServer+AreaBodyStatus,RID,UInt64,Int32,Int32])` | 0.2877 / 0.2271 | 1.267 | 0.2875 / 0.2250 | 0:0 / 0:0 | 2.73 / 2.15 |
| `AreaSetCollisionLayer(RID, UInt32)` | 0.2708 / 0.1979 | 1.368 | 0.3688 / 0.2000 | 0:0 / 0:0 | 2.34 / 2.29 |
| `AreaSetCollisionMask(RID, UInt32)` | 0.2528 / 0.2483 | 1.018 | 0.2531 / 0.2500 | 0:0 / 0:0 | 2.15 / 2.69 |
| `AreaSetGravity(RID, Single)` | 0.1838 / 0.1798 | 1.022 | 0.1844 / 0.1812 | 0:0 / 0:0 | 2.05 / 2.15 |
| `AreaSetGravityPoint(RID, Boolean)` | 0.1814 / 0.1773 | 1.023 | 0.1812 / 0.1781 | 0:0 / 0:0 | 2.15 / 2.05 |
| `AreaSetGravityPointUnitDistance(RID, Single)` | 0.1835 / 0.1796 | 1.022 | 0.1844 / 0.1812 | 0:0 / 0:0 | 2.00 / 2.20 |
| `AreaSetGravitySpaceOverride(RID, SpaceOverride)` | 0.1874 / 0.1841 | 1.018 | 0.2094 / 0.2250 | 0:0 / 0:0 | 2.34 / 2.20 |
| `AreaSetGravityVector(RID, Vector2)` | 0.1854 / 0.1901 | 0.975 | 0.1875 / 0.1812 | 0:0 / 0:0 | 2.29 / 2.20 |
| `AreaSetLinearDamp(RID, Single)` | 0.2348 / 0.1798 | 1.306 | 0.2344 / 0.1812 | 0:0 / 0:0 | 2.64 / 2.10 |
| `AreaSetLinearDampSpaceOverride(RID, SpaceOverride)` | 0.1854 / 0.1798 | 1.031 | 0.1844 / 0.1812 | 0:0 / 0:0 | 2.00 / 2.29 |
| `AreaSetMonitorCallback(RID, Action`5[PhysicsServer+AreaBodyStatus,RID,UInt64,Int32,Int32])` | 0.2300 / 0.2339 | 0.983 | 0.2313 / 0.3406 | 0:0 / 0:0 | 2.20 / 2.29 |
| `AreaSetMonitorable(RID, Boolean)` | 0.1688 / 0.1632 | 1.034 | 0.1688 / 0.1625 | 0:0 / 0:0 | 1.95 / 2.00 |
| `AreaSetPriority(RID, Int32)` | 0.1817 / 0.1774 | 1.024 | 0.1812 / 0.1781 | 0:0 / 0:0 | 2.20 / 2.10 |
| `AreaSetShape(RID, Int32, RID)` | 0.5197 / 0.4735 | 1.098 | 0.5219 / 0.4750 | 0:0 / 0:0 | 2.05 / 2.25 |
| `AreaSetShapeDisabled(RID, Int32, Boolean)` | 0.3318 / 0.2718 | 1.221 | 0.3312 / 0.2719 | 0:0 / 0:0 | 2.05 / 2.25 |
| `AreaSetShapeTransform(RID, Int32, Transform)` | 0.5306 / 0.4713 | 1.126 | 0.5312 / 0.4719 | 0:0 / 0:0 | 2.20 / 2.10 |
| `AreaSetSpace(RID, RID)` | 0.5796 / 0.5739 | 1.010 | 0.6094 / 0.7406 | 940:940 / 544.359:544.359 | 2.10 / 2.20 |
| `AreaSetTransform(RID, Transform)` | 0.2598 / 0.2388 | 1.088 | 0.2625 / 0.2406 | 0:0 / 0:0 | 2.00 / 2.05 |
| `BodyAddCollisionException(RID, RID)` | 0.2737 / 0.2497 | 1.096 | 0.2781 / 0.2531 | 0:0 / 0:0 | 2.10 / 2.15 |
| `BodyAddConstantCentralForce(RID, Vector2)` | 0.1586 / 0.2412 | 0.658 | 0.1594 / 0.2625 | 0:0 / 0:0 | 2.15 / 2.25 |
| `BodyAddConstantForce(RID, Vector2, Vector2)` | 0.2054 / 297.2740 | 0.001 | 0.2062 / 351.2406 | 0:0 / 0:0 | 2.34 / 2.78 |
| `BodyAddConstantTorque(RID, Single)` | 0.1882 / 0.2604 | 0.723 | 0.1906 / 0.2625 | 0:0 / 0:0 | 2.59 / 2.64 |
| `BodyAddShape(RID, RID, Nullable`1[Transform], Boolean)` | 1.2257 / 0.9468 | 1.295 | 1.3438 / 1.2063 | 1960:1960 / 1048:1048 | 2.10 / 2.25 |
| `BodyApplyCentralForce(RID, Vector2)` | 0.1700 / 0.1656 | 1.026 | 0.1969 / 0.1656 | 0:0 / 0:0 | 2.10 / 2.34 |
| `BodyApplyCentralImpulse(RID, Vector2)` | 0.3488 / 298.2393 | 0.001 | 0.4000 / 378.5688 | 0:0 / 0:0 | 2.05 / 2.69 |
| `BodyApplyForce(RID, Vector2, Vector2)` | 0.1969 / 292.6774 | 0.001 | 0.1969 / 348.5125 | 0:0 / 0:0 | 2.25 / 2.73 |
| `BodyApplyImpulse(RID, Vector2, Vector2)` | 0.3558 / 405.3917 | 0.001 | 0.3563 / 478.0969 | 0:0 / 0:0 | 2.44 / 2.39 |
| `BodyApplyTorque(RID, Single)` | 0.1769 / 0.1653 | 1.070 | 0.1531 / 0.1656 | 0:0 / 0:0 | 2.05 / 2.10 |
| `BodyApplyTorqueImpulse(RID, Single)` | 0.3342 / 303.4199 | 0.001 | 0.4594 / 364.7094 | 0:0 / 0:0 | 2.15 / 2.44 |
| `BodyAttachCanvasInstanceID(RID, UInt64)` | 0.0531 / 0.0677 | 0.784 | 0.0531 / 0.0688 | 0:0 / 0:0 | 2.15 / 2.25 |
| `BodyAttachObject(RID, ElectronObject)` | 0.0962 / 0.1249 | 0.770 | 0.0969 / 0.1375 | 0:0 / 0:0 | 2.25 / 2.73 |
| `BodyClearShapes(RID)` | 0.5085 / 0.3884 | 1.309 | 0.6531 / 0.4062 | 536:536 / 536:536 | 2.29 / 2.25 |
| `BodyGetTransform(RID, ReadOnlySpan`1[RID], Span`1[Transform])` | 0.2872 / 98.7210 | 0.003 | 0.2875 / 149.9406 | 0:0 / 0:0 | 2.05 / 2.78 |
| `BodyRemoveCollisionException(RID, RID)` | 0.2732 / 0.2566 | 1.065 | 0.2750 / 0.2594 | 0:0 / 0:0 | 2.20 / 2.20 |
| `BodyRemoveShape(RID, Int32)` | 0.3917 / 0.3516 | 1.114 | 0.4844 / 0.4875 | 0:0 / 0:0 | 2.29 / 2.20 |
| `BodyResetMassProperties(RID)` | 0.3067 / 0.2040 | 1.504 | 0.3094 / 0.2062 | 0:0 / 0:0 | 2.34 / 2.10 |
| `BodySetAngularDamp(RID, Single)` | 0.1132 / 0.1091 | 1.038 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.29 / 2.29 |
| `BodySetAngularDampMode(RID, DampMode)` | 0.1143 / 0.1142 | 1.001 | 0.1156 / 0.1562 | 0:0 / 0:0 | 2.25 / 2.20 |
| `BodySetAngularVelocity(RID, Single)` | 0.1656 / 0.1475 | 1.123 | 0.1719 / 0.1469 | 0:0 / 0:0 | 2.20 / 2.10 |
| `BodySetAxisVelocity(RID, Vector2)` | 0.1852 / 299.7214 | 0.001 | 0.1875 / 354.0437 | 0:0 / 0:0 | 2.34 / 2.73 |
| `BodySetBounce(RID, Single)` | 0.5472 / 0.7754 | 0.706 | 0.5500 / 0.7812 | 0:0 / 0:0 | 2.29 / 2.29 |
| `BodySetCanSleep(RID, Boolean)` | 0.1250 / 0.1554 | 0.805 | 0.1250 / 0.1562 | 0:0 / 0:0 | 2.29 / 2.29 |
| `BodySetCenterOfMass(RID, Vector2)` | 0.3121 / 0.4387 | 0.711 | 0.3125 / 0.4656 | 0:0 / 0:0 | 2.20 / 2.29 |
| `BodySetCollisionLayer(RID, UInt32)` | 0.2073 / 0.1554 | 1.334 | 0.2094 / 0.1562 | 0:0 / 0:0 | 1.90 / 2.10 |
| `BodySetCollisionMask(RID, UInt32)` | 0.2086 / 0.1555 | 1.342 | 0.2094 / 0.1562 | 0:0 / 0:0 | 2.15 / 2.15 |
| `BodySetCollisionPriority(RID, Single)` | 0.1100 / 0.0636 | 1.730 | 0.0531 / 0.0656 | 0:0 / 0:0 | 1.95 / 2.29 |
| `BodySetConstantForce(RID, Vector2)` | 0.1279 / 0.1333 | 0.959 | 0.1281 / 0.1344 | 0:0 / 0:0 | 2.39 / 2.44 |
| `BodySetConstantTorque(RID, Single)` | 0.1454 / 0.1595 | 0.912 | 0.1469 / 0.1594 | 0:0 / 0:0 | 2.05 / 2.00 |
| `BodySetContactsReportedDepthThreshold(RID, Single)` | 0.0880 / 0.0854 | 1.030 | 0.0875 / 0.0875 | 0:0 / 0:0 | 2.29 / 2.29 |
| `BodySetContinuousCollisionDetectionMode(RID, CCDMode)` | 0.1970 / 0.1818 | 1.084 | 0.2219 / 0.1812 | 0:0 / 0:0 | 2.15 / 2.15 |
| `BodySetForceIntegrationCallback(RID, Action`1[PhysicsDirectBodyState])` | 0.0886 / 0.0859 | 1.031 | 0.0906 / 0.0875 | 0:0 / 0:0 | 1.90 / 2.10 |
| `BodySetForceIntegrationCallback[T](RID, Action`2[PhysicsDirectBodyState,T], T)` | 0.0985 / 0.1000 | 0.985 | 0.1031 / 0.1094 | 96:96 / 96:96 | 2.29 / 2.34 |
| `BodySetFriction(RID, Single)` | 0.6898 / 0.8281 | 0.833 | 0.9563 / 0.8375 | 0:0 / 0:0 | 2.15 / 0.68 |
| `BodySetGravityScale(RID, Single)` | 0.1136 / 0.1094 | 1.038 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.34 / 2.29 |
| `BodySetInertia(RID, Single)` | 0.3493 / 0.5097 | 0.685 | 0.3656 / 0.5344 | 0:0 / 0:0 | 2.20 / 1.95 |
| `BodySetLinearDamp(RID, Single)` | 0.1265 / 0.1092 | 1.158 | 0.1406 / 0.1094 | 0:0 / 0:0 | 2.29 / 1.95 |
| `BodySetLinearDampMode(RID, DampMode)` | 0.1133 / 0.1098 | 1.032 | 0.1156 / 0.1094 | 0:0 / 0:0 | 2.25 / 1.95 |
| `BodySetLinearVelocity(RID, Vector2)` | 0.1441 / 0.1497 | 0.963 | 0.1406 / 0.1500 | 0:0 / 0:0 | 1.95 / 2.29 |
| `BodySetMass(RID, Single)` | 0.3072 / 0.4128 | 0.744 | 0.3094 / 0.4406 | 0:0 / 0:0 | 2.29 / 2.20 |
| `BodySetMaxContactsReported(RID, Int32)` | 0.0992 / 0.1057 | 0.939 | 0.1000 / 0.1062 | 0:0 / 0:0 | 2.05 / 2.05 |
| `BodySetMode(RID, BodyMode)` | 1.0561 / 153.0382 | 0.007 | 1.2000 / 199.1906 | 0:0 / 0:0 | 2.29 / 2.64 |
| `BodySetOmitForceIntegration(RID, Boolean)` | 0.0988 / 0.0885 | 1.116 | 0.0969 / 0.0906 | 0:0 / 0:0 | 2.29 / 2.15 |
| `BodySetShape(RID, Int32, RID)` | 0.9440 / 0.8350 | 1.131 | 1.1125 / 0.8375 | 0:0 / 0:0 | 2.20 / 2.29 |
| `BodySetShapeAsOneWayCollision(RID, Int32, Boolean, Single, Nullable`1[Vector2])` | 0.9035 / 0.8204 | 1.101 | 1.2719 / 0.8219 | 0:0 / 0:0 | 1.90 / 2.29 |
| `BodySetShapeDisabled(RID, Int32, Boolean)` | 0.5859 / 0.5464 | 1.072 | 0.5875 / 0.5500 | 0:0 / 0:0 | 2.00 / 2.10 |
| `BodySetShapeTransform(RID, Int32, Transform)` | 1.0900 / 0.8931 | 1.220 | 1.3656 / 1.0375 | 0:0 / 0:0 | 2.98 / 2.29 |
| `BodySetSleeping(RID, Boolean)` | 0.3024 / 100.2210 | 0.003 | 0.3031 / 134.4000 | 0:0 / 0:0 | 2.29 / 2.73 |
| `BodySetSpace(RID, RID)` | 2.5755 / 656.5294 | 0.004 | 2.7250 / 738.0969 | 1144:1144 / 764.359:764.359 | 2.25 / 2.88 |
| `BodySetStateSyncCallback(RID, Action`1[PhysicsDirectBodyState])` | 0.0885 / 0.0862 | 1.027 | 0.0906 / 0.0875 | 0:0 / 0:0 | 2.00 / 2.10 |
| `BodySetTransform(RID, Transform)` | 0.2755 / 0.2994 | 0.920 | 0.2625 / 0.3000 | 0:0 / 0:0 | 2.25 / 2.64 |
| `DampedSpringJointSetDamping(RID, Single)` | 0.3275 / 0.4750 | 0.689 | 0.3250 / 0.4656 | 0:0 / 0:0 | 2.20 / 2.25 |
| `DampedSpringJointSetRestLength(RID, Single)` | 0.3234 / 0.4643 | 0.697 | 0.3250 / 0.4656 | 0:0 / 0:0 | 2.05 / 2.15 |
| `DampedSpringJointSetStiffness(RID, Single)` | 0.3257 / 0.4662 | 0.699 | 0.3250 / 0.4688 | 0:0 / 0:0 | 2.29 / 2.15 |
| `FreeRID(RID)` | 0.7325 / 0.7819 | 0.937 | 0.8031 / 0.9656 | 848:848 / 848:848 | 2.29 / 2.10 |
| `JointClear(RID)` | 0.7960 / 0.8778 | 0.907 | 0.7812 / 1.0094 | 0:0 / 0:0 | 2.34 / 2.88 |
| `JointDisableCollisionsBetweenBodies(RID, Boolean)` | 0.8000 / 0.9791 | 0.817 | 0.7969 / 1.1406 | 0:0 / 0:0 | 2.20 / 2.29 |
| `JointMakeDampedSpring(RID, Vector2, Vector2, RID, RID)` | 2.0873 / 820.5712 | 0.003 | 2.3594 / 905.6219 | 120:120 / 184:184 | 2.10 / 2.34 |
| `JointMakeGroove(RID, Vector2, Vector2, Vector2, RID, RID)` | 2.1131 / 816.1877 | 0.003 | 2.2812 / 889.9406 | 120:120 / 184:184 | 2.39 / 2.64 |
| `JointMakePin(RID, Vector2, RID, RID)` | 2.0694 / 822.0545 | 0.003 | 2.1187 / 948.6844 | 120:120 / 184:184 | 2.39 / 2.78 |
| `JointSetBias(RID, Single)` | 0.3362 / 0.4625 | 0.727 | 0.3344 / 0.4625 | 0:0 / 0:0 | 2.34 / 2.00 |
| `JointSetMaxBias(RID, Single)` | 0.3896 / 0.4640 | 0.840 | 0.5469 / 0.4656 | 0:0 / 0:0 | 2.15 / 2.00 |
| `JointSetMaxForce(RID, Single)` | 0.3387 / 0.5446 | 0.622 | 0.3344 / 0.5844 | 0:0 / 0:0 | 2.34 / 2.20 |
| `PinJointSetAngularLimitEnabled(RID, Boolean)` | 0.3472 / 0.4427 | 0.784 | 0.2938 / 0.4375 | 0:0 / 0:0 | 2.05 / 2.29 |
| `PinJointSetAngularLimitLower(RID, Single)` | 0.3025 / 0.2990 | 1.012 | 0.2906 / 0.3000 | 0:0 / 0:0 | 1.95 / 2.20 |
| `PinJointSetAngularLimitUpper(RID, Single)` | 0.2877 / 0.2997 | 0.960 | 0.2906 / 0.3000 | 0:0 / 0:0 | 2.15 / 1.95 |
| `PinJointSetMotorEnabled(RID, Boolean)` | 0.2861 / 0.5182 | 0.552 | 0.2875 / 0.5437 | 0:0 / 0:0 | 2.05 / 2.25 |
| `PinJointSetMotorMaxTorque(RID, Single)` | 0.2882 / 0.4425 | 0.651 | 0.2906 / 0.4437 | 0:0 / 0:0 | 2.20 / 2.25 |
| `PinJointSetMotorTargetVelocity(RID, Single)` | 0.2922 / 0.4392 | 0.665 | 0.2875 / 0.4406 | 0:0 / 0:0 | 2.15 / 2.00 |
| `PinJointSetSoftness(RID, Single)` | 0.3330 / 0.4688 | 0.710 | 0.3344 / 0.4688 | 0:0 / 0:0 | 2.25 / 2.00 |
| `SetActive(Boolean)` | 0.0045 / 0.0044 | 1.022 | 0.0063 / 0.0063 | 0:0 / 0:0 | 2.29 / 2.10 |
| `ShapeSetData(RID, Shape)` | 1.6322 / 1.3294 | 1.228 | 2.7781 / 1.3687 | 1920.12:1920.12 / 1920.12:1920.12 | 2.29 / 2.20 |
| `SpaceSetActive(RID, Boolean)` | 0.0469 / 0.0420 | 1.116 | 0.0469 / 0.0437 | 0:0 / 0:0 | 2.10 / 2.25 |
| `SpaceSetBodyAngularVelocitySleepThreshold(RID, Single)` | 0.0608 / 0.0663 | 0.917 | 0.0625 / 0.0688 | 0:0 / 0:0 | 2.34 / 1.95 |
| `SpaceSetBodyLinearVelocitySleepThreshold(RID, Single)` | 0.0644 / 0.0660 | 0.976 | 0.1031 / 0.0656 | 0:0 / 0:0 | 3.08 / 2.39 |
| `SpaceSetBodyTimeToSleep(RID, Single)` | 0.0609 / 0.0712 | 0.855 | 0.0625 / 0.1062 | 0:0 / 0:0 | 2.29 / 2.00 |
| `SpaceSetConstraintDefaultBias(RID, Single)` | 0.1684 / 0.4473 | 0.376 | 0.1688 / 0.4500 | 0:0 / 0:0 | 2.05 / 2.00 |
| `SpaceSetContactDefaultBias(RID, Single)` | 0.0833 / 0.0663 | 1.257 | 0.1094 / 0.0688 | 0:0 / 0:0 | 3.17 / 2.25 |
| `SpaceSetContactMaxAllowedPenetration(RID, Single)` | 0.0583 / 0.0666 | 0.877 | 0.0594 / 0.0688 | 0:0 / 0:0 | 2.20 / 2.05 |
| `SpaceSetContactMaxSeparation(RID, Single)` | 0.0585 / 0.0666 | 0.879 | 0.0594 / 0.0688 | 0:0 / 0:0 | 2.10 / 2.29 |
| `SpaceSetContactRecycleRadius(RID, Single)` | 0.0571 / 0.1127 | 0.507 | 0.0594 / 0.1281 | 0:0 / 0:0 | 2.29 / 3.91 |
| `SpaceSetSolverIterations(RID, Int32)` | 0.0477 / 0.0496 | 0.962 | 0.0500 / 0.0500 | 0:0 / 0:0 | 2.34 / 2.29 |
| `SpaceStep(RID, Double)` | 12.7300 / 4087.6284 | 0.003 | 14.7656 / 4289.3250 | 0:0 / 0:0 | 2.10 / 2.78 |

## PhysicsDirectBodyState

| Public member | Mean CPU / GPU, us | CPU/GPU | p95 CPU / GPU, us | Managed CPU / GPU, owner:process B/call | Empty loop CPU / GPU, ns |
| --- | ---: | ---: | ---: | ---: | ---: |
| `get_Sleeping()` | 0.0556 / 0.0735 | 0.757 | 0.0563 / 0.0750 | 0:0 / 0:0 | 20.51 / 19.38 |
| `GetContactColliderObject(Int32)` | 0.0585 / 0.0567 | 1.032 | 0.0594 / 0.0563 | 0:0 / 0:0 | 2.15 / 1.81 |
| `GetSpaceState()` | 0.0854 / 0.0818 | 1.044 | 0.0875 / 0.0844 | 0:0 / 0:0 | 2.00 / 2.29 |
| `GetContactCollider(Int32)` | 0.0478 / 0.0585 | 0.817 | 0.0500 / 0.0594 | 0:0 / 0:0 | 2.34 / 2.78 |
| `get_Transform()` | 0.0740 / 0.0509 | 1.453 | 0.0750 / 0.0531 | 0:0 / 0:0 | 2.10 / 2.00 |
| `GetConstantForce()` | 0.0492 / 0.0473 | 1.041 | 0.0500 / 0.0500 | 0:0 / 0:0 | 1.90 / 2.00 |
| `GetContactColliderPosition(Int32)` | 0.0513 / 0.0625 | 0.821 | 0.0531 / 0.0625 | 0:0 / 0:0 | 2.15 / 2.69 |
| `GetContactColliderVelocityAtPosition(Int32)` | 0.0642 / 0.0504 | 1.273 | 0.0656 / 0.0500 | 0:0 / 0:0 | 2.69 / 2.00 |
| `GetContactImpulse(Int32)` | 0.0514 / 0.0498 | 1.032 | 0.0531 / 0.0500 | 0:0 / 0:0 | 2.15 / 2.15 |
| `GetContactLocalNormal(Int32)` | 0.0513 / 0.0499 | 1.029 | 0.0531 / 0.0500 | 0:0 / 0:0 | 2.15 / 2.15 |
| `GetContactLocalPosition(Int32)` | 0.0511 / 0.0499 | 1.025 | 0.0531 / 0.0500 | 0:0 / 0:0 | 2.10 / 2.25 |
| `GetContactLocalVelocityAtPosition(Int32)` | 0.0516 / 0.0501 | 1.028 | 0.0531 / 0.0531 | 0:0 / 0:0 | 2.00 / 2.29 |
| `GetVelocityAtLocalPosition(Vector2)` | 0.0834 / 0.0870 | 0.959 | 0.0844 / 0.0875 | 0:0 / 0:0 | 2.29 / 1.90 |
| `get_CenterOfMass()` | 0.0875 / 0.0930 | 0.942 | 0.0875 / 0.0938 | 0:0 / 0:0 | 2.64 / 2.78 |
| `get_CenterOfMassLocal()` | 0.0633 / 0.0583 | 1.086 | 0.0750 / 0.0594 | 0:0 / 0:0 | 2.69 / 2.10 |
| `get_LinearVelocity()` | 0.0624 / 0.0481 | 1.295 | 0.0656 / 0.0500 | 0:0 / 0:0 | 2.00 / 2.10 |
| `get_TotalGravity()` | 0.0594 / 0.0458 | 1.299 | 0.0594 / 0.0469 | 0:0 / 0:0 | 2.69 / 1.90 |
| `GetContactColliderShape(Int32)` | 0.0467 / 0.0535 | 0.873 | 0.0469 / 0.0813 | 0:0 / 0:0 | 2.15 / 3.81 |
| `GetContactCount()` | 0.0459 / 0.0439 | 1.047 | 0.0469 / 0.0437 | 0:0 / 0:0 | 2.20 / 2.20 |
| `GetContactLocalShape(Int32)` | 0.0464 / 0.0450 | 1.031 | 0.0469 / 0.0469 | 0:0 / 0:0 | 1.95 / 2.20 |
| `GetConstantTorque()` | 0.0488 / 0.0472 | 1.034 | 0.0500 / 0.0469 | 0:0 / 0:0 | 2.05 / 2.20 |
| `get_AngularVelocity()` | 0.0602 / 0.0476 | 1.266 | 0.0625 / 0.0500 | 0:0 / 0:0 | 2.15 / 2.29 |
| `get_InverseInertia()` | 0.0625 / 0.0786 | 0.794 | 0.0625 / 0.0781 | 0:0 / 0:0 | 2.05 / 2.15 |
| `get_InverseMass()` | 0.0585 / 0.0686 | 0.854 | 0.0594 / 0.0688 | 0:0 / 0:0 | 1.95 / 2.10 |
| `get_Step()` | 0.0459 / 0.0444 | 1.035 | 0.0469 / 0.0469 | 0:0 / 0:0 | 2.10 / 2.10 |
| `get_TotalAngularDamp()` | 0.0487 / 0.0471 | 1.033 | 0.0500 / 0.0469 | 0:0 / 0:0 | 2.20 / 1.81 |
| `get_TotalLinearDamp()` | 0.0492 / 0.0472 | 1.042 | 0.0500 / 0.0469 | 0:0 / 0:0 | 2.00 / 2.20 |
| `GetContactColliderObject[T](Int32)` | 0.0498 / 0.0501 | 0.994 | 0.0500 / 0.0500 | 0:0 / 0:0 | 2.20 / 2.29 |
| `get_CollisionLayer()` | 0.0476 / 0.0460 | 1.034 | 0.0500 / 0.0469 | 0:0 / 0:0 | 2.05 / 2.10 |
| `get_CollisionMask()` | 0.0475 / 0.0457 | 1.040 | 0.0500 / 0.0469 | 0:0 / 0:0 | 2.10 / 2.15 |
| `GetContactColliderID(Int32)` | 0.0465 / 0.0466 | 0.997 | 0.0469 / 0.0469 | 0:0 / 0:0 | 2.15 / 2.59 |
| `AddConstantCentralForce(Vector2)` | 0.1605 / 0.1632 | 0.983 | 0.1594 / 0.1656 | 0:0 / 0:0 | 2.10 / 2.05 |
| `AddConstantForce(Vector2, Vector2)` | 0.2661 / 303.1586 | 0.001 | 0.2656 / 374.9875 | 0:0 / 0:0 | 2.05 / 2.25 |
| `AddConstantTorque(Single)` | 0.1613 / 0.2009 | 0.803 | 0.1562 / 0.2000 | 0:0 / 0:0 | 2.10 / 3.42 |
| `ApplyCentralForce(Vector2)` | 0.2003 / 0.2143 | 0.935 | 0.2000 / 0.2156 | 0:0 / 0:0 | 2.20 / 2.10 |
| `ApplyCentralImpulse(Vector2)` | 0.3977 / 294.1688 | 0.001 | 0.3719 / 328.7375 | 0:0 / 0:0 | 2.20 / 2.73 |
| `ApplyForce(Vector2, Vector2)` | 0.2448 / 296.0703 | 0.001 | 0.2406 / 344.4375 | 0:0 / 0:0 | 2.15 / 2.34 |
| `ApplyImpulse(Vector2, Vector2)` | 0.4137 / 392.0642 | 0.001 | 0.4094 / 442.6938 | 0:0 / 0:0 | 2.10 / 2.34 |
| `ApplyTorque(Single)` | 0.2034 / 0.2092 | 0.972 | 0.2313 / 0.2094 | 0:0 / 0:0 | 2.29 / 2.10 |
| `ApplyTorqueImpulse(Single)` | 0.3660 / 295.7671 | 0.001 | 0.3688 / 387.1906 | 0:0 / 0:0 | 2.25 / 2.73 |
| `IntegrateForces()` | 0.6299 / 302.8320 | 0.002 | 0.6375 / 352.6969 | 0:0 / 0:0 | 2.05 / 2.73 |
| `SetConstantForce(Vector2)` | 0.0635 / 0.0743 | 0.855 | 0.0656 / 0.0750 | 0:0 / 0:0 | 2.00 / 2.15 |
| `SetConstantTorque(Single)` | 0.0837 / 0.0719 | 1.165 | 0.1219 / 0.0719 | 0:0 / 0:0 | 2.69 / 2.10 |
| `set_AngularVelocity(Single)` | 0.0966 / 0.1082 | 0.893 | 0.1000 / 0.1094 | 0:0 / 0:0 | 2.20 / 2.15 |
| `set_CollisionLayer(UInt32)` | 0.1426 / 0.1133 | 1.259 | 0.1437 / 0.1156 | 0:0 / 0:0 | 1.95 / 2.29 |
| `set_CollisionMask(UInt32)` | 0.1449 / 0.1160 | 1.249 | 0.1469 / 0.1156 | 0:0 / 0:0 | 2.10 / 2.39 |
| `set_LinearVelocity(Vector2)` | 0.1301 / 0.1106 | 1.176 | 0.1313 / 0.1125 | 0:0 / 0:0 | 2.64 / 2.25 |
| `set_Sleeping(Boolean)` | 0.1973 / 105.1527 | 0.002 | 0.2000 / 155.8375 | 0:0 / 0:0 | 2.20 / 2.73 |
| `set_Transform(Transform)` | 0.2180 / 0.2138 | 1.020 | 0.2156 / 0.2156 | 0:0 / 0:0 | 2.20 / 2.20 |

## Physical nodes and collision snapshots

| Public member | Mean CPU / GPU, us | CPU/GPU | p95 CPU / GPU, us | Managed CPU / GPU, owner:process B/call | Empty loop CPU / GPU, ns |
| --- | ---: | ---: | ---: | ---: | ---: |
| `Area.HasOverlappingAreas()` | 0.0356 / 0.0273 | 1.306 | 0.0375 / 0.0281 | 0:0 / 0:0 | 18.70 / 18.85 |
| `Area.HasOverlappingBodies()` | 0.0194 / 0.0189 | 1.028 | 0.0250 / 0.0187 | 0:0 / 0:0 | 1.61 / 2.10 |
| `Area.OverlapsArea(Node)` | 0.0387 / 0.0303 | 1.279 | 0.0406 / 0.0312 | 0:0 / 0:0 | 3.03 / 2.25 |
| `Area.OverlapsBody(Node)` | 0.0306 / 0.0269 | 1.136 | 0.0312 / 0.0281 | 0:0 / 0:0 | 2.25 / 2.73 |
| `Area.GetOverlappingAreas()` | 0.1020 / 0.1042 | 0.978 | 0.1094 / 0.1187 | 120:120 / 120:120 | 1.51 / 2.64 |
| `Area.GetOverlappingBodies()` | 0.1882 / 0.1883 | 0.999 | 0.1938 / 0.1969 | 128:128 / 128:128 | 2.34 / 2.39 |
| `CharacterBody.GetLastSlideCollision(KinematicCollision)` | 0.0291 / 0.0291 | 1.000 | 0.0312 / 0.0312 | 0:0 / 0:0 | 3.17 / 2.34 |
| `CharacterBody.IsOnCeiling()` | 0.0081 / 0.0081 | 0.994 | 0.0094 / 0.0094 | 0:0 / 0:0 | 1.46 / 2.10 |
| `CharacterBody.IsOnCeilingOnly()` | 0.0081 / 0.0082 | 0.982 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.15 / 2.39 |
| `CharacterBody.IsOnFloor()` | 0.0084 / 0.0083 | 1.018 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.29 / 2.49 |
| `CharacterBody.IsOnFloorOnly()` | 0.0105 / 0.0083 | 1.271 | 0.0125 / 0.0094 | 0:0 / 0:0 | 2.73 / 2.39 |
| `CharacterBody.IsOnWall()` | 0.0077 / 0.0081 | 0.952 | 0.0094 / 0.0094 | 0:0 / 0:0 | 3.17 / 2.39 |
| `CharacterBody.IsOnWallOnly()` | 0.0087 / 0.0083 | 1.059 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.34 / 2.25 |
| `CharacterBody.MoveAndSlide()` | 3.9732 / 207.9830 | 0.019 | 4.1156 / 253.1500 | 0:0 / 0:0 | 2.00 / 3.32 |
| `CharacterBody.GetLastSlideCollision()` | 0.1266 / 0.1266 | 1.000 | 0.2188 / 0.2000 | 272:272 / 272:272 | 1.51 / 1.90 |
| `CharacterBody.GetSlideCollision(Int32)` | 0.1127 / 0.1187 | 0.950 | 0.1469 / 0.1562 | 272:272 / 272:272 | 3.17 / 2.59 |
| `CharacterBody.GetFloorNormal()` | 0.0075 / 0.0083 | 0.900 | 0.0094 / 0.0094 | 0:0 / 0:0 | 1.66 / 2.44 |
| `CharacterBody.GetLastMotion()` | 0.0084 / 0.0081 | 1.036 | 0.0094 / 0.0094 | 0:0 / 0:0 | 1.46 / 2.20 |
| `CharacterBody.GetPlatformVelocity()` | 0.0103 / 0.0082 | 1.256 | 0.0125 / 0.0094 | 0:0 / 0:0 | 2.78 / 2.44 |
| `CharacterBody.GetPositionDelta()` | 0.0186 / 0.0184 | 1.011 | 0.0187 / 0.0187 | 0:0 / 0:0 | 1.86 / 1.81 |
| `CharacterBody.GetRealVelocity()` | 0.0083 / 0.0081 | 1.018 | 0.0094 / 0.0094 | 0:0 / 0:0 | 3.17 / 2.25 |
| `CharacterBody.GetWallNormal()` | 0.0079 / 0.0081 | 0.976 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.83 / 2.15 |
| `CharacterBody.GetSlideCollisionCount()` | 0.0085 / 0.0084 | 1.017 | 0.0094 / 0.0094 | 0:0 / 0:0 | 1.46 / 2.20 |
| `CharacterBody.GetFloorAngle(Nullable`1[Vector2])` | 0.0146 / 0.0144 | 1.010 | 0.0156 / 0.0156 | 0:0 / 0:0 | 1.95 / 2.20 |
| `CharacterBody.ApplyFloorSnap()` | 0.0813 / 0.0788 | 1.033 | 0.0750 / 0.0938 | 0:0 / 0:0 | 2.29 / 2.78 |
| `CharacterBody.GetSlideCollision(Int32, KinematicCollision)` | 0.0289 / 0.0292 | 0.992 | 0.0312 / 0.0312 | 0:0 / 0:0 | 3.17 / 2.15 |
| `CollisionObject.GetCollisionLayerValue(Int32)` | 0.0080 / 0.0083 | 0.964 | 0.0094 / 0.0094 | 0:0 / 0:0 | 1.46 / 2.20 |
| `CollisionObject.GetCollisionMaskValue(Int32)` | 0.0100 / 0.0079 | 1.273 | 0.0125 / 0.0094 | 0:0 / 0:0 | 2.78 / 2.39 |
| `CollisionObject.IsShapeOwnerDisabled(UInt32)` | 0.0291 / 0.0281 | 1.033 | 0.0312 / 0.0281 | 0:0 / 0:0 | 1.56 / 2.44 |
| `CollisionObject.IsShapeOwnerOneWayCollisionEnabled(UInt32)` | 0.0291 / 0.0277 | 1.051 | 0.0312 / 0.0281 | 0:0 / 0:0 | 3.08 / 2.29 |
| `CollisionObject.ShapeOwnerGetOwner(UInt32)` | 0.0335 / 0.0319 | 1.052 | 0.0344 / 0.0312 | 0:0 / 0:0 | 1.61 / 2.39 |
| `CollisionObject.GetRID()` | 0.0047 / 0.0047 | 1.000 | 0.0063 / 0.0063 | 0:0 / 0:0 | 1.51 / 2.20 |
| `CollisionObject.ShapeOwnerGetShape(UInt32, Int32)` | 0.0333 / 0.0323 | 1.030 | 0.0344 / 0.0344 | 0:0 / 0:0 | 2.78 / 2.39 |
| `CollisionObject.ShapeOwnerGetTransform(UInt32)` | 0.0295 / 0.0278 | 1.063 | 0.0312 / 0.0281 | 0:0 / 0:0 | 2.49 / 2.44 |
| `CollisionObject.GetShapeOwnerOneWayCollisionDirection(UInt32)` | 0.0368 / 0.0360 | 1.023 | 0.0375 / 0.0344 | 0:0 / 0:0 | 2.83 / 2.78 |
| `CollisionObject.ShapeOwnerGetShapeCount(UInt32)` | 0.0292 / 0.0349 | 0.836 | 0.0312 / 0.0344 | 0:0 / 0:0 | 1.46 / 2.78 |
| `CollisionObject.ShapeOwnerGetShapeIndex(UInt32, Int32)` | 0.0311 / 0.0293 | 1.060 | 0.0312 / 0.0312 | 0:0 / 0:0 | 2.49 / 2.10 |
| `CollisionObject.GetShapeOwnerOneWayCollisionMargin(UInt32)` | 0.0292 / 0.0274 | 1.064 | 0.0312 / 0.0281 | 0:0 / 0:0 | 3.08 / 2.39 |
| `CollisionObject.CreateShapeOwner(ElectronObject)` | 0.4238 / 0.4228 | 1.003 | 0.4375 / 0.4469 | 304:304 / 304:304 | 2.73 / 2.49 |
| `CollisionObject.ShapeFindOwner(Int32)` | 0.0263 / 0.0235 | 1.116 | 0.0281 / 0.0250 | 0:0 / 0:0 | 2.83 / 2.39 |
| `CollisionObject.GetShapeOwners()` | 0.0861 / 0.0836 | 1.030 | 0.0906 / 0.0938 | 200:200 / 200:200 | 1.56 / 2.00 |
| `CollisionObject.RemoveShapeOwner(UInt32)` | 0.5548 / 0.5548 | 1.000 | 0.5781 / 0.5687 | 0:0 / 0:0 | 1.61 / 2.25 |
| `CollisionObject.SetCollisionLayerValue(Int32, Boolean)` | 0.2854 / 0.2182 | 1.308 | 0.2875 / 0.2188 | 0:0 / 0:0 | 2.49 / 2.34 |
| `CollisionObject.SetCollisionMaskValue(Int32, Boolean)` | 0.3505 / 0.2510 | 1.396 | 0.5250 / 0.2719 | 0:0 / 0:0 | 2.44 / 2.54 |
| `CollisionObject.ShapeOwnerAddShape(UInt32, Shape)` | 0.5463 / 0.5449 | 1.003 | 0.5531 / 0.5531 | 184:184 / 184:184 | 2.78 / 4.49 |
| `CollisionObject.ShapeOwnerClearShapes(UInt32)` | 0.5062 / 0.6923 | 0.731 | 0.5156 / 0.9750 | 0:0 / 0:0 | 2.44 / 4.39 |
| `CollisionObject.ShapeOwnerRemoveShape(UInt32, Int32)` | 0.4919 / 0.4884 | 1.007 | 0.5062 / 0.5062 | 0:0 / 0:0 | 3.12 / 2.83 |
| `CollisionObject.ShapeOwnerSetDisabled(UInt32, Boolean)` | 0.1291 / 0.1268 | 1.018 | 0.1313 / 0.1281 | 0:0 / 0:0 | 3.17 / 2.34 |
| `CollisionObject.ShapeOwnerSetOneWayCollision(UInt32, Boolean)` | 0.1623 / 0.1416 | 1.146 | 0.1688 / 0.1500 | 0:0 / 0:0 | 2.98 / 2.49 |
| `CollisionObject.ShapeOwnerSetOneWayCollisionDirection(UInt32, Vector2)` | 0.1543 / 0.1214 | 1.271 | 0.1531 / 0.1219 | 0:0 / 0:0 | 2.83 / 2.29 |
| `CollisionObject.ShapeOwnerSetOneWayCollisionMargin(UInt32, Single)` | 0.1313 / 0.1111 | 1.182 | 0.1406 / 0.1125 | 0:0 / 0:0 | 3.17 / 2.20 |
| `CollisionObject.ShapeOwnerSetTransform(UInt32, Transform)` | 0.1371 / 0.1529 | 0.897 | 0.1375 / 0.1969 | 0:0 / 0:0 | 1.90 / 1.90 |
| `CollisionPolygon.GetConfigurationWarnings()` | 0.0889 / 0.0931 | 0.955 | 0.1031 / 0.1031 | 64:64 / 64:64 | 2.59 / 2.29 |
| `CollisionShape.GetConfigurationWarnings()` | 0.0870 / 0.0937 | 0.929 | 0.0969 / 0.1031 | 64:64 / 64:64 | 1.51 / 2.34 |
| `Joint.GetRID()` | 0.0044 / 0.0044 | 1.000 | 0.0063 / 0.0063 | 0:0 / 0:0 | 1.56 / 2.44 |
| `Joint.GetConfigurationWarnings()` | 0.1450 / 0.1482 | 0.979 | 0.1625 / 0.1594 | 64:64 / 64:64 | 1.95 / 2.39 |
| `KinematicCollision.GetCollider()` | 0.0090 / 0.0087 | 1.034 | 0.0094 / 0.0094 | 0:0 / 0:0 | 3.08 / 2.20 |
| `KinematicCollision.GetColliderShape()` | 0.0248 / 0.0273 | 0.907 | 0.0250 / 0.0437 | 0:0 / 0:0 | 3.08 / 2.39 |
| `KinematicCollision.GetLocalShape()` | 0.0251 / 0.0247 | 1.018 | 0.0250 / 0.0250 | 0:0 / 0:0 | 3.17 / 2.15 |
| `KinematicCollision.GetColliderRID()` | 0.0045 / 0.0046 | 0.989 | 0.0063 / 0.0063 | 0:0 / 0:0 | 1.81 / 2.39 |
| `KinematicCollision.GetColliderVelocity()` | 0.0062 / 0.0045 | 1.370 | 0.0063 / 0.0063 | 0:0 / 0:0 | 2.88 / 2.10 |
| `KinematicCollision.GetNormal()` | 0.0047 / 0.0059 | 0.793 | 0.0063 / 0.0063 | 0:0 / 0:0 | 3.17 / 2.83 |
| `KinematicCollision.GetPosition()` | 0.0045 / 0.0046 | 0.989 | 0.0063 / 0.0063 | 0:0 / 0:0 | 2.78 / 2.39 |
| `KinematicCollision.GetRemainder()` | 0.0044 / 0.0046 | 0.958 | 0.0063 / 0.0063 | 0:0 / 0:0 | 1.56 / 2.44 |
| `KinematicCollision.GetTravel()` | 0.0045 / 0.0048 | 0.929 | 0.0063 / 0.0063 | 0:0 / 0:0 | 3.17 / 2.20 |
| `KinematicCollision.GetColliderShapeIndex()` | 0.0049 / 0.0044 | 1.122 | 0.0063 / 0.0063 | 0:0 / 0:0 | 2.29 / 2.20 |
| `KinematicCollision.GetAngle(Nullable`1[Vector2])` | 0.0120 / 0.0121 | 0.996 | 0.0125 / 0.0125 | 0:0 / 0:0 | 1.76 / 2.15 |
| `KinematicCollision.GetDepth()` | 0.0044 / 0.0043 | 1.011 | 0.0063 / 0.0063 | 0:0 / 0:0 | 1.95 / 2.20 |
| `KinematicCollision.GetColliderID()` | 0.0046 / 0.0045 | 1.011 | 0.0063 / 0.0063 | 0:0 / 0:0 | 2.88 / 2.25 |
| `PhysicsBody.TestMove(Transform, Vector2, KinematicCollision, Single, Boolean)` | 9.1583 / 343.2135 | 0.027 | 10.5625 / 376.4844 | 0:0 / 0:0 | 2.83 / 3.08 |
| `PhysicsBody.MoveAndCollide(Vector2, Boolean, Single, Boolean)` | 9.1411 / 345.1713 | 0.026 | 9.2812 / 403.0719 | 272:272 / 272:272 | 2.29 / 3.03 |
| `PhysicsBody.GetCollisionExceptions()` | 0.1642 / 0.1298 | 1.265 | 0.1750 / 0.1437 | 56:56 / 56:56 | 2.83 / 2.44 |
| `PhysicsBody.GetGravity()` | 0.0092 / 0.0092 | 1.000 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.88 / 2.10 |
| `PhysicsBody.AddCollisionExceptionWith(PhysicsBody)` | 0.2639 / 0.2777 | 0.950 | 0.2656 / 0.2750 | 0:0 / 0:0 | 2.93 / 2.49 |
| `PhysicsBody.RemoveCollisionExceptionWith(PhysicsBody)` | 0.3067 / 0.3125 | 0.982 | 0.3250 / 0.3250 | 0:0 / 0:0 | 2.44 / 2.34 |
| `RayCast.GetCollisionMaskValue(Int32)` | 0.0077 / 0.0079 | 0.981 | 0.0094 / 0.0094 | 0:0 / 0:0 | 1.51 / 2.34 |
| `RayCast.IsColliding()` | 0.0086 / 0.0088 | 0.978 | 0.0094 / 0.0094 | 0:0 / 0:0 | 3.17 / 2.44 |
| `RayCast.GetCollider()` | 0.0118 / 0.0120 | 0.988 | 0.0125 / 0.0125 | 0:0 / 0:0 | 1.46 / 2.20 |
| `RayCast.GetColliderRID()` | 0.0076 / 0.0078 | 0.969 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.83 / 2.29 |
| `RayCast.GetCollisionNormal()` | 0.0075 / 0.0078 | 0.962 | 0.0094 / 0.0094 | 0:0 / 0:0 | 3.17 / 2.10 |
| `RayCast.GetCollisionPoint()` | 0.0078 / 0.0075 | 1.039 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.49 / 2.29 |
| `RayCast.GetColliderShape()` | 0.0088 / 0.0112 | 0.786 | 0.0094 / 0.0125 | 0:0 / 0:0 | 1.46 / 3.47 |
| `RayCast.AddException(CollisionObject)` | 0.0427 / 0.0498 | 0.859 | 0.0437 / 0.0437 | 0:0 / 0:0 | 3.17 / 2.20 |
| `RayCast.AddExceptionRID(RID)` | 0.0227 / 0.0226 | 1.006 | 0.0250 / 0.0250 | 0:0 / 0:0 | 2.64 / 2.39 |
| `RayCast.ClearExceptions()` | 0.0286 / 0.0290 | 0.988 | 0.0312 / 0.0312 | 0:0 / 0:0 | 3.17 / 2.44 |
| `RayCast.ForceRaycastUpdate()` | 1.1769 / 110.9067 | 0.011 | 1.3438 / 159.8031 | 0:0 / 0:0 | 2.88 / 2.93 |
| `RayCast.RemoveException(CollisionObject)` | 0.0417 / 0.0418 | 0.998 | 0.0437 / 0.0437 | 0:0 / 0:0 | 2.20 / 2.44 |
| `RayCast.RemoveExceptionRID(RID)` | 0.0212 / 0.0216 | 0.982 | 0.0219 / 0.0219 | 0:0 / 0:0 | 3.17 / 2.44 |
| `RayCast.SetCollisionMaskValue(Int32, Boolean)` | 0.0242 / 0.0237 | 1.019 | 0.0250 / 0.0250 | 0:0 / 0:0 | 1.71 / 2.25 |
| `RigidBody.GetCollidingBodies()` | 0.1033 / 0.0921 | 1.121 | 0.1531 / 0.1000 | 120:120 / 120:120 | 3.17 / 2.44 |
| `RigidBody.GetContactCount()` | 0.0089 / 0.0112 | 0.799 | 0.0094 / 0.0125 | 0:0 / 0:0 | 2.49 / 2.78 |
| `RigidBody.AddConstantCentralForce(Vector2)` | 0.3268 / 0.3291 | 0.993 | 0.3281 / 0.3281 | 0:0 / 0:0 | 2.83 / 2.39 |
| `RigidBody.AddConstantForce(Vector2, Vector2)` | 0.4097 / 299.8171 | 0.001 | 0.4094 / 353.9563 | 0:0 / 0:0 | 1.66 / 2.49 |
| `RigidBody.AddConstantTorque(Single)` | 0.3227 / 0.3257 | 0.991 | 0.3250 / 0.3281 | 0:0 / 0:0 | 1.86 / 2.44 |
| `RigidBody.ApplyCentralForce(Vector2)` | 0.3063 / 0.3184 | 0.962 | 0.3063 / 0.3187 | 0:0 / 0:0 | 1.56 / 2.34 |
| `RigidBody.ApplyCentralImpulse(Vector2)` | 0.5489 / 297.6000 | 0.002 | 0.6219 / 365.4250 | 0:0 / 0:0 | 2.98 / 3.27 |
| `RigidBody.ApplyForce(Vector2, Vector2)` | 0.4088 / 296.9649 | 0.001 | 0.5437 / 361.3750 | 0:0 / 0:0 | 2.39 / 3.22 |
| `RigidBody.ApplyImpulse(Vector2, Vector2)` | 0.5522 / 389.9773 | 0.001 | 0.5469 / 471.7750 | 0:0 / 0:0 | 1.95 / 3.22 |
| `RigidBody.ApplyTorque(Single)` | 0.3053 / 0.3143 | 0.971 | 0.3063 / 0.3156 | 0:0 / 0:0 | 1.56 / 2.15 |
| `RigidBody.ApplyTorqueImpulse(Single)` | 0.4921 / 298.7986 | 0.002 | 0.4906 / 370.8562 | 0:0 / 0:0 | 3.17 / 3.17 |
| `RigidBody.SetAxisVelocity(Vector2)` | 0.4395 / 304.9193 | 0.001 | 0.5000 / 381.6500 | 0:0 / 0:0 | 1.76 / 2.59 |
| `ShapeCast.GetCollisionMaskValue(Int32)` | 0.0098 / 0.0104 | 0.943 | 0.0125 / 0.0125 | 0:0 / 0:0 | 2.98 / 2.73 |
| `ShapeCast.IsColliding()` | 0.0079 / 0.0081 | 0.976 | 0.0094 / 0.0094 | 0:0 / 0:0 | 3.08 / 2.05 |
| `ShapeCast.GetCollider(Int32)` | 0.0133 / 0.0134 | 0.989 | 0.0156 / 0.0156 | 0:0 / 0:0 | 1.76 / 1.03 |
| `ShapeCast.GetColliderRID(Int32)` | 0.0101 / 0.0103 | 0.976 | 0.0125 / 0.0125 | 0:0 / 0:0 | 1.56 / 2.25 |
| `ShapeCast.GetCollisionNormal(Int32)` | 0.0102 / 0.0104 | 0.981 | 0.0125 / 0.0125 | 0:0 / 0:0 | 3.17 / 2.34 |
| `ShapeCast.GetCollisionPoint(Int32)` | 0.0103 / 0.0101 | 1.014 | 0.0125 / 0.0125 | 0:0 / 0:0 | 2.20 / 2.78 |
| `ShapeCast.GetColliderShape(Int32)` | 0.0101 / 0.0104 | 0.967 | 0.0125 / 0.0125 | 0:0 / 0:0 | 1.56 / 2.25 |
| `ShapeCast.GetCollisionCount()` | 0.0081 / 0.0084 | 0.960 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.54 / 3.17 |
| `ShapeCast.GetClosestCollisionSafeFraction()` | 0.0096 / 0.0083 | 1.153 | 0.0156 / 0.0094 | 0:0 / 0:0 | 2.00 / 2.93 |
| `ShapeCast.GetClosestCollisionUnsafeFraction()` | 0.0081 / 0.0081 | 1.000 | 0.0094 / 0.0094 | 0:0 / 0:0 | 3.17 / 1.90 |
| `ShapeCast.AddException(CollisionObject)` | 0.0430 / 0.0427 | 1.006 | 0.0437 / 0.0437 | 0:0 / 0:0 | 1.42 / 2.44 |
| `ShapeCast.AddExceptionRID(RID)` | 0.0227 / 0.0227 | 0.998 | 0.0250 / 0.0250 | 0:0 / 0:0 | 1.56 / 2.15 |
| `ShapeCast.ClearExceptions()` | 0.0209 / 0.0205 | 1.021 | 0.0219 / 0.0219 | 0:0 / 0:0 | 2.64 / 1.27 |
| `ShapeCast.ForceShapecastUpdate()` | 5.3085 / 379.7684 | 0.014 | 5.5062 / 431.5531 | 0:0 / 0:0 | 3.12 / 2.49 |
| `ShapeCast.RemoveException(CollisionObject)` | 0.0417 / 0.0416 | 1.002 | 0.0437 / 0.0437 | 0:0 / 0:0 | 2.49 / 2.39 |
| `ShapeCast.RemoveExceptionRID(RID)` | 0.0215 / 0.0215 | 0.998 | 0.0219 / 0.0219 | 0:0 / 0:0 | 1.71 / 2.10 |
| `ShapeCast.SetCollisionMaskValue(Int32, Boolean)` | 0.0226 / 0.0299 | 0.754 | 0.0250 / 0.0312 | 0:0 / 0:0 | 1.90 / 2.78 |

## Resources, geometry and snapshots

| Public member | Mean CPU / GPU, us | CPU/GPU | p95 CPU / GPU, us | Managed CPU / GPU, owner:process B/call | Empty loop CPU / GPU, ns |
| --- | ---: | ---: | ---: | ---: | ---: |
| `CapsuleShape.GetRect()` | 0.0090 / 0.0088 | 1.017 | 0.0094 / 0.0094 | 0:0 / 0:0 | 18.75 / 19.09 |
| `CircleShape.GetRect()` | 0.0086 / 0.0088 | 0.978 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.39 / 2.39 |
| `ConcavePolygonShape.GetRect()` | 0.0116 / 0.0116 | 1.000 | 0.0125 / 0.0125 | 0:0 / 0:0 | 2.44 / 2.44 |
| `ConvexPolygonShape.GetRect()` | 0.0126 / 0.0125 | 1.004 | 0.0125 / 0.0125 | 0:0 / 0:0 | 2.44 / 2.25 |
| `ConvexPolygonShape.SetPointCloud(ReadOnlySpan`1[Vector2])` | 0.6069 / 0.5993 | 1.013 | 0.7406 / 0.6219 | 688:688 / 688:688 | 1.95 / 2.44 |
| `PhysicsCheckpoint.Capture()` | 1.6811 / 106.7074 | 0.016 | 1.6781 / 160.3688 | 0:0 / 0:0 | 2.34 / 2.98 |
| `PhysicsCheckpoint.Dispose()` | 0.0816 / 1.8429 | 0.044 | 0.0969 / 2.7625 | 0:0 / 0:0 | 1.95 / 3.03 |
| `PhysicsCheckpoint.Restore()` | 1.8629 / 102.4419 | 0.018 | 1.8781 / 134.8469 | 0:0 / 0:0 | 2.44 / 3.03 |
| `PhysicsRayQueryParameters.Create(Vector2, Vector2, UInt32, RID[])` | 0.1302 / 0.1325 | 0.983 | 0.1688 / 0.1594 | 208:208 / 208:208 | 2.49 / 2.49 |
| `PhysicsSnapshot.GetEncodedSize()` | 0.0077 / 0.0073 | 1.053 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.39 / 2.49 |
| `PhysicsSnapshot.WriteTo(Span`1[Byte])` | 0.0302 / 0.0199 | 1.517 | 0.0219 / 0.0219 | 0:0 / 0:0 | 2.44 / 2.59 |
| `PhysicsSnapshotMap.Unbind(UInt64)` | 0.0492 / 0.0472 | 1.043 | 0.0500 / 0.0500 | 0:0 / 0:0 | 2.54 / 2.10 |
| `PhysicsSnapshotMap.Apply(PhysicsSnapshot)` | 6.9229 / 875.5510 | 0.008 | 7.0594 / 958.2781 | 0:0 / 0:0 | 2.49 / 2.98 |
| `PhysicsSnapshotMap.Bind(UInt64, UInt32, RID)` | 0.2154 / 0.2078 | 1.037 | 0.2562 / 0.2437 | 1472:1472 / 1472:1472 | 2.00 / 2.54 |
| `PhysicsSnapshotMap.Capture(PhysicsSnapshot)` | 7.1154 / 123.8476 | 0.057 | 7.2375 / 185.6687 | 0:0 / 0:0 | 2.49 / 3.03 |
| `PhysicsSnapshotMap.Dispose()` | 0.0505 / 0.0508 | 0.995 | 0.0531 / 0.0531 | 0:0 / 0:0 | 2.49 / 3.27 |
| `PhysicsSnapshot.ReadFrom(ReadOnlySpan`1[Byte])` | 0.7131 / 0.7763 | 0.919 | 0.7188 / 0.9000 | 0:0 / 0:0 | 2.49 / 2.78 |
| `PhysicsTestMotionResult.GetCollider()` | 0.0092 / 0.0091 | 1.005 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.34 / 2.44 |
| `PhysicsTestMotionResult.GetColliderRID()` | 0.0073 / 0.0073 | 1.007 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.54 / 2.39 |
| `PhysicsTestMotionResult.GetColliderVelocity()` | 0.0074 / 0.0073 | 1.020 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.15 / 2.34 |
| `PhysicsTestMotionResult.GetCollisionNormal()` | 0.0073 / 0.0071 | 1.021 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.34 / 2.10 |
| `PhysicsTestMotionResult.GetCollisionPoint()` | 0.0069 / 0.0072 | 0.959 | 0.0094 / 0.0094 | 0:0 / 0:0 | 1.90 / 2.05 |
| `PhysicsTestMotionResult.GetRemainder()` | 0.0072 / 0.0071 | 1.021 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.10 / 2.15 |
| `PhysicsTestMotionResult.GetTravel()` | 0.0090 / 0.0101 | 0.898 | 0.0094 / 0.0125 | 0:0 / 0:0 | 2.93 / 2.78 |
| `PhysicsTestMotionResult.GetColliderShape()` | 0.0062 / 0.0063 | 0.984 | 0.0063 / 0.0063 | 0:0 / 0:0 | 1.71 / 2.49 |
| `PhysicsTestMotionResult.GetCollisionLocalShape()` | 0.0078 / 0.0062 | 1.262 | 0.0094 / 0.0063 | 0:0 / 0:0 | 2.88 / 2.49 |
| `PhysicsTestMotionResult.GetCollisionDepth()` | 0.0062 / 0.0063 | 0.969 | 0.0063 / 0.0063 | 0:0 / 0:0 | 2.49 / 2.25 |
| `PhysicsTestMotionResult.GetCollisionSafeFraction()` | 0.0062 / 0.0078 | 0.787 | 0.0063 / 0.0094 | 0:0 / 0:0 | 1.95 / 2.78 |
| `PhysicsTestMotionResult.GetCollisionUnsafeFraction()` | 0.0079 / 0.0062 | 1.276 | 0.0094 / 0.0063 | 0:0 / 0:0 | 2.73 / 2.39 |
| `PhysicsTestMotionResult.GetColliderID()` | 0.0063 / 0.0063 | 0.992 | 0.0063 / 0.0063 | 0:0 / 0:0 | 2.44 / 2.49 |
| `PhysicsTestMotionResult.SetCollision(RID, Int32, RID, Int32, Vector2, Vector2, Single, Vector2, Vector2, Vector2, Single, Single)` | 0.1899 / 0.1885 | 1.008 | 0.1875 / 0.2344 | 0:0 / 0:0 | 2.34 / 2.20 |
| `PhysicsTestMotionResult.SetMotion(Vector2)` | 0.0286 / 0.0280 | 1.021 | 0.0312 / 0.0281 | 0:0 / 0:0 | 2.05 / 1.90 |
| `RectangleShape.GetRect()` | 0.0086 / 0.0088 | 0.978 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.34 / 1.90 |
| `SegmentShape.GetRect()` | 0.0093 / 0.0094 | 0.995 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.39 / 1.90 |
| `SeparationRayShape.GetRect()` | 0.0086 / 0.0087 | 0.989 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.10 / 2.44 |
| `Shape.Collide(Transform, Shape, Transform)` | 0.5370 / 0.5606 | 0.958 | 0.6281 / 0.7906 | 0:0 / 0:0 | 1.95 / 2.34 |
| `Shape.CollideWithMotion(Transform, Vector2, Shape, Transform, Vector2)` | 0.5276 / 0.5365 | 0.983 | 0.5281 / 0.5406 | 0:0 / 0:0 | 1.95 / 2.44 |
| `Shape.GetRID()` | 0.0129 / 0.0129 | 1.004 | 0.0156 / 0.0156 | 0:0 / 0:0 | 2.15 / 2.39 |
| `Shape.GetRect()` | 0.0087 / 0.0087 | 1.000 | 0.0094 / 0.0094 | 0:0 / 0:0 | 2.44 / 2.49 |
| `Shape.GetGeometryData()` | 0.0154 / 0.0158 | 0.975 | 0.0156 / 0.0156 | 0:0 / 0:0 | 2.44 / 2.44 |
| `Shape.CollideAndGetContacts(Transform, Shape, Transform)` | 0.5706 / 0.5746 | 0.993 | 0.5750 / 0.5875 | 40:40 / 40:40 | 2.54 / 2.49 |
| `Shape.CollideWithMotionAndGetContacts(Transform, Vector2, Shape, Transform, Vector2)` | 0.5939 / 0.5917 | 1.004 | 0.6000 / 0.6000 | 40:40 / 40:40 | 2.59 / 2.54 |
| `ShapeGeometry.GetCustomMassProperties()` | 0.0034 / 0.0028 | 1.190 | 0.0063 / 0.0031 | 0:0 / 0:0 | 2.88 / 2.34 |
| `ShapeGeometry.Capsule(Single, Single)` | 0.0151 / 0.0163 | 0.928 | 0.0219 / 0.0281 | 120:120 / 120:120 | 2.39 / 2.39 |
| `ShapeGeometry.Circle(Single)` | 0.0126 / 0.0132 | 0.952 | 0.0156 / 0.0187 | 120:120 / 120:120 | 2.44 / 2.44 |
| `ShapeGeometry.ConcavePolygon(ReadOnlySpan`1[Vector2])` | 0.0258 / 0.0267 | 0.967 | 0.0312 / 0.0344 | 160:160 / 160:160 | 2.39 / 2.49 |
| `ShapeGeometry.ConvexPolygon(ReadOnlySpan`1[Vector2])` | 0.1904 / 0.1927 | 0.988 | 0.2031 / 0.2094 | 592:592 / 592:592 | 2.44 / 2.44 |
| `ShapeGeometry.Custom(String, ReadOnlySpan`1[Byte], Rect2, PhysicsMassProperties, ReadOnlySpan`1[Vector2], ShapeCollisionAlgorithm)` | 0.0289 / 0.0293 | 0.983 | 0.0344 / 0.0344 | 120:120 / 120:120 | 2.25 / 2.49 |
| `ShapeGeometry.Rectangle(Vector2)` | 0.0135 / 0.0131 | 1.030 | 0.0187 / 0.0156 | 120:120 / 120:120 | 2.44 / 2.39 |
| `ShapeGeometry.Segment(Vector2, Vector2)` | 0.0208 / 0.0207 | 1.002 | 0.0250 / 0.0250 | 120:120 / 120:120 | 2.44 / 2.44 |
| `ShapeGeometry.SeparationRay(Single, Boolean)` | 0.0160 / 0.0160 | 1.003 | 0.0250 / 0.0219 | 120:120 / 120:120 | 2.78 / 2.88 |
| `ShapeGeometry.WorldBoundary(Vector2, Single)` | 0.0350 / 0.0329 | 1.064 | 0.0437 / 0.0375 | 120:120 / 120:120 | 2.39 / 2.44 |
| `ShapeGeometry.GetRectangleSize()` | 0.0027 / 0.0024 | 1.143 | 0.0031 / 0.0031 | 0:0 / 0:0 | 2.29 / 2.25 |
| `ShapeGeometry.GetCircleRadius()` | 0.0029 / 0.0027 | 1.091 | 0.0031 / 0.0031 | 0:0 / 0:0 | 2.20 / 2.10 |
| `ShapeGeometry.GetConcavePolygonSegments()` | 0.0030 / 0.0025 | 1.216 | 0.0031 / 0.0031 | 0:0 / 0:0 | 2.39 / 2.49 |
| `ShapeGeometry.GetConvexPolygonPoints()` | 0.0030 / 0.0025 | 1.216 | 0.0031 / 0.0031 | 0:0 / 0:0 | 2.49 / 2.39 |
| `ShapeGeometry.GetCustomDrawingSegments()` | 0.0028 / 0.0028 | 1.000 | 0.0031 / 0.0031 | 0:0 / 0:0 | 2.25 / 2.49 |
| `ShapeGeometry.GetCustomData()` | 0.0027 / 0.0026 | 1.019 | 0.0031 / 0.0031 | 0:0 / 0:0 | 2.34 / 2.05 |
| `ShapeGeometry.GetCustomFormatID()` | 0.0042 / 0.0042 | 0.988 | 0.0063 / 0.0063 | 0:0 / 0:0 | 2.44 / 2.49 |
| `ShapeGeometry.GetSegmentEndpoints()` | 0.0034 / 0.0036 | 0.945 | 0.0063 / 0.0063 | 0:0 / 0:0 | 2.44 / 2.49 |
| `ShapeGeometry.GetWorldBoundaryPlane()` | 0.0033 / 0.0034 | 0.971 | 0.0031 / 0.0063 | 0:0 / 0:0 | 2.10 / 2.49 |
| `ShapeGeometry.GetSeparationRayParameters()` | 0.0031 / 0.0032 | 0.955 | 0.0031 / 0.0031 | 0:0 / 0:0 | 2.49 / 2.54 |
| `ShapeGeometry.GetCapsuleDimensions()` | 0.0042 / 0.0044 | 0.967 | 0.0063 / 0.0063 | 0:0 / 0:0 | 2.39 / 1.90 |
| `WorldBoundaryShape.GetRect()` | 0.0327 / 0.0256 | 1.274 | 0.0344 / 0.0281 | 0:0 / 0:0 | 2.83 / 2.10 |

## Physical skeleton methods

| Public member | Mean CPU / GPU, us | CPU/GPU | p95 CPU / GPU, us | Managed CPU / GPU, owner:process B/call | Empty loop CPU / GPU, ns |
| --- | ---: | ---: | ---: | ---: | ---: |
| `PhysicalBone.IsSimulatingPhysics()` | 0.0630 / 0.0640 | 0.985 | 0.0625 / 0.0656 | 0:0 / 0:0 | 19.04 / 18.95 |
| `PhysicalBone.GetJoint()` | 0.0274 / 0.0194 | 1.412 | 0.0312 / 0.0219 | 0:0 / 0:0 | 2.88 / 2.44 |
| `PhysicalBone.GetConfigurationWarnings()` | 0.1105 / 0.1147 | 0.964 | 0.1187 / 0.1250 | 32:32 / 32:32 | 1.86 / 2.39 |
| `SkeletonModificationJiggle.GetJiggleJointOverride(Int32)` | 0.0153 / 0.0152 | 1.006 | 0.0156 / 0.0156 | 0:0 / 0:0 | 2.39 / 2.20 |
| `SkeletonModificationJiggle.GetJiggleJointUseGravity(Int32)` | 0.0152 / 0.0153 | 0.994 | 0.0156 / 0.0156 | 0:0 / 0:0 | 2.39 / 1.81 |
| `SkeletonModificationJiggle.GetUseColliders()` | 0.0126 / 0.0124 | 1.020 | 0.0125 / 0.0125 | 0:0 / 0:0 | 2.44 / 2.44 |
| `SkeletonModificationJiggle.GetJiggleJointGravity(Int32)` | 0.0200 / 0.0158 | 1.266 | 0.0219 / 0.0156 | 0:0 / 0:0 | 2.83 / 2.54 |
| `SkeletonModificationJiggle.GetJiggleJointBoneIndex(Int32)` | 0.0819 / 0.0830 | 0.987 | 0.0844 / 0.0844 | 0:0 / 0:0 | 1.90 / 2.20 |
| `SkeletonModificationJiggle.GetJiggleJointDamping(Int32)` | 0.0146 / 0.0153 | 0.952 | 0.0156 / 0.0156 | 0:0 / 0:0 | 2.39 / 2.44 |
| `SkeletonModificationJiggle.GetJiggleJointMass(Int32)` | 0.0151 / 0.0149 | 1.013 | 0.0156 / 0.0156 | 0:0 / 0:0 | 2.05 / 2.39 |
| `SkeletonModificationJiggle.GetJiggleJointStiffness(Int32)` | 0.0149 / 0.0150 | 0.997 | 0.0156 / 0.0156 | 0:0 / 0:0 | 2.39 / 2.49 |
| `SkeletonModificationJiggle.GetJiggleJointBoneNode(Int32)` | 0.0165 / 0.0165 | 1.000 | 0.0187 / 0.0187 | 0:0 / 0:0 | 2.10 / 1.90 |
| `SkeletonModificationJiggle.GetCollisionMask()` | 0.0129 / 0.0126 | 1.019 | 0.0156 / 0.0125 | 0:0 / 0:0 | 2.10 / 2.34 |
| `SkeletonModificationJiggle.Reset()` | 0.0949 / 0.0953 | 0.996 | 0.0969 / 0.0969 | 0:0 / 0:0 | 2.39 / 2.44 |
| `SkeletonModificationJiggle.SetCollisionMask(UInt32)` | 0.0286 / 0.0359 | 0.797 | 0.0312 / 0.0375 | 0:0 / 0:0 | 2.29 / 2.88 |
| `SkeletonModificationJiggle.SetJiggleJointBoneIndex(Int32, Int32)` | 0.2319 / 0.2347 | 0.988 | 0.2344 / 0.2437 | 264:264 / 264:264 | 1.95 / 2.34 |
| `SkeletonModificationJiggle.SetJiggleJointBoneNode(Int32, String)` | 0.0380 / 0.0380 | 1.000 | 0.0406 / 0.0406 | 0:0 / 0:0 | 2.54 / 2.05 |
| `SkeletonModificationJiggle.SetJiggleJointDamping(Int32, Single)` | 0.0356 / 0.0358 | 0.993 | 0.0375 / 0.0375 | 0:0 / 0:0 | 2.39 / 2.39 |
| `SkeletonModificationJiggle.SetJiggleJointGravity(Int32, Vector2)` | 0.0350 / 0.0352 | 0.994 | 0.0375 / 0.0375 | 0:0 / 0:0 | 2.29 / 1.90 |
| `SkeletonModificationJiggle.SetJiggleJointMass(Int32, Single)` | 0.0355 / 0.0366 | 0.972 | 0.0375 / 0.0375 | 0:0 / 0:0 | 2.15 / 3.17 |
| `SkeletonModificationJiggle.SetJiggleJointOverride(Int32, Boolean)` | 0.0380 / 0.0382 | 0.995 | 0.0406 / 0.0406 | 0:0 / 0:0 | 2.54 / 1.95 |
| `SkeletonModificationJiggle.SetJiggleJointStiffness(Int32, Single)` | 0.0347 / 0.0347 | 1.001 | 0.0344 / 0.0344 | 0:0 / 0:0 | 1.95 / 2.54 |
| `SkeletonModificationJiggle.SetJiggleJointUseGravity(Int32, Boolean)` | 0.0325 / 0.0329 | 0.990 | 0.0344 / 0.0344 | 0:0 / 0:0 | 2.54 / 2.10 |
| `SkeletonModificationJiggle.SetUseColliders(Boolean)` | 0.0395 / 0.0315 | 1.253 | 0.0406 / 0.0312 | 0:0 / 0:0 | 2.83 / 2.44 |
| `SkeletonModificationPhysicalBones.GetPhysicalBoneNode(Int32)` | 0.0205 / 0.0162 | 1.269 | 0.0219 / 0.0187 | 0:0 / 0:0 | 2.73 / 2.20 |
| `SkeletonModificationPhysicalBones.FetchPhysicalBones()` | 0.5322 / 0.5439 | 0.978 | 0.5625 / 0.5719 | 576:576 / 576:576 | 2.10 / 2.44 |
| `SkeletonModificationPhysicalBones.SetPhysicalBoneNode(Int32, String)` | 0.0372 / 0.0374 | 0.995 | 0.0375 / 0.0375 | 0:0 / 0:0 | 1.90 / 2.49 |
| `SkeletonModificationPhysicalBones.StartSimulation(String[])` | 0.2132 / 0.2083 | 1.024 | 0.2125 / 0.2094 | 0:0 / 0:0 | 2.44 / 2.44 |
| `SkeletonModificationPhysicalBones.StopSimulation(String[])` | 0.2106 / 0.2104 | 1.001 | 0.2125 / 0.2125 | 0:0 / 0:0 | 2.54 / 3.17 |
