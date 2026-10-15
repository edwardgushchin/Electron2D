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
Application packets incur 20–99 ms delay, deterministic loss, duplication and
reordering before transport submission. The two CPU processes have display variables
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
The final topology now contains 21 objects within 32 prepared slots. Command history
uses 262144 bytes per process, excluding array headers. The example's application
wire is version 2 and rejects previous recipe/slot layouts; the engine portable
snapshot format is unchanged.

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
