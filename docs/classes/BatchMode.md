# BatchMode

Last updated: 2026-10-20

- **Assembly:** `Electron2D.Batch` development tool; outside the runtime API census.
- **Namespace:** `Electron2D.Tools.Batch`
- **Source:** [Protocol.cs](../../tools/Batch/Protocol.cs)
- **Declaration:** `public enum BatchMode`

Selects one [batch capability profile](../components/project-batch.md#three-modes). Authoring=0 saves a detached scene without gameplay activation. Headless=1 uses the ordinary manual scene lifecycle without native presentation. Rendered=2 uses an actual native renderer and completed-frame movie capture. JSON uses the enum names; undefined values reject. The enum does not identify hardware or establish platform acceptance.

```csharp
if (context.Mode == BatchMode.Rendered)
    context.Observe("native-run", true);
```

The snippet requires an active BatchContext. Mode is immutable for a job; capture in another mode fails explicitly. See [the executable verification scope](../components/project-batch.md#executable-verification-and-limits).
