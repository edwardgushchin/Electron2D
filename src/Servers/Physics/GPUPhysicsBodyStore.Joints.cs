using System.Runtime.InteropServices;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    internal readonly record struct JointHandle(int Index, uint Generation, long Owner);
    internal readonly record struct JointDefinition(PhysicsServer.JointType Type, BodyHandle BodyA, BodyHandle BodyB, Transform FrameA, Transform FrameB)
    {
        internal bool DisableCollision { get; init; } = true;
        internal bool LimitEnabled { get; init; }
        internal bool MotorEnabled { get; init; }
        internal float LowerAngle { get; init; }
        internal float UpperAngle { get; init; }
        internal float MotorVelocity { get; init; }
        internal float MotorMaxTorque { get; init; } = 10;
        internal float LowerTranslation { get; init; }
        internal float UpperTranslation { get; init; } = 50;
        internal float RestLength { get; init; } = 50;
        internal float Stiffness { get; init; } = 20;
        internal float Damping { get; init; } = 1;
    }
    private struct JointSlot
    {
        internal JointDefinition Definition;
        internal uint Generation;
        internal int NextFree, PreviousA, NextA, PreviousB, NextB;
        internal bool Alive, Dirty;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct JointData
    {
        internal uint Generation, Type, A, B;
        internal uint GenerationA, GenerationB, Flags, Padding;
        internal Float4 FrameA, FrameB, Limits, MotorSpring, Policy;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct JointEdit { internal uint Index, Padding1, Padding2, Padding3; internal JointData Value; }
    [StructLayout(LayoutKind.Sequential)]
    private struct JointEditSettings { internal uint Stage, Count, Joints, Capacity; }
    private JointSlot[] _jointSlots = [];
    private JointEdit[] _jointEdits = [];
    private readonly List<int> _dirtyJoints = [];
    private int _jointHighWater, _jointFree = -1, _springJointCount;
    private RenderHandle? _jointsGPU, _jointStatesGPU, _jointEditsGPU, _jointFiltersGPU, _jointEditPipeline, _jointPreparePipeline;
    private int _jointCapacity, _jointStateCapacity, _jointEditCapacity, _jointFilterCapacity;
    internal int JointCount { get; private set; }
    internal long JointUploadBytes { get; private set; }
    private const int JointRows = 5;

    internal JointHandle AddJoint(in JointDefinition definition)
    {
        ValidateJointDefinition(definition);
        var index = _jointFree;
        if (index < 0)
        {
            if (_jointHighWater == _jointSlots.Length) Array.Resize(ref _jointSlots, Math.Max(64, checked(_jointSlots.Length * 2)));
            index = _jointHighWater++;
        }
        else _jointFree = _jointSlots[index].NextFree;
        ref var slot = ref _jointSlots[index];
        slot.Generation = checked(slot.Generation + 1); slot.Alive = true; slot.Definition = definition;
        LinkJoint(index, definition.BodyA.Index);
        if (definition.BodyB != default) LinkJoint(index, definition.BodyB.Index);
        JointCount++; if (definition.Type == PhysicsServer.JointType.DampedSpring) _springJointCount++;
        MarkJoint(index);
        return new(index, slot.Generation, _identity);
    }

    internal JointDefinition GetJointDefinition(JointHandle joint) { Validate(joint); return _jointSlots[joint.Index].Definition; }

    internal void SetJoint(JointHandle joint, in JointDefinition definition)
    {
        Validate(joint); ValidateJointDefinition(definition);
        ref var slot = ref _jointSlots[joint.Index];
        if (slot.Definition == definition) return;
        WakeJoint(slot.Definition);
        UnlinkJoint(joint.Index, slot.Definition.BodyA.Index);
        if (slot.Definition.BodyB != default) UnlinkJoint(joint.Index, slot.Definition.BodyB.Index);
        if (slot.Definition.Type == PhysicsServer.JointType.DampedSpring) _springJointCount--;
        if (definition.Type == PhysicsServer.JointType.DampedSpring) _springJointCount++;
        slot.Definition = definition;
        LinkJoint(joint.Index, definition.BodyA.Index);
        if (definition.BodyB != default) LinkJoint(joint.Index, definition.BodyB.Index);
        MarkJoint(joint.Index);
    }

    internal void RemoveJoint(JointHandle joint)
    {
        Validate(joint); ref var slot = ref _jointSlots[joint.Index];
        WakeJoint(slot.Definition);
        UnlinkJoint(joint.Index, slot.Definition.BodyA.Index);
        if (slot.Definition.BodyB != default) UnlinkJoint(joint.Index, slot.Definition.BodyB.Index);
        slot.Alive = false; slot.NextFree = _jointFree; _jointFree = joint.Index;
        JointCount--; if (slot.Definition.Type == PhysicsServer.JointType.DampedSpring) _springJointCount--;
        MarkJoint(joint.Index);
    }

    private void Validate(JointHandle joint)
    {
        EnsureAccess();
        if (joint.Owner != _identity || (uint)joint.Index >= _jointHighWater || !_jointSlots[joint.Index].Alive || _jointSlots[joint.Index].Generation != joint.Generation)
            throw new ArgumentException("The joint handle is foreign, stale or removed.", nameof(joint));
    }
    private void ValidateJointDefinition(in JointDefinition d)
    {
        Validate(d.BodyA);
        if (d.BodyB != default) Validate(d.BodyB);
        if (d.Type is < PhysicsServer.JointType.Pin or > PhysicsServer.JointType.DampedSpring || d.BodyA == d.BodyB || d.BodyB == default && d.Type != PhysicsServer.JointType.Pin)
            throw new ArgumentException("Joint endpoints and role are invalid.", nameof(d));
        ValidateShapePose(d.FrameA); ValidateShapePose(d.FrameB);
        foreach (var value in (ReadOnlySpan<float>)[d.FrameA.Origin.Length(), d.FrameB.Origin.Length(), d.LowerTranslation, d.UpperTranslation, d.RestLength])
            PhysicsJointRuntime.ValidateExtent(value);
        if (!float.IsFinite(d.LowerAngle) || !float.IsFinite(d.UpperAngle) || d.LimitEnabled && (d.LowerAngle < -0.99f * MathF.PI || d.UpperAngle > 0.99f * MathF.PI || d.LowerAngle > d.UpperAngle) ||
            d.LowerTranslation > d.UpperTranslation || !float.IsFinite(d.MotorVelocity) || !float.IsFinite(d.MotorMaxTorque * 10_000) || d.MotorMaxTorque < 0 ||
            d.RestLength < 0 || !float.IsFinite(d.Stiffness) || d.Stiffness < 0 || !float.IsFinite(d.Damping) || d.Damping < 0)
            throw new ArgumentOutOfRangeException(nameof(d));
    }
    private ref int JointNext(int index, int body) => ref (_jointSlots[index].Definition.BodyA.Index == body ? ref _jointSlots[index].NextA : ref _jointSlots[index].NextB);
    private ref int JointPrevious(int index, int body) => ref (_jointSlots[index].Definition.BodyA.Index == body ? ref _jointSlots[index].PreviousA : ref _jointSlots[index].PreviousB);
    private void LinkJoint(int index, int body)
    {
        ref var first = ref _slots[body].FirstJoint;
        JointPrevious(index, body) = -1; JointNext(index, body) = first;
        if (first >= 0) JointPrevious(first, body) = index;
        first = index;
    }
    private void UnlinkJoint(int index, int body)
    {
        var previous = JointPrevious(index, body); var next = JointNext(index, body);
        if (previous < 0) _slots[body].FirstJoint = next; else JointNext(previous, body) = next;
        if (next >= 0) JointPrevious(next, body) = previous;
    }
    private void RemoveBodyJoints(BodyHandle body)
    {
        while (_slots[body.Index].FirstJoint is var index && index >= 0)
            RemoveJoint(new(index, _jointSlots[index].Generation, _identity));
    }
    private void WakeJoint(in JointDefinition definition)
    {
        Wake(definition.BodyA.Index, true);
        if (definition.BodyB != default) Wake(definition.BodyB.Index, true);
    }
    private void MarkJoint(int index)
    {
        WakeJoint(_jointSlots[index].Definition);
        if (!_jointSlots[index].Dirty) { _jointSlots[index].Dirty = true; _dirtyJoints.Add(index); }
        _pairBodyVersion = -1;
    }
    private void FlushJoints()
    {
        if (_dirtyJoints.Count == 0) return;
        try
        {
            Grow(ref _jointsGPU, ref _jointCapacity, _jointHighWater, sizeof(JointData), true);
            Grow(ref _jointStatesGPU, ref _jointStateCapacity, _jointHighWater, 48, true);
            Grow(ref _jointFiltersGPU, ref _jointFilterCapacity, checked(2 * _jointHighWater), 4, false);
            Grow(ref _jointEditsGPU, ref _jointEditCapacity, _dirtyJoints.Count, sizeof(JointEdit), false);
            if (_jointEdits.Length < _dirtyJoints.Count) Array.Resize(ref _jointEdits, Capacity(_dirtyJoints.Count));
            for (var i = 0; i < _dirtyJoints.Count; i++)
            {
                var index = _dirtyJoints[i]; ref var slot = ref _jointSlots[index]; var d = slot.Definition;
                _jointEdits[i] = new()
                {
                    Index = (uint)index,
                    Value = new()
                    {
                        Generation = slot.Generation,
                        Type = slot.Alive ? (uint)d.Type : 3,
                        A = (uint)d.BodyA.Index,
                        B = d.BodyB == default ? uint.MaxValue : (uint)d.BodyB.Index,
                        GenerationA = d.BodyA.Generation,
                        GenerationB = d.BodyB.Generation,
                        Flags = (d.DisableCollision ? 4u : 0u) | (d.LimitEnabled ? 1u : 0u) | (d.MotorEnabled ? 2u : 0u),
                        FrameA = new(d.FrameA.Origin.X, d.FrameA.Origin.Y, d.FrameA.X.X, d.FrameA.X.Y),
                        FrameB = new(d.FrameB.Origin.X, d.FrameB.Origin.Y, d.FrameB.X.X, d.FrameB.X.Y),
                        Limits = new(d.LowerTranslation, d.UpperTranslation, d.LowerAngle, d.UpperAngle),
                        MotorSpring = new(d.MotorVelocity, d.MotorMaxTorque * 10_000, d.RestLength, d.Stiffness),
                        Policy = new(d.Damping, 0, 0, 0)
                    }
                };
            }
            UploadJoints();
            foreach (var index in _dirtyJoints) _jointSlots[index].Dirty = false;
            Array.Clear(_jointEdits, 0, _dirtyJoints.Count); _dirtyJoints.Clear();
        }
        catch { _failed = true; throw; }
    }

    private void UploadJoints()
    {
        _jointEditPipeline ??= _context.CreatePipeline("PhysicsResidentJointEdits.comp.spv");
        _spatialSummary ??= Buffer(8);
        var bytes = checked(_dirtyJoints.Count * sizeof(JointEdit));
        GrowTransfer(ref _spatialUpload, ref _spatialUploadBytes, checked(8 + bytes), SDL.GPUTransferBufferUsage.Upload);
        GrowTransfer(ref _spatialDownload, ref _spatialDownloadBytes, 8, SDL.GPUTransferBufferUsage.Download);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire resident joint edits");
        try
        {
            var mapped = SDL.MapGPUTransferBuffer(Device, _spatialUpload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map resident joint edits");
            *(ulong*)mapped = 0;
            fixed (JointEdit* source = _jointEdits) System.Buffer.MemoryCopy(source, (void*)(mapped + 8), bytes, bytes);
            SDL.UnmapGPUTransferBuffer(Device, _spatialUpload.DangerousGetHandle());
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin resident joint upload");
            UploadSpatial(copy, _spatialSummary, 0, 8); UploadSpatial(copy, _jointEditsGPU!, 8, (uint)bytes); SDL.EndGPUCopyPass(copy);
            var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[4];
            for (uint stage = 0; stage < 3; stage++)
            {
                outputs[0] = new() { Buffer = _jointsGPU!.DangerousGetHandle() }; outputs[1] = new() { Buffer = _jointStatesGPU!.DangerousGetHandle() };
                outputs[2] = new() { Buffer = _jointFiltersGPU!.DangerousGetHandle() }; outputs[3] = new() { Buffer = _spatialSummary.DangerousGetHandle() };
                var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 4);
                if (compute == 0) throw GPUPhysicsDevice.Failure("begin resident joint edits");
                SDL.BindGPUComputePipeline(compute, _jointEditPipeline.DangerousGetHandle());
                var input = _jointEditsGPU!.DangerousGetHandle(); SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)(&input), 1);
                var settings = new JointEditSettings { Stage = stage, Count = (uint)(stage == 0 ? _dirtyJoints.Count : stage == 1 ? _jointFilterCapacity : _jointHighWater), Joints = (uint)_jointHighWater, Capacity = (uint)_jointFilterCapacity };
                SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(JointEditSettings));
                SDL.DispatchGPUCompute(compute, (settings.Count + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute);
            }
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin resident joint status");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _spatialSummary.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _spatialDownload!.DangerousGetHandle() }); SDL.EndGPUCopyPass(copy);
            Finish(ref command); UploadBytes += bytes + 8; JointUploadBytes += bytes; ReadbackBytes += 8; UniformBytes += 3 * sizeof(JointEditSettings);
            mapped = SDL.MapGPUTransferBuffer(Device, _spatialDownload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map resident joint status");
            try { if (*(uint*)mapped != 0) throw new InvalidOperationException("GPU joint edits returned invalid identities or filter storage."); }
            finally { SDL.UnmapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle()); }
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }
    private void DisposeJoints() { _jointsGPU?.Dispose(); _jointStatesGPU?.Dispose(); _jointEditsGPU?.Dispose(); _jointFiltersGPU?.Dispose(); _jointEditPipeline?.Dispose(); _jointPreparePipeline?.Dispose(); }
}
