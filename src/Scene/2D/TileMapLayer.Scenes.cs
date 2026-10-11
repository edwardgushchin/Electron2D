namespace Electron2D;

public partial class TileMapLayer
{
    private readonly Dictionary<Vector2i, (Cell Cell, Node Root)> _sceneCells = [];
    private bool _sceneAllDirty = true;
    private TileSetSource? ResolveSource(Cell cell) => _tileSet is { IsDisposed: false } && _tileSet.HasSource(cell.SourceID) &&
        _tileSet.GetSource(cell.SourceID) is { IsDisposed: false } source && source.HasTile(cell.Atlas) && source.HasAlternativeTile(cell.Atlas, cell.Alternative) ? source : null;
    private void UpdateSceneCells(Vector2i[] changed, bool force)
    {
        if (!IsInsideTree || !_enabled || _tileSet is null) { ReleaseSceneCells(); return; }
        foreach (var coords in changed)
        {
            if (IsDisposed || !IsInsideTree) return;
            _sceneCells.TryGetValue(coords, out var old); var previous = old.Root;
            if (!_cells.TryGetValue(coords, out var cell) || ResolveSource(cell) is not TileSetScenesCollectionSource source || source.SceneForCell(cell.Alternative) is not { } packed)
            { _sceneCells.Remove(coords); RetireScene(previous); continue; }
            if (!force && old.Cell == cell && previous is { IsDisposed: false, IsQueuedForDeletion: false } && ReferenceEquals(previous.Parent, this) && ReferenceEquals(previous.SpawnSceneIdentity, packed)) continue;
            for (Node? ancestor = this; ancestor is not null; ancestor = ancestor.Parent)
                if (ReferenceEquals(ancestor.SpawnSceneIdentity, packed)) throw new InvalidOperationException("A scene tile cannot recursively instantiate its own ancestor template.");
            Node? prepared = null;
            try
            {
                prepared = packed.Instantiate();
                if (prepared is not CanvasItem) throw new InvalidOperationException("The scene tile template no longer has a canvas root.");
                var position = MapToLocal(coords);
                if (prepared is Entity entity)
                    entity.Transform = new Transform(CellPoint(Vector2.Right, cell.Alternative), CellPoint(Vector2.Down, cell.Alternative), position) * entity.Transform;
                else if (prepared is Control control) control.Position += position;
                if (IsDisposed || !IsInsideTree || !_enabled || !_cells.TryGetValue(coords, out var current) || current != cell)
                { prepared.Dispose(); prepared = null; continue; }
                prepared.Name = SceneCellName(coords);
                AddChild(prepared);
                if (IsDisposed || !IsInsideTree || !_enabled || prepared.IsDisposed || !ReferenceEquals(prepared.Parent, this) || !_cells.TryGetValue(coords, out current) || current != cell)
                { RetireScene(prepared); prepared = null; continue; }
                _sceneCells[coords] = (cell, prepared); prepared = null; RetireScene(previous);
            }
            catch (Exception error) { if (prepared is { IsDisposed: false }) { try { prepared.Dispose(); } catch (Exception cleanup) { throw new AggregateException(error, cleanup); } } throw; }
        }
    }
    private string SceneCellName(Vector2i coords)
    {
        var name = $"SceneTile_{coords.X}_{coords.Y}"; var candidate = name; var suffix = 1;
        while (Children.Any(n => n.Name == candidate)) candidate = name + "_" + suffix++;
        return candidate;
    }
    private void RetireScene(Node? node)
    { if (node is { IsDisposed: false } && ReferenceEquals(node.Parent, this)) node.QueueFree(); }
    private void ReleaseSceneCells(bool exiting = false)
    {
        foreach (var placed in _sceneCells.Values)
        {
            var node = placed.Root;
            if (node is { IsDisposed: false } && ReferenceEquals(node.Parent, this))
            { if (exiting) { if (Tree is { IsDisposed: false } tree) tree.QueueDelete(node); } else node.QueueFree(); }
        }
        _sceneCells.Clear();
    }
}
