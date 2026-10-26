namespace Electron2D.Editor;

/// <summary>Copied owner-local selection context; it does not mutate or save a scene.</summary>
/// <param name="Revision">Monotonic selection token for this owner lifetime.</param>
/// <param name="NodeID">Selected stable scene object, or empty for no selection.</param>
public sealed record SessionSelection(ulong Revision, Guid NodeID);

public sealed partial class ProjectSession
{
    private Guid _selectedNode;
    private ulong _selectionRevision = 1;
    private void ReconcileSelection()
    {
        if (_selectedNode != Guid.Empty && !_state.Nodes.Any(n => n.ID == _selectedNode))
        { _selectedNode = Guid.Empty; _selectionRevision = checked(_selectionRevision + 1); }
    }
    internal SessionSelection Selection()
    {
        Check(); FlushCodeBuffer(); ReconcileSelection(); return new(_selectionRevision, _selectedNode);
    }
    internal SessionResult Select(string origin, Guid nodeID, ulong sceneRevision, ulong selectionRevision)
    {
        Begin();
        try
        {
            Expected(SessionDocumentKind.Scene, sceneRevision); ReconcileSelection();
            if (_selectionRevision != selectionRevision) throw new InvalidOperationException("Selection changed; inspect current context before replacing it.");
            if (nodeID != Guid.Empty && !_state.Nodes.Any(n => n.ID == nodeID)) throw new ArgumentException("The scene object no longer exists.", nameof(nodeID));
            if (_selectedNode != nodeID) { _selectionRevision = checked(_selectionRevision + 1); _selectedNode = nodeID; }
            return Result(Guid.Empty, SessionChangeKind.Selected, origin, false);
        }
        finally { _busy = false; }
    }
}
