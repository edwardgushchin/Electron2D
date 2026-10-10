# PhysicsNetwork

An executable authoritative physics game using public Electron2D APIs. A CPU server
steps a headless scene at 60 Hz. Clients send numbered inputs, predict locally,
apply portable server snapshots and replay unconfirmed input. The verification run
starts an independent GPU client and a CPU client that joins later.

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
neither display nor rendering services. The GPU client must actually select GPU.

For the test-project version, which additionally measures private backend transfer
and wait counters without exposing them to the example:

```sh
ELECTRON2D_TEST_PHYSICS_NETWORK=1 dotnet run --project tests/Electron2D.Tests -c Release
```

The scenario includes moving/jumping players, contact/Area events, a two-body pin,
sleeping bodies, crate creation at tick 90, removal at 140, ID reuse with a new
generation at 160, a control-owner change at 240, and a verified sleeper waking at 300. Each client is deliberately
moved away from its predicted trajectory. The final authoritative state at tick 360
must agree across processes, including identities, generations, ownership and sleep.
Invalid session tokens, unowned input and old object generations are rejected.

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
