namespace Electron2D;

public abstract partial class ElectronObject
{
    private Script? _constructionScript;

    /// <summary>Gets the borrowed source asset associated with this instance's compiled construction.</summary>
    /// <returns>The exact source asset, or null when construction had no Script association.</returns>
    /// <remarks>The association is available during the derived constructor and does not change when source text changes.
    /// Disposing the borrowed asset does not stop compiled callbacks; the returned identity may then be disposed.
    /// Copies and version-nine scene/resource archives retain the association through their resource ownership policy.
    /// Earlier archives retain null when no association was recorded. Live reassignment is a separate workflow.
    /// Prepared reads require no metadata discovery, native device, callback or managed allocation.</remarks>
    /// <exception cref="ObjectDisposedException">This owner is disposing on another thread or has finished disposing.</exception>
    public Script? GetScript() { ThrowIfDisposed(); return Volatile.Read(ref _constructionScript); }

    internal (Script Asset, Type Type)? ConstructionScript =>
        _constructionScript is { } script ? (script, GetType()) : null;

    internal bool RemapConstructionScript(Func<Resource, Resource> remap)
    {
        if (_constructionScript is { } script)
            return ReplaceConstructionScript(remap(script) as Script ?? throw new InvalidOperationException("A compiled source reference must remain a Script."));
        return false;
    }

    internal bool CopyConstructionScript(ElectronObject source) => ReplaceConstructionScript(source._constructionScript);

    private bool ReplaceConstructionScript(Script? source)
    {
        if (ReferenceEquals(_constructionScript, source)) return false;
        Volatile.Write(ref _constructionScript, source);
        return true;
    }

    internal void NotifyConstructionScriptChanged()
    {
        NotifyPropertyListChanged();
        if (!IsDisposed) NotifyScriptChanged();
    }
}
