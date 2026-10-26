namespace Electron2D;

public sealed partial class SkeletonModificationStack
{
    internal bool HasEditorGizmos
    {
        get
        {
            lock (_gate)
            {
                Read();
                if (!_setup) return false;
                foreach (var mod in _mods) if (mod is { IsDisposed: false, EditorDrawGizmo: true }) return true;
                return false;
            }
        }
    }

    internal void DrawEditorGizmos()
    {
        var owner = GetSkeleton(); owner?.Tree?.EnsureOwnerThread();
        lock (_gate)
        {
            Read();
            if (!_setup || owner is not { IsInsideTree: true }) return;
            if (_executing || _preparing) throw new InvalidOperationException("Editor gizmo drawing cannot reenter a running stack.");
            if (_executeDepth == 128) throw new InvalidOperationException("Stack nesting exceeds 128 levels.");
            _executing = true; _executeDepth++;
        }
        try
        {
            foreach (var mod in _mods)
                if (mod is { IsDisposed: false }) mod.DrawEditorGizmo(this);
        }
        finally
        {
            lock (_gate) { _executing = false; _executeDepth--; }
            owner.DrawSetTransformMatrix(Transform.Identity);
        }
    }
}
