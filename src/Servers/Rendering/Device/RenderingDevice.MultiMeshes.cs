using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    internal RID CreateMultiMeshBuffer(MultiMeshGPUState owner, ReadOnlySpan<byte> data, bool indirect = false)
    {
        IdleList(true); if (data.IsEmpty) throw new ArgumentException("A retained buffer requires capacity.", nameof(data));
        var entry = new Entry { Kind = Kind.Storage, Size = (uint)data.Length, Usage = indirect ? StorageBufferUsage.DispatchIndirect : 0, MeshOwner = owner, MeshCommand = indirect };
        try
        {
            entry.Handle = new(SDL.CreateGPUBuffer(Device, new() { Size = entry.Size, Usage = SDL.GPUBufferUsageFlags.ComputeStorageRead | SDL.GPUBufferUsageFlags.ComputeStorageWrite | SDL.GPUBufferUsageFlags.GraphicsStorageRead | (indirect ? SDL.GPUBufferUsageFlags.Indirect : 0) }), h => SDL.ReleaseGPUBuffer(Device, h), _context.Handle);
            entry.Upload = Transfer(entry.Size, SDL.GPUTransferBufferUsage.Upload); Upload(entry, 0, data); return Add(entry);
        }
        catch { entry.Handle?.Dispose(); entry.Upload?.Dispose(); throw; }
    }
    internal RID ExportMultiMeshBuffer(RID rid)
    {
        Access(); if (!rid.IsValid()) return default;
        if (!_entries.TryGetValue(rid, out var entry) || !entry.Alive) throw new ArgumentException("The instance buffer has expired.", nameof(rid));
        entry.MeshExported = true; return rid;
    }
    internal void UpdateMultiMeshBuffer(RID rid, uint offset, ReadOnlySpan<byte> data)
    {
        Access(true); if (_drawPass != 0) throw new InvalidOperationException("Instance data cannot upload inside a native render pass.");
        var entry = _entries[rid]; Range(entry, offset, (uint)data.Length); Upload(entry, offset, data);
    }
    internal void CopyMultiMeshBuffer(RID source, RID target, uint offset, uint count)
    {
        Access(true); if (_drawPass != 0) throw new InvalidOperationException("Instance history cannot copy inside a native render pass.");
        var pass = SDL.BeginGPUCopyPass(Commands()); if (pass == 0) throw GPUPhysicsDevice.Failure("begin instance history copy");
        SDL.CopyGPUBufferToBuffer(pass, new() { Buffer = _entries[source].NativeHandle, Offset = offset }, new() { Buffer = _entries[target].NativeHandle, Offset = offset }, count, false); SDL.EndGPUCopyPass(pass);
    }
    internal void ExpireMultiMeshBuffer(RID rid) { if (_entries.TryGetValue(rid, out var entry)) { entry.Expired = true; EvictResourceCaches(); } }
    internal nint MultiMeshHandle(RID rid, nint device)
    {
        Access(true); if (Device != device) throw new InvalidOperationException("Instance data belongs to another native device.");
        return _entries.TryGetValue(rid, out var entry) && entry.Alive ? entry.NativeHandle : throw new ArgumentException("Instance buffer storage has expired.", nameof(rid));
    }
    private readonly List<RID> _retiredMultiMeshBuffers = [];
    private void RetireMultiMeshBuffers()
    {
        if (_command != 0 || _fence != 0 || _recordedReadbacks.Count != 0) return;
        var retired = false;
        foreach (var (rid, entry) in _entries)
        {
            if (entry.MeshOwner is null || entry.Alive) continue;
            if (!entry.MeshExported) _retiredMultiMeshBuffers.Add(rid);
            retired = true; entry.Expired = true; entry.MeshOwner = null; entry.Handle?.Dispose(); entry.Upload?.Dispose(); entry.Download?.Dispose(); entry.Handle = entry.Upload = entry.Download = null;
        }
        foreach (var rid in _retiredMultiMeshBuffers) _entries.Remove(rid); _retiredMultiMeshBuffers.Clear(); if (retired) EvictResourceCaches();
    }
}
