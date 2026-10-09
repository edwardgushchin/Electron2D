# PhysicsServer.Backend

Last updated: 2026-10-09

- Declaration: `public enum PhysicsServer.Backend`
- Source: [PhysicsServer.Backends.cs](../../src/Servers/Physics/PhysicsServer.Backends.cs)
- Component: [Physics backend selection](../components/physics-backends.md)

Selects physics independently of rendering. CPU remains available on machines with
a GPU. A world retains its initial choice; replacing Viewport.World uses the normal
body and joint detach/reattach lifecycle rather than silently migrating a solver.

| Value | Number | Behavior |
| --- | ---: | --- |
| `CPU` | 0 | Managed Box2D.NET world. No renderer, window or compute device is needed. |
| `GPU` | 1 | Independent resident compute world. No Box2D world or CPU solver is created. |

Use `PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU, allowCPUFallback: true)`
for a caller-owned server space or `new World(PhysicsServer.Backend.GPU, true)`
for a viewport world. Parameterless factories retain CPU behavior. Invalid enum
values reject before allocation. Startup fallback is separately allowed by the
caller; requested and actual backend plus the fallback diagnostic remain readable.

This enum does not imply completed cross-platform or networking acceptance. See
the component's exercised capabilities and remaining requirements.
