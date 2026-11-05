# Public physics timing

This project references the runtime as an ordinary consumer. It has no friend
access, backend dependencies, private reflection, internal profiling controls or
private timing counters. Performance intervals call the public Electron2D API.

## Complete scene frames

The `scene` mode measures `SceneTree.PhysicsFrame` on an explicitly selected CPU
or GPU world, with four walls, awake rigid bodies, contact events, eight-contact
caps and interpolation. Each fresh process warms 1600 and measures 256 frames
at fixed 1/144 second, with sixteen solver iterations. It checks completed ticks,
scene/direct-state agreement, finite and awake bodies, containment, penetration,
energy, contacts and zero warmed owner/process managed allocations. Physical
validation and cleanup are outside the timed interval.

```powershell
bin/physics-query-timing/PhysicsQueryPerformance.Consumer.exe scene cpu 16384 bin/scene-cpu-16384.json
bin/physics-query-timing/PhysicsQueryPerformance.Consumer.exe scene gpu 16384 bin/scene-gpu-16384.json
```

Every mode rejects accidental runtime friend access at startup. The consumer does
not produce private solver-stage timings or window FPS. Native device/driver and
host-load observations are separate environment evidence, outside physics timing.

## Direct-space queries

This public-only consumer measures every public query overload declared by
`PhysicsDirectSpaceState`: ray, point (array/span), shape (array/span), motion
cast, collision (array/span), and rest info. Startup compares the registered cases
against the compiled class and rejects missing or duplicate overloads.

Publish once in Release, then use fresh processes for each backend and body count:

```powershell
dotnet publish tests/PhysicsQueryPerformance.Consumer -c Release -r win-x64 --self-contained true -p:RuntimeFrameworkVersion=10.0.14 -o bin/physics-query-timing
bin/physics-query-timing/PhysicsQueryPerformance.Consumer.exe cpu 256 bin/query-cpu-256.json
bin/physics-query-timing/PhysicsQueryPerformance.Consumer.exe gpu 256 bin/query-gpu-256.json
```

Keep the runtime version, compiled runtime, native dependencies, host, fixture and
settings identical within each comparison. Record the actual selected GPU adapter
and driver separately. A software device does not measure hardware GPU speed.
The ordinary native asset prerequisites still apply to the GPU profile.

The fixture uses an unchanged static circle grid and executes both a one-collider
hit and a distant miss. Each overload warms 64 batches and measures 128 batches of
64 calls. Positive/negative results and collider identity are checked outside the
clock; every timed result contributes to the checked checksum. Array overloads
retain their allocation contract; span storage is caller-owned and reused.

The JSON reports unadjusted wall nanoseconds per call, including loop/delegate
overhead, and a separate empty-loop baseline. Percentiles describe batch means,
not individual calls. Synchronous query results include necessary transfers and
completion. Managed bytes cover the measured owner/process interval; they do not
describe retained, native or device memory. Setup, validation, reporting and
cleanup are outside the clock. No collection is forced in a timed interval.

These repeated warmed queries do not measure cold/edited-world queries, solver
stages, networking or window FPS. Full steps belong to the separate scene profile.

## Server, direct-body, node and resource methods

Use the same published host and dependencies for both backend arguments:

```powershell
PhysicsQueryPerformance.Consumer.exe server cpu bin/server-cpu.json
PhysicsQueryPerformance.Consumer.exe server gpu bin/server-gpu.json
PhysicsQueryPerformance.Consumer.exe body cpu bin/body-cpu.json
PhysicsQueryPerformance.Consumer.exe body gpu bin/body-gpu.json
PhysicsQueryPerformance.Consumer.exe nodes cpu bin/nodes-cpu.json
PhysicsQueryPerformance.Consumer.exe nodes gpu bin/nodes-gpu.json
PhysicsQueryPerformance.Consumer.exe resources cpu bin/resources-cpu.json
PhysicsQueryPerformance.Consumer.exe resources gpu bin/resources-gpu.json
PhysicsQueryPerformance.Consumer.exe bones cpu bin/bones-cpu.json
PhysicsQueryPerformance.Consumer.exe bones gpu bin/bones-gpu.json
```

The profiles enumerate declared public methods on their explicit family lists;
an unbound overload fails rather than disappearing from the report. Public
metadata selects cases and compiles ordinary calls before timing. There is no
runtime reflection invocation in a measured call. Inherited physical methods are
measured at their declaring type; general Object/Node/resource contracts retain
their subsystem scope. Constructors, other property/event accessors and protected
implementation hooks are outside the non-accessor method inventory.

| Mode | Measured members | Prepared scenario |
| --- | ---: | --- |
| `server` | 215 method overloads | Fresh completed two-body/Area/two-joint world per overload; typed public shape/motion queries and lifecycle cases |
| `body` | 29 methods and 20 property accessors | Real completed rigid/static contact; retained direct view and warmed contact details |
| `nodes` | 118 methods | Attached bodies, positive Area overlaps, character slide, ray/shape casts, shape owners and caller-owned collision snapshots |
| `resources` | 64 methods | Shape/resource geometry, motion results, query-parameter factory, local checkpoints and portable snapshots |
| `bones` | 29 methods | Bound physical bone, authored child pin joint and one-slot physical/jiggle modifier chains |

Ordinary cases warm 32 and measure 64 batches of 32 calls. World creation uses
one warm and eight measured single calls while a peer world/device remains live;
it is not cold device startup. Loops call prepared delegates and retain results.
Caller-owned objects, shape-data copies, collision snapshots, checkpoints, query
parameters and temporary RIDs are released outside timing. Explicit `Dispose`
and `FreeRID` cases time real prepared releases. Each row retains its own empty
loop, timer resolution, batch percentiles and owner/process managed bytes.

Setters time host return and exclude deferred simulation. Prepared scalar values
alternate where applicable; repeated skeleton requests and exception insertion
or removal can encounter already requested/present/absent state. Character
`MoveAndCollide` is test-only, `TestMove` reuses a result, and `MoveAndSlide` and
`ApplyFloorSnap` repeat at the settled floor with zero requested velocity. The
server motion and shape-collision cases require positive results. Getters observe
unchanged completed state; raw bodies have no scene collider object association.
These are defined samples of each method, not every branch or possible workload.

Resource-level collisions and immutable geometry use the same host algorithm
under both world selections. Parameterless `SpaceCreate()` always selects CPU.
Snapshot capture/apply is local to the prepared world with reusable buffers; it
does not measure a network transport. No selected-world speedup is implied for
backend-neutral operations. Do not sum the per-method rows into a frame estimate:
the complete `scene` frame already includes deferred work and synchronization.

The [per-method report](../../docs/components/physics-public-method-performance.md)
lists the measured families and the remaining process-registration,
extension-context and rendered-diagnostic entries. The
[complete-frame/query report](../../docs/components/physics-current-performance.md)
keeps hardware, source identities and qualification limits.
