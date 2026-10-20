# BatchHost

Last updated: 2026-10-20

- **Assembly:** `Electron2D.Batch` development tool; outside `Electron2D.dll`.
- **Namespace:** `Electron2D.Tools.Batch`
- **Source:** [BatchHost.cs](../../tools/Batch/BatchHost.cs)
- **Declaration:** `public static class BatchHost`

Hosts one compiled scenario process launched through the [project controller](../components/project-batch.md). It uses public runtime services for scene files, scheduling, input and actual rendered capture. There is no reflected method invocation or runtime dependency on a game assembly.

```csharp
// Program.cs in a CLI-created scenario project:
return BatchHost.Run(args, static () => new Scenario());
```

`Scenario` derives from [BatchScenario](BatchScenario.md); the generated project is a complete compiling example.

## Methods

| Signature | Contract |
| --- | --- |
| `public static int Run(string[] args, Func<BatchScenario> factory)` | Executes one controller request and returns its explicit protocol exit status. |

### Run

Arguments are the controller marker, preserved request path, new output directory and job nonce. Null arguments/factory throw ArgumentNullException. Invalid launch arguments return 2 without starting the engine. The factory returns a fresh scenario; a null result or callback exception is a failure. All callbacks execute on the current engine owner thread. The host owns the valid detached source root, any native wrapper, context generator and writer; the scenario owns its other resources. Invalid already-attached factory roots are rejected before ownership transfer. Cleanup attempts every owned stage and preserves failure diagnostics.

Authoring saves an explicitly selected scene or a new `scene.e2dscene`, without host-driven gameplay. Headless activates a real SceneTree with a non-Window root. Rendered consumes the source Window or wraps another source root in a native Window. Source viewport content preserves ordinary routing/composition rules. Scene save/load and captured frames use existing public APIs. Every simulation needs explicit Complete; exhausted budgets and premature quit fail.

`run.json` is written atomically after cleanup. The controller validates its nonce, request/assembly identity and process exit status before producing `report.json`. Directly calling this SDK entry point does not provide process isolation or interrupt a hung callback; use the CLI for deadlines and cancellation. Its current process-group job owner supports Linux; other controller platforms reject run until their job lifecycle exists. The controller protocol, limits, verified cases and current platform scope are defined in [project batch execution](../components/project-batch.md).
