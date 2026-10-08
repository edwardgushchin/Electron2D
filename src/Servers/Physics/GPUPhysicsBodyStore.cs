using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

/// <summary>Authoritative device body/geometry storage with sparse edits, integration, broad phase and explicit reads; no CPU solver world.</summary>
internal sealed unsafe partial class GPUPhysicsBodyStore : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly record struct BodyHandle(int Index, uint Generation, long Owner);
    internal readonly record struct BodyDefinition(PhysicsServer.BodyMode Mode, Vector2 Position, float Rotation,
        Vector2 Velocity, float AngularVelocity, float Mass = 1, float Inertia = 0, float GravityScale = 1,
        float LinearDamp = 0, float AngularDamp = 0, Vector2 ConstantForce = default, float ConstantTorque = 0);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Snapshot
    {
        internal Float4 Pose, Velocity;
        internal readonly Vector2 Position => new(Pose.X, Pose.Y);
        internal readonly float Rotation => MathF.Atan2(Pose.W, Pose.Z);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Body
    {
        internal Float4 Pose, Velocity, Force, Properties;
        internal uint Generation, Mode, Locks, Alive;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Command
    {
        internal int Index;
        internal uint Generation, Mask, Padding;
        internal Body Body;
        internal Float4 Impulse;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Settings
    {
        internal Float4 Step;
        internal uint Stage, Count, Capacity, Padding;
    }
    private struct Slot
    {
        internal uint Generation;
        internal int NextFree, Command, FirstShape;
        internal bool Alive;
    }

    private const uint Create = 1, Destroy = 2, Pose = 4, Velocity = 8, Impulse = 16, Force = 32;
    private static long _nextIdentity;
    private readonly long _identity = Interlocked.Increment(ref _nextIdentity);
    private readonly GPUPhysicsDevice _context;
    private readonly RenderHandle _pipeline;
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private RenderHandle? _bodies, _commands, _requests, _results, _status, _upload, _download;
    private Slot[] _slots = [];
    private Command[] _pending = [];
    private int _highWater, _free = -1, _pendingCount;
    private long _bodyVersion;
    private bool _disposed, _failed;
    internal int Count { get; private set; }
    internal string Driver => _context.Driver;
    internal string DeviceName => _context.DeviceName;
    internal long UploadBytes { get; private set; }
    internal long ReadbackBytes { get; private set; }
    internal long UniformBytes { get; private set; }
    internal long DeviceCopyBytes { get; private set; }
    internal long SubmissionCount { get; private set; }
    internal double WaitMS { get; private set; }
    private nint Device => _context.Device;

    internal GPUPhysicsBodyStore()
    {
        _context = new();
        try { _pipeline = _context.CreatePipeline("PhysicsResidentBodies.comp.spv"); }
        catch { _context.Dispose(); throw; }
    }

    internal BodyHandle Add(in BodyDefinition definition)
    {
        EnsureAccess();
        if (!Enum.IsDefined(definition.Mode) || !definition.Position.IsFinite() || !definition.Velocity.IsFinite() ||
            !float.IsFinite(definition.Rotation) || !float.IsFinite(definition.AngularVelocity) ||
            !float.IsFinite(definition.Mass) || definition.Mass <= 0 || !float.IsFinite(1 / definition.Mass) ||
            !float.IsFinite(definition.Inertia) || definition.Inertia < 0 || definition.Inertia > 0 && !float.IsFinite(1 / definition.Inertia) ||
            !definition.ConstantForce.IsFinite() || !float.IsFinite(definition.ConstantTorque) ||
            !float.IsFinite(definition.GravityScale) || !float.IsFinite(definition.LinearDamp) || !float.IsFinite(definition.AngularDamp))
            throw new ArgumentOutOfRangeException(nameof(definition));
        var index = _free;
        if (index < 0) { Reserve(_highWater + 1); index = _highWater++; }
        else _free = _slots[index].NextFree;
        ref var slot = ref _slots[index];
        slot.Generation = checked(slot.Generation + 1); slot.Alive = true; slot.FirstShape = -1; Count++;
        ref var command = ref Edit(index);
        command = new() { Index = index, Generation = slot.Generation, Mask = Create };
        command.Body = new()
        {
            Pose = new(definition.Position.X, definition.Position.Y, MathF.Cos(definition.Rotation), MathF.Sin(definition.Rotation)),
            Velocity = new(definition.Velocity.X, definition.Velocity.Y, definition.AngularVelocity, 0),
            Force = new(definition.ConstantForce.X, definition.ConstantForce.Y, definition.ConstantTorque, definition.GravityScale),
            Properties = new(1 / definition.Mass, definition.Inertia > 0 ? 1 / definition.Inertia : 0, definition.LinearDamp, definition.AngularDamp),
            Generation = slot.Generation,
            Mode = (uint)definition.Mode,
            Locks = definition.Mode == PhysicsServer.BodyMode.RigidLinear ? 4u : 0u,
            Alive = 1
        };
        return new(index, slot.Generation, _identity);
    }

    internal void Remove(BodyHandle body)
    {
        Validate(body);
        RemoveBodyShapes(body);
        ref var command = ref Edit(body.Index);
        command = new() { Index = body.Index, Generation = body.Generation, Mask = Destroy };
        ref var slot = ref _slots[body.Index];
        slot.Alive = false; slot.NextFree = _free; _free = body.Index; Count--;
    }

    internal void SetPose(BodyHandle body, Vector2 position, float rotation)
    {
        Validate(body);
        if (!position.IsFinite() || !float.IsFinite(rotation)) throw new ArgumentOutOfRangeException(nameof(position));
        ref var command = ref Edit(body.Index); command.Mask |= Pose;
        command.Body.Pose = new(position.X, position.Y, MathF.Cos(rotation), MathF.Sin(rotation));
    }

    internal void SetVelocity(BodyHandle body, Vector2 linear, float angular)
    {
        Validate(body);
        if (!linear.IsFinite() || !float.IsFinite(angular)) throw new ArgumentOutOfRangeException(nameof(linear));
        ref var command = ref Edit(body.Index); command.Mask |= Velocity;
        command.Body.Velocity = new(linear.X, linear.Y, angular, 0);
        // A later explicit velocity assignment supersedes earlier queued impulses.
        command.Impulse = default; command.Mask &= ~Impulse;
    }

    internal void ApplyImpulse(BodyHandle body, Vector2 linear, float angular = 0)
    {
        Validate(body);
        if (!linear.IsFinite() || !float.IsFinite(angular)) throw new ArgumentOutOfRangeException(nameof(linear));
        ref var command = ref Edit(body.Index);
        var total = command.Impulse + new Float4(linear.X, linear.Y, angular, 0);
        if (!Finite(total)) throw new ArgumentOutOfRangeException(nameof(linear));
        command.Impulse = total; command.Mask |= Impulse;
    }

    internal void SetConstantForce(BodyHandle body, Vector2 linear, float angular = 0)
    {
        Validate(body);
        if (!linear.IsFinite() || !float.IsFinite(angular)) throw new ArgumentOutOfRangeException(nameof(linear));
        ref var command = ref Edit(body.Index); command.Mask |= Force;
        command.Body.Force = new(linear.X, linear.Y, angular, command.Body.Force.W);
    }

    internal void Step(float delta, Vector2 gravity)
    {
        EnsureAccess();
        if (!float.IsFinite(delta) || delta < 0 || !gravity.IsFinite()) throw new ArgumentOutOfRangeException(nameof(delta));
        if (_highWater == 0 || delta == 0 && _pendingCount == 0) return;
        Submit(delta, gravity, default, default);
    }

    internal void Read(ReadOnlySpan<BodyHandle> bodies, Span<Snapshot> results)
    {
        EnsureAccess();
        if (results.Length < bodies.Length) throw new ArgumentException("The state destination is too small.", nameof(results));
        foreach (var body in bodies) Validate(body);
        if (bodies.IsEmpty) return;
        // A caller may request one body more than once; scratch capacity follows request count.
        Reserve(Math.Max(_highWater, bodies.Length));
        Submit(0, default, bodies, results);
    }

    private ref Command Edit(int index)
    {
        ref var slot = ref _slots[index];
        if (slot.Command < 0)
        {
            slot.Command = _pendingCount++;
            _pending[slot.Command] = new() { Index = index, Generation = slot.Generation };
        }
        return ref _pending[slot.Command];
    }

    private void Validate(BodyHandle body)
    {
        EnsureAccess();
        if (body.Owner != _identity || (uint)body.Index >= (uint)_highWater || !_slots[body.Index].Alive || _slots[body.Index].Generation != body.Generation)
            throw new ArgumentException("The GPU body handle is stale or foreign.", nameof(body));
    }

    private void EnsureAccess()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_owner != Environment.CurrentManagedThreadId) throw new InvalidOperationException("GPU body state requires its owner thread.");
        if (_failed) throw new InvalidOperationException("The GPU body store failed and must be disposed.");
    }

    private RenderHandle Buffer(uint size) => new(SDL.CreateGPUBuffer(Device, new SDL.GPUBufferCreateInfo
    { Size = size, Usage = SDL.GPUBufferUsageFlags.ComputeStorageRead | SDL.GPUBufferUsageFlags.ComputeStorageWrite }),
        handle => SDL.ReleaseGPUBuffer(Device, handle), _context.Handle);
    private RenderHandle Transfer(uint size, SDL.GPUTransferBufferUsage usage) => new(SDL.CreateGPUTransferBuffer(Device,
        new SDL.GPUTransferBufferCreateInfo { Size = size, Usage = usage }), handle => SDL.ReleaseGPUTransferBuffer(Device, handle), _context.Handle);

    private void Reserve(int required)
    {
        if (required <= _slots.Length) return;
        var capacity = checked((int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(64, required)));
        RenderHandle? bodies = null, commands = null, requests = null, results = null, status = null, upload = null, download = null;
        try
        {
            bodies = Buffer(checked((uint)(capacity * sizeof(Body))));
            commands = Buffer(checked((uint)(capacity * sizeof(Command))));
            requests = Buffer(checked((uint)(capacity * sizeof(BodyHandle))));
            results = Buffer(checked((uint)(capacity * sizeof(Snapshot))));
            status = Buffer(4);
            upload = Transfer(checked((uint)(4 + capacity * (sizeof(Command) + sizeof(BodyHandle)))), SDL.GPUTransferBufferUsage.Upload);
            download = Transfer(checked((uint)(4 + capacity * sizeof(Snapshot))), SDL.GPUTransferBufferUsage.Download);
            var slots = new Slot[capacity]; var pending = new Command[capacity];
            Array.Copy(_slots, slots, _slots.Length); Array.Copy(_pending, pending, _pendingCount);
            for (var i = _slots.Length; i < capacity; i++) slots[i].Command = -1;
            if (_bodies is not null)
            {
                var command = SDL.AcquireGPUCommandBuffer(Device);
                if (command == 0) throw GPUPhysicsDevice.Failure("acquire body-growth commands");
                try
                {
                    var copy = SDL.BeginGPUCopyPass(command);
                    if (copy == 0) throw GPUPhysicsDevice.Failure("begin resident body growth");
                    var bytes = checked((uint)(_highWater * sizeof(Body)));
                    if (bytes > 0) SDL.CopyGPUBufferToBuffer(copy, new() { Buffer = _bodies.DangerousGetHandle() },
                        new() { Buffer = bodies.DangerousGetHandle() }, bytes, false);
                    SDL.EndGPUCopyPass(copy);
                    _failed = true; Finish(ref command); _failed = false; DeviceCopyBytes += bytes;
                }
                finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
            }
            DisposeBuffers();
            (_bodies, _commands, _requests, _results, _status, _upload, _download) = (bodies, commands, requests, results, status, upload, download);
            bodies = commands = requests = results = status = upload = download = null;
            _slots = slots; _pending = pending;
        }
        finally { bodies?.Dispose(); commands?.Dispose(); requests?.Dispose(); results?.Dispose(); status?.Dispose(); upload?.Dispose(); download?.Dispose(); }
    }

    private void Submit(float delta, Vector2 gravity, ReadOnlySpan<BodyHandle> requests, Span<Snapshot> results)
    {
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire resident body commands");
        try
        {
            var commandBytes = checked((uint)(_pendingCount * sizeof(Command)));
            var requestBytes = checked((uint)(requests.Length * sizeof(BodyHandle)));
            var outputBytes = checked((uint)(requests.Length * sizeof(Snapshot)));
            var mapped = SDL.MapGPUTransferBuffer(Device, _upload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map resident edits");
            try
            {
                *(uint*)mapped = 0;
                fixed (Command* source = _pending) System.Buffer.MemoryCopy(source, (byte*)mapped + 4, commandBytes, commandBytes);
                fixed (BodyHandle* source = requests) System.Buffer.MemoryCopy(source, (byte*)mapped + 4 + commandBytes, requestBytes, requestBytes);
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _upload.DangerousGetHandle()); }
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin resident edits");
            Upload(copy, _status!, 0, 4);
            if (commandBytes > 0) Upload(copy, _commands!, 4, commandBytes);
            if (requestBytes > 0) Upload(copy, _requests!, 4 + commandBytes, requestBytes);
            SDL.EndGPUCopyPass(copy);
            var settings = new Settings { Step = new(gravity.X, gravity.Y, delta, 0), Capacity = (uint)_highWater };
            Dispatch(command, ref settings, 0, _pendingCount);
            if (delta > 0) Dispatch(command, ref settings, 1, _highWater);
            Dispatch(command, ref settings, 2, requests.Length);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin resident results");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _status!.DangerousGetHandle(), Size = 4 }, new() { TransferBuffer = _download!.DangerousGetHandle() });
            if (outputBytes > 0) SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _results!.DangerousGetHandle(), Size = outputBytes },
                new() { TransferBuffer = _download.DangerousGetHandle(), Offset = 4 });
            SDL.EndGPUCopyPass(copy);
            // A submitted interval may have mutated device state. Any later error invalidates the store.
            _failed = true;
            Finish(ref command);
            UploadBytes += 4 + commandBytes + requestBytes; ReadbackBytes += 4 + outputBytes;
            UniformBytes += sizeof(Settings) * ((_pendingCount > 0 ? 1 : 0) + (delta > 0 ? 1 : 0) + (requests.Length > 0 ? 1 : 0));
            mapped = SDL.MapGPUTransferBuffer(Device, _download.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map resident results");
            try
            {
                if (*(uint*)mapped != 0) throw new InvalidOperationException("GPU resident body work returned invalid state.");
                fixed (Snapshot* destination = results) System.Buffer.MemoryCopy((byte*)mapped + 4, destination, outputBytes, outputBytes);
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _download.DangerousGetHandle()); }
            if (delta > 0 || _pendingCount > 0) _bodyVersion++;
            for (var i = 0; i < _pendingCount; i++) _slots[_pending[i].Index].Command = -1;
            Array.Clear(_pending, 0, _pendingCount); _pendingCount = 0;
            _failed = false;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }

    private void Upload(nint copy, RenderHandle destination, uint offset, uint size) =>
        SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _upload!.DangerousGetHandle(), Offset = offset },
            new() { Buffer = destination.DangerousGetHandle(), Size = size }, false);

    private void Dispatch(nint command, ref Settings settings, uint stage, int count)
    {
        if (count == 0) return;
        settings.Stage = stage; settings.Count = (uint)count;
        var binding = stackalloc SDL.GPUStorageBufferReadWriteBinding[3];
        binding[0] = new() { Buffer = _bodies!.DangerousGetHandle() };
        binding[1] = new() { Buffer = _status!.DangerousGetHandle() };
        binding[2] = new() { Buffer = _results!.DangerousGetHandle() };
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)binding, 3);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin resident body compute");
        SDL.BindGPUComputePipeline(compute, _pipeline.DangerousGetHandle());
        var inputs = stackalloc nint[2] { _commands!.DangerousGetHandle(), _requests!.DangerousGetHandle() };
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 2);
        fixed (Settings* uniform = &settings) SDL.PushGPUComputeUniformData(command, 0, (nint)uniform, (uint)sizeof(Settings));
        SDL.DispatchGPUCompute(compute, ((uint)count + 63) / 64, 1, 1);
        SDL.EndGPUComputePass(compute);
    }

    private void Finish(ref nint command)
    {
        var submitted = command; command = 0;
        var fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
        if (fence == 0) throw GPUPhysicsDevice.Failure("submit resident body work");
        try
        {
            var start = Stopwatch.GetTimestamp();
            if (!SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1)) throw GPUPhysicsDevice.Failure("wait for resident body work");
            WaitMS += Stopwatch.GetElapsedTime(start).TotalMilliseconds; SubmissionCount++;
        }
        finally { SDL.ReleaseGPUFence(Device, fence); }
    }

    private static bool Finite(Float4 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z) && float.IsFinite(value.W);
    private void DisposeBuffers() { _bodies?.Dispose(); _commands?.Dispose(); _requests?.Dispose(); _results?.Dispose(); _status?.Dispose(); _upload?.Dispose(); _download?.Dispose(); }
    public void Dispose()
    {
        if (_disposed) return;
        if (_owner != Environment.CurrentManagedThreadId) throw new InvalidOperationException("GPU body state requires its owner thread.");
        _disposed = true; DisposeSpatial(); DisposeBuffers(); _pipeline.Dispose(); _context.Dispose();
    }
}
