using System.Runtime.InteropServices;

namespace Electron2D;

public sealed class SceneTree
{
    private Node[] _freeQueue;
    private int _freeCount;
    private readonly GroupIndex _groups = new();

    public SceneTree(Node root, int deferredFreeQueueCapacity  = 1024)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        if (deferredFreeQueueCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(deferredFreeQueueCapacity));

        _freeQueue = new Node[deferredFreeQueueCapacity];

        Root.InternalEnterTree(this);
        Root.InternalReady();
    }

    public Node Root { get; }

    public bool Paused { get; set; }

    public ReadOnlySpan<Node> GetNodesInGroup(string group) => _groups.GetNodes(group);

    internal int RegisterInGroup(string group, Node node) => _groups.Add(group, node);

    internal void UnregisterFromGroup(string group, int index, Node removedNode)
        => _groups.Remove(group, index, removedNode);

    internal void QueueFree(Node node)
    {
        if ((uint)_freeCount >= (uint)_freeQueue.Length)
            throw new InvalidOperationException("SceneTree deferred free queue overflow. Increase maxDeferredFreePerFrame.");

        _freeQueue[_freeCount++] = node;
    }

    public void FlushFreeQueue()
    {
        for (var i = 0; i < _freeCount; i++)
        {
            var n = _freeQueue[i];
            _freeQueue[i] = null!;
            n.InternalFreeImmediate();
        }
        _freeCount = 0;
    }

    public void Process(float delta)
        => ProcessNode(Root, parentMode: ProcessMode.Always, delta);

    public void PhysicsProcess(float fixedDelta)
        => PhysicsProcessNode(Root, parentMode: ProcessMode.Always, fixedDelta);

    private void ProcessNode(Node node, ProcessMode parentMode, float delta)
    {
        var mode = node.ProcessMode == ProcessMode.Inherit ? parentMode : node.ProcessMode;
        if (ShouldRun(mode))
            node.InternalProcess(delta);

        var count = node.GetChildCount();
        for (var i = 0; i < count; i++)
            ProcessNode(node.GetChild(i), mode, delta);
    }

    private void PhysicsProcessNode(Node node, ProcessMode parentMode, float fixedDelta)
    {
        var mode = node.ProcessMode == ProcessMode.Inherit ? parentMode : node.ProcessMode;
        if (ShouldRun(mode))
            node.InternalPhysicsProcess(fixedDelta);

        var count = node.GetChildCount();
        for (var i = 0; i < count; i++)
            PhysicsProcessNode(node.GetChild(i), mode, fixedDelta);
    }

    private bool ShouldRun(ProcessMode mode)
    {
        if (mode == ProcessMode.Disable) return false;
        if (mode == ProcessMode.Always) return true;
        if (mode == ProcessMode.Pausable) return !Paused;
        if (mode == ProcessMode.WhenPaused) return Paused;
        return true;
    }
}
