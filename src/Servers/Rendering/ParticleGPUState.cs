using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe class ParticleGPUState : IDisposable
{
    private readonly RenderHandle _device;
    internal readonly int Amount;
    internal RenderHandle State, Previous, Incoming, Outgoing, Order, History, Poses;
    private RenderHandle _upload;
    private RenderHandle? _download;
    private uint _uploadSize, _downloadSize;
    internal int HistoryFrames, HistoryNext, HistoryWritten, OrderCount;
    internal int IncomingCapacity, OutgoingCapacity;
    private int _poseCount;
    private Transform[]? _uploadedPoses;
    private Vector4[] _poseRows = [];
    internal bool Initialized;
    internal ParticleGPUState(RenderingParticleState source, RenderHandle device)
    {
        _device = device; Amount = source.Amount;
        var bytes = checked((uint)(Amount * ParticleStateLayout.ByteSize));
        State = Previous = Incoming = Outgoing = Order = History = Poses = _upload = null!;
        try
        {
            State = Buffer(bytes); Previous = Buffer(bytes);
            Incoming = Buffer(96); Outgoing = Buffer(96); History = Buffer(bytes); Poses = Buffer(32);
            OrderCount = 1; while (OrderCount < Amount) OrderCount = checked(OrderCount * 2);
            Order = Buffer(checked((uint)OrderCount * 8)); _uploadSize = 96; _upload = Transfer(_uploadSize, SDL.GPUTransferBufferUsage.Upload);
            HistoryFrames = 1;
        }
        catch { Dispose(); throw; }
    }
    private nint Device => _device.DangerousGetHandle();
    private RenderHandle Buffer(uint bytes) => new(SDL.CreateGPUBuffer(Device, new() { Size = bytes, Usage = SDL.GPUBufferUsageFlags.ComputeStorageRead | SDL.GPUBufferUsageFlags.ComputeStorageWrite }), h => SDL.ReleaseGPUBuffer(Device, h), _device);
    private RenderHandle Transfer(uint bytes, SDL.GPUTransferBufferUsage usage) => new(SDL.CreateGPUTransferBuffer(Device, new() { Size = bytes, Usage = usage }), h => SDL.ReleaseGPUTransferBuffer(Device, h), _device);
    internal void Prepare(RenderingParticleState source, nint command, bool intake)
    {
        var incoming = intake ? Amount : 0; var outgoing = source.Subemitter is { Released: false } child ? child.Amount : 0;
        if (incoming != IncomingCapacity)
        {
            var replacement = Buffer(checked(((uint)incoming + 1) * 96)); Incoming.Dispose(); Incoming = replacement; IncomingCapacity = incoming;
        }
        if (outgoing != OutgoingCapacity)
        {
            var replacement = Buffer(checked(((uint)outgoing + 1) * 96)); Outgoing.Dispose(); Outgoing = replacement; OutgoingCapacity = outgoing;
        }
        var frames = source.Trails && source.BindPoses.Length > 1 ? Math.Max(2, checked((int)Math.Ceiling(source.TrailLength * (source.FixedFPS > 0 ? source.FixedFPS : 60d)) + 1)) : 1;
        if ((long)frames * Amount * 96 > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(source), "Particle history exceeds the native byte budget.");
        if (frames != HistoryFrames)
        {
            var replacement = Buffer(checked((uint)(frames * Amount * 96))); History.Dispose(); History = replacement; HistoryFrames = frames; HistoryNext = HistoryWritten = 0;
        }
        if (source.BindPoses.Length != _poseCount)
        {
            var replacement = Buffer(checked((uint)Math.Max(1, source.BindPoses.Length) * 32)); Poses.Dispose(); Poses = replacement; _poseCount = source.BindPoses.Length;
        }
        if (!ReferenceEquals(_uploadedPoses, source.BindPoses))
        {
            if (_poseRows.Length != source.BindPoses.Length * 2) _poseRows = new Vector4[source.BindPoses.Length * 2];
            for (var i = 0; i < source.BindPoses.Length; i++)
            {
                var pose = source.BindPoses[i]; _poseRows[i * 2] = new(pose.X.X, pose.Y.X, 0, pose.Origin.X); _poseRows[i * 2 + 1] = new(pose.X.Y, pose.Y.Y, 0, pose.Origin.Y);
            }
            if (_poseRows.Length != 0) Upload(command, Poses, 0, MemoryMarshal.AsBytes(_poseRows.AsSpan()));
            _uploadedPoses = source.BindPoses;
        }
        Span<ParticleStateLayout> header = stackalloc ParticleStateLayout[1]; header.Clear(); header[0].State.Y = IncomingCapacity;
        if (source.ManualCount != 0)
        {
            source.Manual[0] = header[0]; source.Manual[0].State.X = source.ManualCount;
            Upload(command, Incoming, 0, MemoryMarshal.AsBytes(source.Manual.AsSpan(0, source.ManualCount + 1)));
        }
        else Upload(command, Incoming, 0, MemoryMarshal.AsBytes(header));
        ResetOutgoing(command);
    }
    internal void ResetOutgoing(nint command)
    {
        Span<ParticleStateLayout> header = stackalloc ParticleStateLayout[1]; header.Clear(); header[0].State.Y = OutgoingCapacity;
        Upload(command, Outgoing, 0, MemoryMarshal.AsBytes(header));
    }
    internal void Upload(nint command, RenderHandle buffer, uint offset, ReadOnlySpan<byte> bytes)
    {
        if (_uploadSize < bytes.Length)
        {
            var replacement = Transfer((uint)bytes.Length, SDL.GPUTransferBufferUsage.Upload); _upload.Dispose(); _upload = replacement; _uploadSize = (uint)bytes.Length;
        }
        var pointer = SDL.MapGPUTransferBuffer(Device, _upload.DangerousGetHandle(), true); if (pointer == 0) throw GPUPhysicsDevice.Failure("map particle upload");
        try { bytes.CopyTo(new Span<byte>((void*)pointer, bytes.Length)); } finally { SDL.UnmapGPUTransferBuffer(Device, _upload.DangerousGetHandle()); }
        var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw GPUPhysicsDevice.Failure("begin particle upload");
        SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _upload.DangerousGetHandle() }, new() { Buffer = buffer.DangerousGetHandle(), Offset = offset, Size = (uint)bytes.Length }, false); SDL.EndGPUCopyPass(copy);
    }
    internal void CopyPrevious(nint command)
    {
        var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw GPUPhysicsDevice.Failure("begin particle previous-state copy");
        SDL.CopyGPUBufferToBuffer(copy, new() { Buffer = State.DangerousGetHandle() }, new() { Buffer = Previous.DangerousGetHandle() }, checked((uint)Amount * 96), false); SDL.EndGPUCopyPass(copy);
    }
    internal void CaptureHistory(nint command)
    {
        var bytes = checked((uint)Amount * 96); var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw GPUPhysicsDevice.Failure("begin particle history capture");
        SDL.CopyGPUBufferToBuffer(copy, new() { Buffer = State.DangerousGetHandle() }, new() { Buffer = History.DangerousGetHandle(), Offset = checked((uint)HistoryNext * bytes) }, bytes, false); SDL.EndGPUCopyPass(copy);
        HistoryNext = (HistoryNext + 1) % HistoryFrames; HistoryWritten = Math.Min(HistoryFrames, HistoryWritten + 1);
    }
    internal ParticleStateLayout[] Read(RenderHandle buffer, int records)
    {
        var bytes = checked((uint)records * 96); var output = new ParticleStateLayout[records]; if (bytes == 0) return output;
        if (_downloadSize < bytes) { var replacement = Transfer(bytes, SDL.GPUTransferBufferUsage.Download); _download?.Dispose(); _download = replacement; _downloadSize = bytes; }
        var command = SDL.AcquireGPUCommandBuffer(Device); if (command == 0) throw GPUPhysicsDevice.Failure("acquire particle readback");
        nint fence = 0;
        try
        {
            var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw GPUPhysicsDevice.Failure("begin particle readback");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = buffer.DangerousGetHandle(), Size = bytes }, new() { TransferBuffer = _download!.DangerousGetHandle() }); SDL.EndGPUCopyPass(copy);
            var submitted = command; command = 0; fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted); if (fence == 0) throw GPUPhysicsDevice.Failure("submit particle readback");
            if (!SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1)) throw GPUPhysicsDevice.Failure("wait for particle readback");
            var pointer = SDL.MapGPUTransferBuffer(Device, _download.DangerousGetHandle(), false); if (pointer == 0) throw GPUPhysicsDevice.Failure("map particle readback");
            try { new ReadOnlySpan<byte>((void*)pointer, (int)bytes).CopyTo(MemoryMarshal.AsBytes(output.AsSpan())); } finally { SDL.UnmapGPUTransferBuffer(Device, _download.DangerousGetHandle()); }
        }
        finally { if (fence != 0) SDL.ReleaseGPUFence(Device, fence); if (command != 0) SDL.CancelGPUCommandBuffer(command); }
        return output;
    }
    internal void CancelPreparedUploads() => _uploadedPoses = null;
    public void Dispose()
    { State?.Dispose(); Previous?.Dispose(); Incoming?.Dispose(); Outgoing?.Dispose(); Order?.Dispose(); History?.Dispose(); Poses?.Dispose(); _upload?.Dispose(); _download?.Dispose(); }
}
