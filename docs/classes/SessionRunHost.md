# SessionRunHost

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [SessionRunHost.cs](../../editor/Session/Jobs/SessionRunHost.cs).
- **Declaration:** `public static class SessionRunHost`

Hosts a fresh compiled scenario for an immutable session job. It uses ordinary public Engine lifecycle, typed archives/input and native movie capture. It does not supervise/isolate its own process; [SessionJob](SessionJob.md) supplies that boundary. Calling it directly cannot stop a nonreturning callback.

## Example

```csharp
return SessionRunHost.Run(args, () => new Game());
```

Partial project entry point: Game derives from SessionScenario, args comes from the owning job. SessionProject.Create generates a complete buildable example. Arbitrary normal application arguments reject explicitly.

## Public method

`public static int Run(string[] args, Func<SessionScenario> factory)`

### Run

args must be the exact private version-two launch tuple supplied by the owner. factory creates a fresh scenario on the calling engine owner thread. Null arguments reject. An active Engine.MainLoop rejects; a child requires an idle engine. The host verifies snapshot inputs, records exact request/binary hashes, configures factories/actions, loads the typed scene with exact pose overlay, calls SceneLoaded and then starts the chosen headless/native lifecycle. BeforeStep follows committed ordered input; AfterStep follows completed processing/capture. Explicit Complete is required before budget exhaustion. Rendered output requires the explicit backend without fallback.

All cleanup stages are attempted and their failures remain diagnostics. Structured run.json is separate from program logs. Returns zero only for completed validated simulation and cleanup. Invalid request/input is code 2, runtime/protocol/callback failure 4, assertion 5, exhausted budget 6, and an absent capability 7; the owning job adds build 3, deadline 124 and cancellation 130. A failed assertion remains failed even if project code catches its thrown exception. Code 4 also reports inability to save a result, through stderr when JSON cannot be written.

See [the job flow](../components/project-jobs.md#build-run-cancel-and-cleanup), [SessionScenario](SessionScenario.md), [SessionRunContext](SessionRunContext.md) and [public tests](../../tests/ProjectJobs/JobContracts.cs). Actual native captures use Linux software Vulkan/OpenGL; other platforms, physical GPU/native allocation and actual agent acceptance remain separate.
