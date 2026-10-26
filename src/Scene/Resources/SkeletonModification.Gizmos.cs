namespace Electron2D;

public abstract partial class SkeletonModification
{
    private bool _editorDrawGizmo;

    /// <summary>Gets or sets whether the bound editor canvas records this modification's gizmo.</summary>
    /// <value>False in the base type; LookAt, TwoBoneIK and CCDIK enable drawing initially.</value>
    /// <remarks>Stored authoring data. Drawing requires an edited-scene owner and editor hint; this flag does
    /// not enable gameplay processing. Changes appear on the next visible editor frame.</remarks>
    /// <exception cref="InvalidOperationException">An attached write is off the scene owner.</exception>
    /// <exception cref="ObjectDisposedException">The modification is disposed.</exception>
    public bool EditorDrawGizmo
    {
        get { lock (ModificationGate) { ThrowIfDisposed(); return _editorDrawGizmo; } }
        set { lock (ModificationGate) { EnsureModificationMutable(); _editorDrawGizmo = value; } EmitChanged(); }
    }

    /// <summary>Records editor-only commands through the bound stack's Skeleton drawing methods.</summary>
    /// <remarks>Runs synchronously on the scene owner inside the skeleton's canvas recording scope. The
    /// stack and modification remain borrowed; reentry, stack replacement and disposal reject. Exceptions
    /// discard partial commands and retain a redraw retry. No numerical execution or pose change is implied.</remarks>
    protected virtual void OnDrawEditorGizmo() { }

    internal void DrawEditorGizmo(SkeletonModificationStack stack)
    {
        lock (ModificationGate)
        {
            ThrowIfDisposed();
            if (!_editorDrawGizmo || !ReferenceEquals(Stack(), stack)) return;
        }
        OnDrawEditorGizmo();
    }
}
