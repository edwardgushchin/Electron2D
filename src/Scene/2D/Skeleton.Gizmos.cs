namespace Electron2D;

public partial class Skeleton
{
    private bool _editorGizmosVisible;
    private bool IsEditorGizmoOwner => Engine.IsEditorHint && Tree?.IsEditedSceneMember(this) == true;

    internal void RefreshEditorGizmos()
    {
        var visible = IsEditorGizmoOwner && _stack is { IsDisposed: false, HasEditorGizmos: true };
        if (visible || _editorGizmosVisible) QueueRedraw();
        _editorGizmosVisible = visible;
    }

    private void DrawEditorGizmos()
    {
        if (!_editorGizmosVisible || !IsEditorGizmoOwner || _stack is not { IsDisposed: false }) return;
        EnsureSetup();
        if (_executing) throw new InvalidOperationException("Skeleton editor drawing cannot reenter execution.");
        _executing = true;
        try { _stack.DrawEditorGizmos(); }
        finally { _executing = false; }
    }

    /// <summary>Rejects skeleton disposal during its borrowed numerical or editor callback.</summary>
    /// <remarks>Validation runs before the terminal disposal transition.</remarks>
    protected override void ValidateDisposal()
    {
        if (_executing) throw new InvalidOperationException("A running skeleton cannot be disposed.");
        base.ValidateDisposal();
    }
}
