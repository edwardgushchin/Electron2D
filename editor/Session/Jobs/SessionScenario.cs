namespace Electron2D.Editor;

/// <summary>Defines compiled project configuration and observations over the loaded snapshot scene.</summary>
/// <remarks>The host invokes these methods on the engine owner thread. The controller process owns deadlines.
/// The host loads the selected immutable scene after Configure and then invokes SceneLoaded. Simulation invokes BeforeStep before
/// input-consuming callbacks and AfterStep after the completed process frame; rendered mode waits for actual capture.
/// Dispose must release scenario-owned resources; the host owns the root and context.</remarks>
public abstract class SessionScenario : IDisposable
{
    /// <summary>Creates a scenario definition without starting gameplay.</summary>
    protected SessionScenario() { }
    /// <summary>Registers project factories, input actions and other cold configuration.</summary>
    /// <param name="context">The run configuration; its root is not available yet.</param>
    protected virtual void Configure(SessionRunContext context) { }
    /// <summary>Configures the loaded snapshot scene before ordinary lifecycle activation.</summary>
    /// <param name="context">The borrowed root and immutable run configuration.</param>
    protected virtual void SceneLoaded(SessionRunContext context) { }
    /// <summary>Prepares the next step after transport input state and delivery have committed.</summary>
    /// <param name="context">The current one-based step and borrowed root.</param>
    protected virtual void BeforeStep(SessionRunContext context) { }
    /// <summary>Observes the completed step and may request successful completion.</summary>
    /// <param name="context">The completed step, root and observations.</param>
    protected virtual void AfterStep(SessionRunContext context) { }
    /// <summary>Releases resources owned by the scenario; the host disposes its root separately.</summary>
    public virtual void Dispose() { }
    internal void Setup(SessionRunContext context) => Configure(context);
    internal void Loaded(SessionRunContext context) => SceneLoaded(context);
    internal void Before(SessionRunContext context) => BeforeStep(context);
    internal void After(SessionRunContext context) => AfterStep(context);
}
