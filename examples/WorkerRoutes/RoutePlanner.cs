namespace Electron2D.Examples.WorkerRoutes;

internal sealed class RoutePlanner : Entity
{
    private readonly AStarGrid[] _grids = new AStarGrid[4];
    private readonly Vector2[]?[] _routes = new Vector2[4][];
    private readonly Vector2[] _pending = new Vector2[4], _positions = new Vector2[4];
    private readonly Action[] _publish = new Action[4];
    private readonly Action<int> _compute;
    private static readonly Color[] RouteColors = [Colors.Cyan, Colors.Yellow, Colors.Green, Colors.White];
    private SceneTree? _tree;
    private float _phase;
    internal int PublishedCount { get; private set; }
    internal int JobBatches { get; private set; }
    internal int OwnerThreadID { get; private set; }
    internal RoutePlanner()
    {
        ProcessEnabled = true; _compute = Compute;
        for (var i = 0; i < _grids.Length; i++)
        {
            _grids[i] = new() { Region = new(0, 0, 28, 28), CellSize = new(2, 2), Offset = new(8, 8) }; _grids[i].Update();
            var index = i; _publish[i] = () => { if (Environment.CurrentManagedThreadId != OwnerThreadID) throw new InvalidOperationException("Publication requires the scene owner."); _positions[index] = _pending[index]; PublishedCount++; QueueRedraw(); };
        }
    }
    protected override void OnReady() { _tree = Tree!; OwnerThreadID = Environment.CurrentManagedThreadId; }
    protected override void OnProcess(double delta)
    {
        _phase += (float)delta;
        var group = WorkerThreadPool.AddGroupTask(_compute, _grids.Length, highPriority: true, description: "route positions");
        WorkerThreadPool.WaitForGroupTaskCompletion(group); JobBatches++;
    }
    private void Compute(int index)
    {
        var route = _routes[index] ??= _grids[index].GetPointPath(new(1, index * 4 + 2), new(23, index * 4 + 2));
        _pending[index] = route[(int)(_phase * 12) % route.Length]; _tree!.Defer(_publish[index]);
    }
    protected override void OnDraw()
    {
        for (var i = 0; i < _routes.Length; i++) if (_routes[i] is { } route)
            {
                DrawPolyline(route, RouteColors[i], 2); DrawRect(new(route[^1] - Vector2.One, new(3, 3)), RouteColors[i]); DrawRect(new(_positions[i] - Vector2.One, new(3, 3)), Colors.White);
            }
    }
    protected override void Dispose(bool disposing)
    {
        foreach (var grid in _grids) grid.Dispose(); base.Dispose(disposing);
    }
}
