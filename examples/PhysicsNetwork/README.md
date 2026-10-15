# PhysicsNetwork

An executable authoritative physics game using public Electron2D APIs. A CPU server
steps a headless scene at 60 Hz. Clients send numbered inputs, predict locally,
apply portable server snapshots and replay unconfirmed input. The verification run
starts an independent GPU client and a CPU client that joins later.

To run the same three-process scenario entirely on CPU without a graphics device,
select the first client's backend explicitly. Add `--threaded` to run each world's
numerical physics on its retained separate thread:

```sh
dotnet run --project examples/PhysicsNetwork -c Release -- --check --client-backend cpu --threaded --ticks 600 --output bin/physics-network-cpu-threaded
```

This writes `server.json`, `cpu.json`, `late.json` and their logs. Every process
must reject strict GPU startup before running CPU physics. Omit `--threaded` for
owner-thread numerical execution. The default GPU scenario remains required for
cross-backend verification; a CPU-only pass does not establish that result.

By default, `--check` coordinates coverage of the scripted impulse phases at ticks 240, 360
and each later multiple of 120. It waits for actual post-admission client steps
and current-epoch player input before advancing the authority, while continuing
packet polling and state delivery. Fixed physical steps and impulse ticks stay
unchanged. Twelve replay lead ticks plus two live prediction ticks fit inside the
existing input resend window. Headless clients accept `--client-poll-delay-ms`
from 0 through 250 to reproduce slow polling. For example:

```sh
dotnet run --project examples/PhysicsNetwork -c Release -- --check --client-backend cpu --threaded --ticks 600 --late-join-tick 310 --client-poll-delay-ms 250 --output bin/physics-network-slow
```

Add `--unpaced` to run the ordinary session without check-progress messages or
authority waits for client phases/inputs. For a headless slow-client check:

```sh
dotnet run --project examples/PhysicsNetwork -c Release -- --check --unpaced --client-backend cpu --threaded --ticks 900 --late-join-tick 310 --client-poll-delay-ms 250 --output bin/physics-network-unpaced
```

All physical steps retain the same geometry, contact, joint, impulse and allocation
assertions. Reports must show zero check-only traffic and waits. Ordinary clients
can skip server impulse ticks between snapshots; their reports must instead prove
actual sleeping-to-awake corrections for the coupled island and all five geometry
families. The final authority frame applies real wake impulses before publication,
so even a slow client must restore that active state. Startup controls reject a
missing awake snapshot. Default paced profiles still require every scripted phase,
local wake impulses and live retired-input replacement; both modes also run the
same deterministic future-input replacement controls. `ElapsedMS` measures each session from construction through report
capture, including network/physics/replay and final acknowledgements, before JSON
output. It is a wall-clock observation, separate from the physical-step samples.

Reports include the completed `CheckPhases` mask, separate phase/input wait
counts, wait milliseconds and attempted progress-message bytes, including the
deliberately invalid future-mask probes. Progress travels through the existing
impaired link and duplicate/reordered masks merge idempotently. The coordinated
check has a bounded deadline of simulated run duration plus 120 seconds, allowing
slow polling and software execution. The --unpaced check and ordinary server/client launches retain their
wall-clock drive and simulated-duration-plus-30-second deadline. Check timings
include deliberate authority waits and do not measure game throughput or FPS.

Future input from the current controller/incarnation replaces a queued command
from a retired control epoch or object generation at the same tick. Conflicting
commands in the same epoch remain rejected. Failure cleanup preserves child logs
before closing their streams.

`--ticks` extends the run without changing the lifecycle script; the default remains
360. `--late-join-tick 310 --ticks 600` reproduces admission after the server's
measurement threshold. Warm allocation samples require 128 actual completed steps
in each process as well as tick 300, and positive measured prediction/replay counts.
Cold replay allocations remain in the total replay counter. The CPU test-host
profiles use 600 ticks so late clients can complete their own warmup.
For admission after tick 300, the authority additionally sleeps the crate, wakes
it with an impulse and transfers player control after the second client joins.
The client must observe those transitions; the final torque remains at the selected
final tick.

```sh
dotnet run --project examples/PhysicsNetwork -c Release -- --check --output bin/physics-network-check
```

The check uses ephemeral loopback ports and writes `server.json`, `gpu.json`,
`late.json` and their logs. It runs real ENet connections across three processes.
The late client starts after the authority confirms tick 90, so startup speed cannot
turn this into simultaneous admission.
Application packets incur 20–99 ms delay, with an additional 150 ms on every
fifth non-dropped send; duplicates add 25 ms. Deterministic loss, duplication
and same-peer reordering occur before transport submission. The two CPU processes have display variables
removed and an unavailable Vulkan driver path; the server checks that it created
neither display nor rendering services. Both CPU processes explicitly attempt strict GPU startup and require rejection
before creating their CPU network worlds; the report records this proof. The GPU
client must actually select GPU.

For the test-project version, which additionally measures private backend transfer
and wait counters without exposing them to the example:

```sh
ELECTRON2D_TEST_PHYSICS_NETWORK=1 dotnet run --project tests/Electron2D.Tests -c Release
```

Use `ELECTRON2D_TEST_PHYSICS_NETWORK=cpu` or `cpu-threaded` for the instrumented
CPU-only profiles, and `gpu-threaded` for the separate-thread GPU profile. The test
host additionally records actual CPU numerical intervals and verifies their thread
against the requested policy. The public-only example reports the captured policy
without claiming those private measurements. GPU modes require an available device.

The scenario includes moving/jumping players, contact/Area events, a two-body pin, a finite groove and a damped spring with separate fixed anchors,
sleeping bodies, crate creation at tick 90, removal at 140, ID reuse with a new
generation at 160, a control-owner change at 240, and a verified sleeper waking at 300. Each client is deliberately
moved away from its predicted trajectory in position, rotation and angular velocity. The final authoritative state at tick 360
must agree across processes, including identities, generations, ownership, complete
pose, linear/angular velocity and sleep. An authority torque impulse at the final
tick requires a real nonzero angular state in that comparison.
Invalid session tokens, unowned input and old object generations are rejected.

Every solved authority, predicted and replayed tick checks actual pin anchors,
finite groove containment and bounded spring response. Small numbered impulses from tick
120 onward exercise continuation, including after late admission and coalesced corrections. Startup checks
independently disable each constraint's effort and require its trajectory guard to
fail. Reports retain joint-step/kick counts and maximum errors. Final topology
contains all three joints and their endpoints. These checks retain the existing
exact-once confirmed effects, adverse delivery and .002 final correction bounds.

A separate spring-loaded column combines a pin, a 100-unit groove, a spring and
real floor contact in the same connected island. Every physical step checks the
constraints and actual collider identity/normal; awake settled frames also check
support impulse against both bodies' weight and spring load. Numbered kicks from
tick 240 every 120 ticks must wake both bodies from natural sleep. Both clients
must restore actual floor contacts and sleeping state from server corrections.
Startup negative controls remove the floor or spring load and require rejection.
The final topology now contains 28 objects within 32 prepared slots. Command history
uses 262144 bytes per process, excluding array headers. The example's application
wire is version 4 and rejects previous recipe/slot layouts; the engine portable
snapshot format is unchanged.

The additional geometry island includes an infinite boundary, a two-segment concave
ledge, a rotated capsule, an asymmetric convex polygon, a directed separation ray
and a rotated two-slot circle/rectangle body. Shape CCD applies to the capsule,
ray and compound body; the polygon selects ray CCD. The ray locks rotation while
the other finite bodies rotate freely. Numbered impulses wake them
every 120 ticks from tick 240. Every authority/prediction/replay step checks finite
motion, actual world-space support bounds, contact identities and local shape
indices. Reports require all five moving families to produce real impulses, sleep,
wake and restore contacts/sleep from authority corrections. Startup controls remove
each support and require the trajectory check to fail. The display draws their
actual public Shape geometry; headless checks establish no rendered/window gate.

A second, ray-only body retains free rotation with explicit inertia 2 and local
angular damping 4. It starts at .3 radians and receives alternating torque impulses
of magnitude 2 on the same numbered wake ticks. The original locked ray remains.
Checks require actual angular motion and restored moving angular state on each
client. Both compound slots must independently produce contacts and positive
impulses; restored contacts must include both indices and match the authored
left-circle/right-box geometry. Startup controls disable each slot and lock the
free ray, requiring the corresponding evidence to fail. Headers one through three
are rejected before message decoding.

`NetworkWorld` owns the game scene and lifecycle recipes. `NetworkSession` owns
bounded command/event histories and peer policy. `NetworkSession.State` applies
snapshots, replays input, confirms each event once, and computes remote interpolation
and local correction smoothing separately from solved poses. `Wire` owns the typed
bounded game format and deterministic network impairment. `ProcessCheck` launches
and verifies the separate processes; it does not simulate networking in memory.

This is a scripted headless example, with presentation coordinates verified
numerically. It opens no game window and makes no rendered/FPS claim. The admission
token and loopback binding are a local demonstration, not an Internet security
protocol. Geometry recipes and lifecycle policy are deliberately application-owned;
this example is not a generic scene serializer or a production matchmaking service.
See [the contract and measured limits](../../docs/components/physics-network-example.md).

Run the GPU client in a real window while the authority and late CPU client remain
headless:

```sh
dotnet run --project examples/PhysicsNetwork -c Release -- --check --window-client --renderer gpu --output bin/physics-network-window
```

Use `--renderer compatibility` to choose the other renderer with the same GPU
physics. A separately launched client accepts `--window`, `--renderer` and
`--capture path.png`. The game retains scripted numbered input and authority
correction. White dots are displayed positions; orange dots are current solved
positions when a local correction visibly separates them. The view fits all actors
without moving physical nodes. `gpu-window.png` captures a verified frame with
identical complete portable physics before/after drawing. Reports include actual
window FPS/frame percentiles, window allocations and rendered interpolation/
smoothing counts, separately from physics/replay metrics.

## NativeAOT on Linux

Publish the complete native output directory and run its executable directly:

```sh
dotnet publish examples/PhysicsNetwork/PhysicsNetwork.csproj -c Release -r linux-x64 -p:PublishAot=true -p:ILLinkTreatWarningsAsErrors=true -p:IlcTreatWarningsAsErrors=true -o bin/physics-network-native
bin/physics-network-native/PhysicsNetwork --check --client-backend cpu --ticks 900 --late-join-tick 310 --output bin/physics-network-native-check
```

Keep the complete published directory intact. The parent launches
its server and clients from the same application, both for direct execution and
for a framework-dependent DLL launched through `dotnet`. Use `--client-backend gpu`
for a GPU early client and add `--threaded` for separate numerical execution;
CPU authority and late CPU client still require denied GPU startup. These checks
retain the ordinary physical, protocol, event, convergence and warm allocation
assertions. Platform and device acceptance must be recorded for the actual run.
