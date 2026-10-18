using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed class MultiMeshGPUState
{
    private readonly WeakReference<MultiMesh> _source;
    internal readonly RenderingDevice Device;
    internal RID Current, Previous, Commands;
    private readonly List<(int First, int End)> _currentDirty = [], _previousDirty = [], _resets = [];
    private bool _copyPrevious, _preparing;
    private bool _commandsChanged;
    internal bool CommandsAlive => !_commandsChanged && _mesh is not { IsDisposed: true };
    private int? _visible;
    private Mesh? _mesh;
    private long _meshRevision = -1;
    private (int Count, bool Indexed)[] _layout = [], _scratchLayout = [];
    internal bool Alive => !Device.IsDisposed && _source.TryGetTarget(out var source) && source.Owns(this);
    internal MultiMeshGPUState(MultiMesh source, RenderingDevice device)
    {
        _source = new(source); Device = device;
    }
    private static void Dirty(List<(int First, int End)> ranges, int first, int count)
    {
        if (count == 0) return; var end = checked(first + count); var index = 0;
        while (index < ranges.Count && ranges[index].End < first) index++;
        while (index < ranges.Count && ranges[index].First <= end) { first = Math.Min(first, ranges[index].First); end = Math.Max(end, ranges[index].End); ranges.RemoveAt(index); }
        ranges.Insert(index, (first, end));
    }
    internal void Changed(int first, int count, bool previous) { Dirty(_currentDirty, first, count); if (previous) Dirty(_previousDirty, first, count); }
    internal void SetSnapshots(int count) { _copyPrevious = false; _resets.Clear(); Changed(0, count, true); }
    internal void ResetPrevious(int first, int count) => Dirty(_resets, first, count);
    internal void PhysicsTick()
    {
        _copyPrevious = true; _resets.Clear();
        foreach (var (first, end) in _currentDirty) Dirty(_previousDirty, first, end - first);
    }
    internal void MeshChanged() { _commandsChanged = true; _mesh = null; _visible = null; }
    internal void SetVisible(int count) => _visible = count;
    internal void Prepare(bool geometry = true)
    {
        if (_preparing) throw new InvalidOperationException("Instance buffer preparation cannot reenter.");
        if (Device.IsDisposed || !_source.TryGetTarget(out var source) || !source.Owns(this)) throw new InvalidOperationException("The retained instance buffers have expired.");
        _preparing = true;
        try
        {
            lock (source.Gate)
            {
                if (!Current.IsValid())
                {
                    Current = Device.CreateMultiMeshBuffer(this, MemoryMarshal.AsBytes(source.CurrentData));
                    try { Previous = Device.CreateMultiMeshBuffer(this, MemoryMarshal.AsBytes(source.PreviousData)); }
                    catch { Device.ExpireMultiMeshBuffer(Current); Current = default; throw; }
                }
                if (_copyPrevious) { Device.CopyMultiMeshBuffer(Current, Previous, 0, (uint)MemoryMarshal.AsBytes(source.CurrentData).Length); _copyPrevious = false; }
                foreach (var (first, end) in _currentDirty) Device.UpdateMultiMeshBuffer(Current, (uint)(first * 4), MemoryMarshal.AsBytes(source.CurrentData[first..end]));
                foreach (var (first, end) in _previousDirty) Device.UpdateMultiMeshBuffer(Previous, (uint)(first * 4), MemoryMarshal.AsBytes(source.PreviousData[first..end]));
                foreach (var (first, end) in _resets) Device.CopyMultiMeshBuffer(Current, Previous, (uint)(first * 4), (uint)((end - first) * 4));
                _currentDirty.Clear(); _previousDirty.Clear(); _resets.Clear();
                if (geometry) PrepareCommands(source);
                if (geometry && _visible is { } visible && Commands.IsValid())
                {
                    Span<byte> value = stackalloc byte[4]; BinaryPrimitives.WriteInt32LittleEndian(value, visible);
                    for (var i = 0; i < _layout.Length; i++) Device.UpdateMultiMeshBuffer(Commands, (uint)(i * 20 + 4), value);
                    _visible = null;
                }
            }
        }
        finally { _preparing = false; }
    }
    private void PrepareCommands(MultiMesh source)
    {
        if (!source.UseIndirect) return;
        var mesh = source.Mesh; if (mesh is not null) ObjectDisposedException.ThrowIf(mesh.IsDisposed, mesh);
        if (!_commandsChanged && ReferenceEquals(mesh, _mesh) && (mesh?.ChangeRevision ?? -1) == _meshRevision) return;
        var sourceRevision = source.ChangeRevision; var revision = mesh?.ChangeRevision ?? -1; var count = mesh?.GetSurfaceCount() ?? 0;
        if (count < 0 || (long)count * 20 > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(count));
        if (_scratchLayout.Length != count) _scratchLayout = new (int Count, bool Indexed)[count]; var layout = _scratchLayout;
        for (var i = 0; i < count; i++) { var indices = mesh!.SurfaceGetArrayIndexLen(i); var vertices = mesh.SurfaceGetArrayLen(i); if (indices < 0 || vertices < 0) throw new ArgumentException("Surface invocation counts must be nonnegative."); layout[i] = (indices == 0 ? vertices : indices, indices != 0); }
        if (mesh is { IsDisposed: true } || mesh is not null && mesh.ChangeRevision != revision || source.ChangeRevision != sourceRevision || !ReferenceEquals(source.Mesh, mesh) || !source.Owns(this)) throw new InvalidOperationException("Mesh callbacks require stable live instance-buffer storage.");
        if (_commandsChanged || !ReferenceEquals(mesh, _mesh) || !layout.AsSpan().SequenceEqual(_layout))
        {
            var bytes = new byte[checked(count * 20)]; for (var i = 0; i < count; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * 20), layout[i].Count);
            var next = count == 0 ? default : Device.CreateMultiMeshBuffer(this, bytes, indirect: true);
            Device.ExpireMultiMeshBuffer(Commands); Commands = next;
        }
        _mesh = mesh; _scratchLayout = _layout; _layout = layout; _meshRevision = revision; _commandsChanged = false;
    }
    internal nint Handle(RID rid, nint device) => Device.MultiMeshHandle(rid, device);
}
