namespace Electron2D;

public partial class SceneTree
{
    [ThreadStatic] internal static ProcessingGroup? CurrentProcessingGroup;
    [System.Runtime.CompilerServices.ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255", Justification = "Prepare per-thread group storage before pooled workers accept jobs.")]
    internal static void RegisterWorkerThreadPreparation() => WorkerPoolRuntime.InitializeThread += static () => CurrentProcessingGroup = null;

    private readonly Dictionary<ulong, ProcessingGroup> _processingGroups = [];
    private static readonly Comparison<GroupEntry> ProcessingGroupOrder = static (left, right) => left.CompareTo(right);
    private static readonly Comparison<ScheduledNode> ProcessingNodeOrder = static (left, right) => left.CompareTo(right);
    private readonly List<GroupEntry> _groupFrame = [];
    private readonly List<ProcessingGroup> _groupStage = [];
    private readonly List<CanvasItem> _transformPublications = [];
    internal enum InterpolationPublicationKind { CanvasPolicy, CameraPolicy, CameraScroll, ViewportReset }
    private readonly record struct InterpolationPublication(Node Node, InterpolationPublicationKind Kind, Vector2 Position = default, float Rotation = 0);
    private readonly List<InterpolationPublication> _interpolationPublications = [];
    internal void QueueInterpolationPublication(Node node, InterpolationPublicationKind kind)
    {
        lock (_workGate) _interpolationPublications.Add(new(node, kind));
    }
    internal void QueueCameraScroll(Camera camera, Vector2 position, float rotation)
    {
        lock (_workGate)
        {
            _interpolationPublications.Add(new(camera, InterpolationPublicationKind.CameraScroll, position, rotation));
            if (!_transformPublications.Contains(camera)) _transformPublications.Add(camera);
        }
    }
    private Action<int>? _runGroupWorker;
    private bool _groupPhysics;
    private volatile bool _processingWorkerStage;
    internal bool IsProcessingWorkerStage => _processingWorkerStage;
    internal void QueueTransformPublication(CanvasItem item)
    {
        lock (_workGate)
            if (!_transformPublications.Contains(item)) _transformPublications.Add(item);
    }
    private void PublishProcessingTransforms(ref List<Exception>? errors)
    {
        try
        {
            foreach (var publication in _interpolationPublications)
            {
                var node = publication.Node;
                if (node.IsDisposed || !ReferenceEquals(node.Tree, this)) continue;
                try
                {
                    switch (publication.Kind)
                    {
                        case InterpolationPublicationKind.CanvasPolicy: ((CanvasItem)node).PublishInterpolationPolicy(); break;
                        case InterpolationPublicationKind.CameraPolicy: ((Camera)node).RefreshInterpolationProcessing(); break;
                        case InterpolationPublicationKind.CameraScroll: ((Camera)node).PublishScroll(publication.Position, publication.Rotation); break;
                        case InterpolationPublicationKind.ViewportReset: ((Viewport)node).ResetCanvasInterpolationSnapshot(); break;
                    }
                }
                catch (Exception error) { CollectException(ref errors, error); }
            }
            foreach (var item in _transformPublications)
            {
                if (item.IsDisposed || !ReferenceEquals(item.Tree, this)) continue;
                try
                {
                    if (item is Camera camera) camera.PublishFinalScroll();
                    else if (item is TileMapLayer tiles) tiles.PublishSceneTransform();
                }
                catch (Exception error) { CollectException(ref errors, error); }
            }
        }
        finally { _interpolationPublications.Clear(); _transformPublications.Clear(); }
    }
    private double _groupUnscaledDelta;
    private ulong _groupTreeOrderPath = ulong.MaxValue;
    private ProcessingGroup DefaultProcessingGroup
    {
        get
        {
            if (_processingGroups.TryGetValue(0, out var group)) return group;
            group = new(this, default); _processingGroups.Add(0, group); return group;
        }
    }

    internal sealed class ProcessingGroup(SceneTree tree, ObjectIdentity owner)
    {
        internal readonly SceneTree Tree = tree;
        internal readonly ObjectIdentity Owner = owner;
        internal readonly Queue<Action> Messages = [];
        internal readonly List<Node> Members = [];
        internal readonly List<ScheduledNode> IdleSchedule = [], PhysicsSchedule = [];
        internal ulong IdleRevision = ulong.MaxValue, PhysicsRevision = ulong.MaxValue;
        internal ulong IdlePath = ulong.MaxValue, PhysicsPath = ulong.MaxValue;
        internal bool Removed;
        internal List<Exception>? Errors;
    }
    private readonly record struct GroupEntry(ProcessingGroup Group, int Order, bool Worker) : IComparable<GroupEntry>
    {
        public int CompareTo(GroupEntry other)
        {
            var order = Order.CompareTo(other.Order); if (order != 0) return order;
            var worker = other.Worker.CompareTo(Worker); return worker != 0 ? worker : Group.Owner.ID.CompareTo(other.Group.Owner.ID);
        }
    }
    internal void EnsureProcessingAccess(Node node)
    {
        if (!IsOwnerThread && (CurrentProcessingGroup is null || !ReferenceEquals(CurrentProcessingGroup, node.ActiveProcessingGroup)))
            throw new InvalidOperationException("The node is outside the current processing group.");
    }
    internal void EnsureProcessingReadAccess()
    {
        if (!IsOwnerThread && CurrentProcessingGroup is null)
            throw new InvalidOperationException("Node observation requires the scene owner or an active processing-group thread.");
    }
    internal void BindProcessingGroup(Node node)
    {
        lock (_workGate)
        {
            ProcessingGroup group;
            if (node.ProcessThreadGroup == Node.ProcessThreadGroupMode.Inherit)
                group = node.Parent?.ActiveProcessingGroup ?? DefaultProcessingGroup;
            else
            {
                group = new(this, node.BorrowIdentity()); _processingGroups[node.InstanceID] = group;
            }
            node.ActiveProcessingGroup = group; group.Members.Add(node); InvalidateProcessOrder();
        }
    }
    internal void UnbindProcessingGroup(Node node)
    {
        lock (_workGate)
        {
            if (node.ActiveProcessingGroup is not { } group) return;
            group.Members.Remove(node); node.ActiveProcessingGroup = null; InvalidateProcessOrder();
            if (group.Owner.ID == node.InstanceID) RetireProcessingGroup(group);
        }
    }
    private void RetireProcessingGroup(ProcessingGroup group)
    {
        group.Removed = true; DiscardDeferred(group.Messages); group.Members.Clear();
        _processingGroups.Remove(group.Owner.ID);
    }
    internal void RebindProcessingGroups(Node node)
    {
        EnsureOwnerThread();
        lock (_workGate)
        {
            if (node.ActiveProcessingGroup is { } old && old.Owner.ID == node.InstanceID) RetireProcessingGroup(old);
            Rebind(node, node.Parent?.ActiveProcessingGroup ?? DefaultProcessingGroup);
        }
        InvalidateProcessOrder();

        void Rebind(Node current, ProcessingGroup inherited)
        {
            var old = current.ActiveProcessingGroup; old?.Members.Remove(current);
            ProcessingGroup selected;
            if (current.ProcessThreadGroup == Node.ProcessThreadGroupMode.Inherit) selected = inherited;
            else if (old is { Removed: false } && old.Owner.ID == current.InstanceID) selected = old;
            else { selected = new(this, current.BorrowIdentity()); _processingGroups[current.InstanceID] = selected; }
            current.ActiveProcessingGroup = selected; selected.Members.Add(current);
            foreach (var child in current.AllChildren) Rebind(child, selected);
        }
    }
    private bool _processingWorkGatePrepared;
    private void PrepareProcessingWorkGate()
    {
        if (_processingWorkGatePrepared) return;
        var entering = false;
        var helper = new System.Threading.Thread(() =>
        {
            Volatile.Write(ref entering, true);
            lock (_workGate) { }
        })
        { IsBackground = true, Name = "Electron2D scene gate preparation" };
        var started = false;
        try
        {
            lock (_workGate)
            {
                using var flow = System.Threading.ExecutionContext.IsFlowSuppressed() ? default : System.Threading.ExecutionContext.SuppressFlow();
                helper.Start(); started = true;
                System.Threading.SpinWait.SpinUntil(() => Volatile.Read(ref entering) &&
                    (helper.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0);
            }
        }
        finally { if (started) helper.Join(); }
        _processingWorkGatePrepared = true;
    }

    private void ProcessNodeGroups(double unscaledDelta, bool physics, ref List<Exception>? errors)
    {
        _groupPhysics = physics; _groupUnscaledDelta = unscaledDelta;
        _groupFrame.Clear();
        foreach (var group in _processingGroups.Values)
        {
            var owner = group.Owner.Target as Node;
            _groupFrame.Add(new(group, owner?.ProcessThreadGroupOrder ?? 0, owner?.ProcessThreadGroup == Node.ProcessThreadGroupMode.SubThread));
        }
        _groupFrame.Sort(ProcessingGroupOrder);
        for (var from = 0; from < _groupFrame.Count;)
        {
            var first = _groupFrame[from]; var end = from + 1;
            while (end < _groupFrame.Count && _groupFrame[end].Order == first.Order && _groupFrame[end].Worker == first.Worker) end++;
            RefreshProcessingTreeOrder(); _groupStage.Clear();
            for (var i = from; i < end; i++)
                if (ProcessingGroupEligible(_groupFrame[i].Group, physics)) _groupStage.Add(_groupFrame[i].Group);
            if (first.Worker && _groupStage.Count != 0)
            {
                try
                {
                    if (!_processingWorkGatePrepared) PrepareProcessingWorkGate();
                    _runGroupWorker ??= index => RunProcessingGroup(_groupStage[index]);
                    BeginSceneTransformAuthoring();
                    try
                    {
                        _processingWorkerStage = true;
                        var task = WorkerThreadPool.AddGroupTask(_runGroupWorker, _groupStage.Count, highPriority: true);
                        WorkerThreadPool.WaitForGroupTaskCompletion(task);
                    }
                    finally { _processingWorkerStage = false; EndSceneTransformAuthoring(); }
                }
                catch (Exception error) { CollectException(ref errors, error); }
            }
            else foreach (var group in _groupStage) RunProcessingGroup(group);
            foreach (var group in _groupStage)
            {
                if (group.Errors is not { } failures) continue;
                foreach (var failure in failures) CollectException(ref errors, failure);
                failures.Clear();
            }
            if (first.Worker) PublishProcessingTransforms(ref errors);
            from = end;
        }
        _groupStage.Clear(); _groupFrame.Clear();
    }
    private bool ProcessingGroupEligible(ProcessingGroup group, bool physics)
    {
        if (group.Removed) return false;
        foreach (var node in group.Members) if (node.HasProcessCallback(physics)) return true;
        var owner = group.Owner.Target as Node;
        var flag = physics ? Node.ProcessThreadMessageFlags.Physics : Node.ProcessThreadMessageFlags.Process;
        lock (_workGate) return group.Messages.Count != 0 && (owner is null || (owner.ProcessThreadMessages & flag) != 0);
    }
    private void RefreshProcessingTreeOrder()
    {
        if (_groupTreeOrderPath == PathRevision) return;
        _scheduleTraversal.Clear(); _scheduleTraversal.Add(Root); var order = 0;
        while (_scheduleTraversal.Count != 0)
        {
            var last = _scheduleTraversal.Count - 1; var node = _scheduleTraversal[last]; _scheduleTraversal.RemoveAt(last);
            node.ProcessingTreeOrder = order++;
            for (var i = node.AllChildren.Count - 1; i >= 0; i--) _scheduleTraversal.Add(node.AllChildren[i]);
        }
        _groupTreeOrderPath = PathRevision;
    }
    private void RunProcessingGroup(ProcessingGroup group)
    {
        if (group.Removed) return;
        var previous = CurrentProcessingGroup; CurrentProcessingGroup = group;
        try
        {
            FlushProcessMessages(group, ref group.Errors);
            if (group.Removed) return;
            if (IsOwnerThread) RefreshProcessingTreeOrder();
            var schedule = _groupPhysics ? group.PhysicsSchedule : group.IdleSchedule;
            ref var revision = ref (_groupPhysics ? ref group.PhysicsRevision : ref group.IdleRevision);
            ref var path = ref (_groupPhysics ? ref group.PhysicsPath : ref group.IdlePath);
            var capturedRevision = Volatile.Read(ref _scheduleRevision);
            if (revision != capturedRevision || path != PathRevision)
            {
                schedule.Clear(); var sorted = true; var previousPriority = int.MinValue;
                foreach (var node in group.Members)
                {
                    if (!node.HasProcessCallback(_groupPhysics)) continue;
                    var priority = _groupPhysics ? node.PhysicsProcessPriority : node.ProcessPriority;
                    if (priority < previousPriority) sorted = false; previousPriority = priority;
                    schedule.Add(new(node, priority, node.ProcessingTreeOrder, node.TreeAttachmentVersion));
                }
                // Membership insertion can differ from current tree order after hierarchy edits.
                if (!sorted || schedule.Count > 1) schedule.Sort(ProcessingNodeOrder);
                revision = capturedRevision; path = PathRevision;
            }
            foreach (var item in schedule)
            {
                var node = item.Node;
                if (node.IsDisposed || !ReferenceEquals(node.Tree, this) || !ReferenceEquals(node.ActiveProcessingGroup, group) ||
                    node.TreeAttachmentVersion != item.Attachment || !node.HasProcessCallback(_groupPhysics) || !node.CanProcess()) continue;
                try { node.RunProcess(_groupUnscaledDelta, _groupPhysics); }
                catch (Exception error) { CollectException(ref group.Errors, error); }
            }
            FlushProcessMessages(group, ref group.Errors);
        }
        finally { CurrentProcessingGroup = previous; }
    }
}
