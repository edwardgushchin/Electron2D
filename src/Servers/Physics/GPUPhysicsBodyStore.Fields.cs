using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    /// <summary>Authored Area/default fields in scene units; default-world modes and priority do not stop fallback.</summary>
    internal readonly record struct FieldParameters(Vector2 GravityVector, float Gravity = 0, bool GravityPoint = false,
        float PointUnitDistance = 0, float LinearDamp = 0, float AngularDamp = 0,
        Area.SpaceOverride GravityMode = Area.SpaceOverride.Disabled, Area.SpaceOverride LinearMode = Area.SpaceOverride.Disabled,
        Area.SpaceOverride AngularMode = Area.SpaceOverride.Disabled, int Priority = 0);
    [StructLayout(LayoutKind.Sequential)]
    private record struct FieldData : IComparable<FieldData>
    {
        internal uint Body, Generation, GravityMode, LinearMode, AngularMode, Point;
        internal int Priority;
        internal uint Order;
        internal Float4 Gravity, Damping;
        internal readonly bool HasOverrides => (GravityMode | LinearMode | AngularMode) != 0;
        public readonly int CompareTo(FieldData other)
        { var priority = other.Priority.CompareTo(Priority); return priority != 0 ? priority : Order.CompareTo(other.Order); }
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct FieldSettings
    {
        internal uint Stage, Count, Bodies, Points, Areas;
        internal float Delta;
        internal uint Padding1, Padding2;
        internal FieldData Default;
    }
    private readonly Dictionary<int, FieldData> _areaFields = [];
    private FieldData[] _fieldEdits = [];
    private RenderHandle? _fieldPipeline, _fieldProfilesGPU, _fieldHeadsGPU, _fieldLinksGPU, _fieldLookupGPU, _fieldUpload, _fieldDownload, _fieldStatus;
    private int _fieldProfileCapacity, _fieldHeadCapacity, _fieldLinkCapacity, _fieldLookupCapacity, _fieldUploadCapacity;
    private uint _fieldOrder;
    private int _activeAreaCount;
    private long _fieldVersion, _steppedFieldVersion = -1;
    private bool _fieldEditsDirty;
    private FieldParameters? _steppedDefaults;
    internal long FieldSubmissionCount { get; private set; }
    internal double FieldMS { get; private set; }
    internal double FieldWaitMS { get; private set; }

    internal void SetAreaFields(BodyHandle area, in FieldParameters fields)
    {
        Validate(area); ValidateFields(fields);
        if (_slots[area.Index].Mode != PhysicsServer.BodyMode.Static) throw new InvalidOperationException("An Area field owner must be static.");
        for (var shape = _slots[area.Index].FirstShape; shape >= 0; shape = _shapeSlots[shape].NextOnBody)
            ValidateFieldShape(area.Index, _shapeSlots[shape].Sensor, attachingArea: true);
        var order = _areaFields.TryGetValue(area.Index, out var previous) ? previous.Order : checked(++_fieldOrder);
        var value = PackFields(fields, (uint)area.Index, area.Generation, order);
        if (_areaFields.ContainsKey(area.Index) && previous.Equals(value)) return;
        _activeAreaCount += (value.HasOverrides ? 1 : 0) - (previous.HasOverrides ? 1 : 0);
        _areaFields[area.Index] = value; _fieldEditsDirty = true; _fieldVersion++;
    }
    internal void RemoveAreaFields(BodyHandle area)
    {
        Validate(area);
        if (_areaFields.Remove(area.Index, out var previous)) { if (previous.HasOverrides) _activeAreaCount--; _fieldEditsDirty = true; _fieldVersion++; }
    }
    private void ValidateFieldShape(int body, bool sensor, bool attachingArea = false)
    {
        if (!sensor && (attachingArea || _areaFields.ContainsKey(body))) throw new InvalidOperationException("Area field geometry must be a sensor.");
    }
    private static void ValidateFields(in FieldParameters fields)
    {
        PhysicsAreaFields.ValidateFinite(fields.GravityVector); PhysicsAreaFields.ValidateFinite(fields.Gravity);
        PhysicsAreaFields.ValidateFinite(fields.PointUnitDistance); PhysicsAreaFields.ValidateFinite(fields.LinearDamp); PhysicsAreaFields.ValidateFinite(fields.AngularDamp);
        PhysicsAreaFields.ValidateMode(fields.GravityMode); PhysicsAreaFields.ValidateMode(fields.LinearMode); PhysicsAreaFields.ValidateMode(fields.AngularMode);
    }
    private static FieldData PackFields(in FieldParameters fields, uint body = uint.MaxValue, uint generation = 0, uint order = 0) => new()
    {
        Body = body,
        Generation = generation,
        Priority = fields.Priority,
        Order = order,
        GravityMode = (uint)fields.GravityMode,
        LinearMode = (uint)fields.LinearMode,
        AngularMode = (uint)fields.AngularMode,
        Point = fields.GravityPoint ? 1u : 0u,
        Gravity = new(fields.GravityVector.X, fields.GravityVector.Y, fields.Gravity, fields.PointUnitDistance),
        Damping = new(fields.LinearDamp, fields.AngularDamp, 0, 0)
    };
    private static bool UniformFields(in FieldParameters fields) => !fields.GravityPoint && fields.LinearDamp == 0 && fields.AngularDamp == 0;

    private bool PrepareFields(in FieldParameters defaults, float delta, float margin)
    {
        if (_activeAreaCount == 0 && UniformFields(defaults)) return false;
        var start = Stopwatch.GetTimestamp(); var wait = WaitMS;
        Step(0, default);
        if (_activeAreaCount > 0) FindContacts(margin, 0.05f);
        _fieldPipeline ??= _context.CreatePipeline("PhysicsResidentFields.comp.spv");
        Grow(ref _fieldProfilesGPU, ref _fieldProfileCapacity, Math.Max(1, _activeAreaCount), sizeof(FieldData), false);
        Grow(ref _fieldHeadsGPU, ref _fieldHeadCapacity, _highWater, 4, false);
        Grow(ref _fieldLookupGPU, ref _fieldLookupCapacity, _highWater, 4, false);
        Grow(ref _fieldLinksGPU, ref _fieldLinkCapacity, Math.Max(1, checked(2 * ContactPointCount)), 8, false);
        _fieldStatus ??= Buffer(8); _fieldDownload ??= Transfer(8, SDL.GPUTransferBufferUsage.Download);
        var editBytes = _fieldEditsDirty ? checked(_activeAreaCount * sizeof(FieldData)) : 0;
        GrowTransfer(ref _fieldUpload, ref _fieldUploadCapacity, checked(8 + editBytes), SDL.GPUTransferBufferUsage.Upload);
        if (_fieldEditsDirty)
        {
            if (_fieldEdits.Length < _activeAreaCount) Array.Resize(ref _fieldEdits, Capacity(_activeAreaCount));
            var at = 0; foreach (var field in _areaFields.Values) if (field.HasOverrides) _fieldEdits[at++] = field;
            Array.Sort(_fieldEdits, 0, _activeAreaCount);
        }
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire resident field reduction");
        try
        {
            var mapped = SDL.MapGPUTransferBuffer(Device, _fieldUpload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map resident field definitions");
            *(ulong*)mapped = 0;
            fixed (FieldData* input = _fieldEdits) System.Buffer.MemoryCopy(input, (byte*)mapped + 8, editBytes, editBytes);
            SDL.UnmapGPUTransferBuffer(Device, _fieldUpload.DangerousGetHandle());
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin field definitions");
            SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _fieldUpload.DangerousGetHandle() }, new() { Buffer = _fieldStatus.DangerousGetHandle(), Size = 8 }, false);
            if (editBytes > 0) SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _fieldUpload.DangerousGetHandle(), Offset = 8 }, new() { Buffer = _fieldProfilesGPU!.DangerousGetHandle(), Size = (uint)editBytes }, false);
            SDL.EndGPUCopyPass(copy);
            var settings = new FieldSettings
            {
                Bodies = (uint)_highWater,
                Areas = (uint)_activeAreaCount,
                Points = _activeAreaCount > 0 ? (uint)ContactPointCount : 0,
                Default = PackFields(defaults),
                Delta = delta
            };
            FieldPass(command, settings, 0, _highWater);
            FieldPass(command, settings, 1, _activeAreaCount);
            FieldPass(command, settings, 2, (int)settings.Points);
            FieldPass(command, settings, 3, _highWater);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin field status");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _fieldStatus.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _fieldDownload.DangerousGetHandle() });
            SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command);
            UploadBytes += 8 + editBytes; ReadbackBytes += 8; FieldSubmissionCount++;
            mapped = SDL.MapGPUTransferBuffer(Device, _fieldDownload.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map field status");
            try { if (*(uint*)mapped != 0) throw new InvalidOperationException("GPU field reduction returned nonfinite fields or invalid membership."); }
            finally { SDL.UnmapGPUTransferBuffer(Device, _fieldDownload.DangerousGetHandle()); }
            _failed = false; _fieldEditsDirty = false;
            FieldMS += Stopwatch.GetElapsedTime(start).TotalMilliseconds; FieldWaitMS += WaitMS - wait;
            return true;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }
    private void FieldPass(nint command, FieldSettings settings, uint stage, int count)
    {
        if (count == 0) return;
        var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[6];
        outputs[0] = new() { Buffer = _bodies!.DangerousGetHandle() }; outputs[1] = new() { Buffer = _resolvedFields!.DangerousGetHandle() };
        outputs[2] = new() { Buffer = _fieldHeadsGPU!.DangerousGetHandle() }; outputs[3] = new() { Buffer = _fieldLinksGPU!.DangerousGetHandle() };
        outputs[4] = new() { Buffer = _fieldLookupGPU!.DangerousGetHandle() }; outputs[5] = new() { Buffer = _fieldStatus!.DangerousGetHandle() };
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 6);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin field compute");
        SDL.BindGPUComputePipeline(compute, _fieldPipeline!.DangerousGetHandle());
        var inputs = stackalloc nint[3] { _fieldProfilesGPU!.DangerousGetHandle(), (_contactsGPU ?? _bodies!).DangerousGetHandle(),
            (_shapesGPU ?? _bodies!).DangerousGetHandle() };
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 3);
        settings.Stage = stage; settings.Count = (uint)count;
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(FieldSettings));
        SDL.DispatchGPUCompute(compute, ((uint)count + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute);
        UniformBytes += sizeof(FieldSettings);
    }
    private void DisposeFields()
    { _fieldPipeline?.Dispose(); _fieldProfilesGPU?.Dispose(); _fieldHeadsGPU?.Dispose(); _fieldLinksGPU?.Dispose(); _fieldLookupGPU?.Dispose(); _fieldStatus?.Dispose(); _fieldUpload?.Dispose(); _fieldDownload?.Dispose(); }
}
