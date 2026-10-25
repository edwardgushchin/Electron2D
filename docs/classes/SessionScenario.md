# SessionScenario

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [SessionScenario.cs](../../editor/Session/Jobs/SessionScenario.cs).
- **Declaration:** `public abstract class SessionScenario : IDisposable`

Compiled project extension points over the exact snapshot scene. The host owns root/context; the scenario owns its additional resources. All callbacks/disposal execute on the game engine owner thread in a fresh process. Trusted callbacks can have ordinary external side effects; this is not a rollback sandbox.

## Example

```csharp
sealed class Game : SessionScenario
{
    protected override void AfterStep(SessionRunContext context)
    {
        context.Check("root-live", !context.Root.IsDisposed, "Scene remains available.");
        if (context.Step == context.StepBudget) context.Complete();
    }
}
```

Partial snippet: imports Electron2D.Editor and uses SessionRunHost as the entry point. SessionProject.Create generates the full public consumer.

## Constructor and extension points

| Signature | Timing |
| --- | --- |
| `protected SessionScenario()` | Construct a definition without activation. |
| `protected virtual void Configure(SessionRunContext context)` | Cold factories/actions before scene load. |
| `protected virtual void SceneLoaded(SessionRunContext context)` | Loaded root before activation. |
| `protected virtual void BeforeStep(SessionRunContext context)` | After ordered input, before ordinary step callbacks. |
| `protected virtual void AfterStep(SessionRunContext context)` | Completed process step/native capture. |
| `public virtual void Dispose()` | Release scenario-owned resources after host scene/native cleanup. |

## Method descriptions

### SessionScenario

The protected constructor has no gameplay effect. The job factory returns a new derived instance for every run.

### Configure

context exposes immutable options and a seeded generator, Step=0. Root is unavailable and throws InvalidOperationException. Register the project's typed archive factories and InputMap actions here. Default implementation does nothing because no optional configuration is required.

### SceneLoaded

Root is borrowed and fully reconstructed with exact captured pose metadata. EnterTree/Ready have not run. Project setters/configuration may change this run's scene without mutating authoring. Default implementation needs no extra setup.

### BeforeStep

Step is one-based. All requested edges for this step have already committed through Input.ParseInputEvent. Ordinary process/physics callbacks follow with the fixed cadence and ordinary runtime ordering. Exceptions fail the run and attempt cleanup; a nonreturning callback is retired by the external job owner.

### AfterStep

Observes a completed frame, after real image capture in Rendered mode. Use context.Observe/Check and Complete. Reaching StepBudget without completion fails; an earlier deliberate Complete is allowed but any requested later captures still make the owning job fail validation.

### Dispose

Release additional scenario-owned resources; never dispose the borrowed context/root as an ownership transfer. The host separately attempts all cleanup stages even when a callback throws. A disposal failure prevents success. The default owns no extra resource.

## Verification and limits

[Compiled job consumers](../../tests/ProjectJobs/Program.cs) exercise unsaved scene/code, fixed steps and actual images. [Failure contracts](../../tests/ProjectJobs/JobContracts.cs) cover callback/assertion/budget failures and hung-callback retirement. No language service, hot reload, special game thread or actual agent acceptance is supplied.
