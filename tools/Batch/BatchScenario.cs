namespace Electron2D.Tools.Batch;

/// <summary>Defines compiled project configuration, a scene factory and scenario observations.</summary>
/// <remarks>The host invokes these methods on the engine owner thread. The controller process owns deadlines.
/// Authoring invokes Configure, creation/loading and Author without attaching the scene. Simulation invokes BeforeStep before
/// input-consuming callbacks and AfterStep after the completed process frame; rendered mode waits for actual capture.
/// Dispose must release scenario-owned resources; the host owns the root and context.</remarks>
public abstract class BatchScenario : IDisposable
{
    /// <summary>Creates a scenario definition without starting gameplay.</summary>
    protected BatchScenario() { }
    /// <summary>Registers project factories, input actions and other cold configuration.</summary>
    /// <param name="context">The run configuration; its root is not available yet.</param>
    protected virtual void Configure(BatchContext context) { }
    /// <summary>Constructs a detached root and its authored children.</summary>
    /// <param name="context">The run configuration and seeded random generator.</param>
    /// <returns>A fresh detached scene root. Rendered mode wraps a non-Window root in an ordinary native Window.</returns>
    protected abstract Node CreateScene(BatchContext context);
    /// <summary>Applies typed edits to the detached root before an authoring save.</summary>
    /// <param name="context">The loaded or newly created detached scene.</param>
    protected virtual void Author(BatchContext context) { }
    /// <summary>Prepares the next step after transport input state and delivery have committed.</summary>
    /// <param name="context">The current one-based step and borrowed root.</param>
    protected virtual void BeforeStep(BatchContext context) { }
    /// <summary>Observes the completed step and may request successful completion.</summary>
    /// <param name="context">The completed step, root and observations.</param>
    protected virtual void AfterStep(BatchContext context) { }
    /// <summary>Releases resources owned by the scenario; the host disposes its root separately.</summary>
    public virtual void Dispose() { }
    internal void Setup(BatchContext context) => Configure(context);
    internal Node Create(BatchContext context) => CreateScene(context);
    internal void AuthorScene(BatchContext context) => Author(context);
    internal void Before(BatchContext context) => BeforeStep(context);
    internal void After(BatchContext context) => AfterStep(context);
}
