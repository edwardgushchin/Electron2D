# BatchScenario

Last updated: 2026-10-20

- **Assembly:** `Electron2D.Batch` development tool.
- **Namespace:** `Electron2D.Tools.Batch`
- **Source:** [BatchScenario.cs](../../tools/Batch/BatchScenario.cs)
- **Declaration:** `public abstract class BatchScenario : IDisposable`

A compiled scenario supplies typed project setup, a detached scene factory, optional authoring edits and simulation observations. The controller launches a fresh project process for each job. Register portable custom node/resource factories in Configure before either creation or file loading. Custom nodes also provide their ordinary scene-instance factory override. The generated [scenario template](../../tools/Batch/Templates/Scenario.cs) is a complete compiling example with real input, physics callbacks, drawing, assertions and persistence.

## API

| Signature | Timing |
| --- | --- |
| `protected BatchScenario()` | Creates an inactive definition. |
| `protected virtual void Configure(BatchContext context)` | Once before root creation/loading; register schemas and input actions. |
| `protected abstract Node CreateScene(BatchContext context)` | Creates a fresh detached root when the request does not select a scene file. |
| `protected virtual void Author(BatchContext context)` | Authoring only, after root creation/loading and before SaveScene. |
| `protected virtual void BeforeStep(BatchContext context)` | After transport input commits/delivers, before the ordinary physics/process lanes. |
| `protected virtual void AfterStep(BatchContext context)` | After a successful process step; rendered mode also waits for its actual completed image. |
| `public virtual void Dispose()` | After owned scene/native cleanup; release scenario-owned resources. |

### Factory and authoring

CreateScene returns a live parentless unqueued node. Returning an attached/disposed/queued object rejects before host ownership. A Window requires native rendered activation; ordinary Node roots support headless execution. Other source roots are wrapped below an ordinary Window for rendered mode, with their normal viewport/input composition preserved. Factory and Author code must retain the detached authoring contract; creating gameplay/native services manually is outside that contract. Stored ownership, custom factories and descriptors follow the ordinary serializer rules.

### Step callbacks and completion

The borrowed [context](BatchContext.md) exposes the one-based attempted step, fixed cadence and source root. BeforeStep may send additional typed input through the public Input API. AfterStep records assertions/observations and calls Complete once its outcome is established. Callback exceptions abort the scenario. Early scene quit and budget exhaustion cannot imply success. No extra warmup simulation is inserted. Real-time/cross-platform bit identity is not promised.

### Disposal

Override Dispose for resources the scenario acquired outside the root. The host disposes its own root and generator separately and attempts all cleanup stages even after errors. A throwing factory remains responsible for objects it acquired without returning them. Callbacks and reporting require the job owner thread. See [verification and limits](../components/project-batch.md#executable-verification-and-limits).
