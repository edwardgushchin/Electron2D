using System.Runtime.InteropServices;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    internal readonly record struct PortableOneWay(ShapeHandle A, int PieceA, ShapeHandle B, int PieceB, bool Allowed);
    [StructLayout(LayoutKind.Sequential)]
    private struct PortableOneWayRecord
    {
        internal uint A, B, GenerationA, GenerationB, PieceA, PieceB, Decision, Padding;
        internal uint ShapeRevisionA, ShapeRevisionB, GeometryRevisionA, GeometryRevisionB;
    }
    private PortableOneWayRecord[] _portableOneWays = [];
    private uint[] _portableOneWayTable = [];

    internal void SetPortableMotion(BodyHandle body, Transform pose, Vector2 linear, float angular, float sleepTime, bool canSleep, bool sleeping,
        Vector2 surface, float surfaceAngular, Vector2 force, float torque, Vector2 gravity, float linearDamp, float angularDamp, bool initialized)
    {
        Validate(body); ref var slot = ref _slots[body.Index];
        slot.CanSleep = canSleep; slot.Surface = new(surface.X, surface.Y, surfaceAngular, slot.Surface.W);
        ref var command = ref Edit(body.Index);
        if ((command.Mask & ~(128u | 1024u)) != 0) throw new InvalidOperationException("Portable motion requires flushed body authoring commands.");
        command.Mask = Pose | Velocity | Force | 65536 | 4194304;
        command.Body.Pose = new(pose.Origin.X, pose.Origin.Y, pose.X.X, pose.X.Y);
        command.Body.Velocity = new(linear.X, linear.Y, angular, sleepTime);
        command.Body.Surface = slot.Surface; command.Body.Force = new(force.X, force.Y, torque, 0);
        command.Body.Locks = (canSleep ? 0u : 8u) | (sleeping && slot.Mode >= PhysicsServer.BodyMode.Rigid ? 16u : 0u) | (initialized ? 8192u : 0u);
        // The portable edit uses the otherwise unused center lane for already-resolved observable fields.
        command.Center = new(gravity.X, gravity.Y, linearDamp, angularDamp);
    }
    internal void BeginPortableRestore()
    {
        EnsureAccess();
        _previousPointCount = 0; _previousSolveDelta = 0; _checkpointRebuildHistory = false; _hasPositionCorrections = false;
        _oneWayHistoryCount = 0; _oneWayReadCapacity = 0; _sleepGraphBodies = _sleepEdgeCount = 0;
        _sleepBodyVersion = _sleepShapeVersion = -1; _wakeAllSleep = false; _publishedSlots = 0;
        _reportCount = 0; _reportReady = false; _reportInitialize = true;
        _contactPairVersion = _pairBodyVersion = _boundsBodyVersion = -1;
        // Reusing the authoring upload path clears joint warm impulses without retaining foreign solver state.
        for (var i = 0; i < _jointHighWater; i++)
            if (_jointSlots[i].Alive && !_jointSlots[i].Dirty)
            { _jointSlots[i].Dirty = true; _dirtyJoints.Add(i); }
    }
    internal void CompletePortableRestore(ReadOnlySpan<PortableOneWay> pairs)
    {
        EnsureAccess(); Step(0, default); FlushJoints(); FindPairs(); ImportPortableOneWays(pairs);
        FindContacts();
        if (_highWater == 0) { ActiveSimulationBodyCount = PublishedActiveBodyCount = PublishedIslandCount = 0; return; }
        EnsureSleep(); Grow(ref _contactsGPU, ref _contactCapacity, 1, 64, false);
        Grow(ref _positionCorrectionsGPU, ref _positionCorrectionCapacity, _highWater, 16, false);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire restored sleep graph");
        try
        {
            var settings = SleepParameters(0); settings.PreviousBodies = settings.PreviousEdges = 0;
            // Build connectivity only. Applying an authoritative pose must not wake bodies or advance quiet clocks.
            SleepPass(command, settings, 3, _highWater, _spatialSummary!);
            SleepPass(command, settings, 4, ContactPointCount + _jointHighWater, _spatialSummary!);
            SleepPass(command, settings, 11, 1, _spatialSummary!);
            SleepPass(command, settings, 12, _highWater, _spatialSummary!);
            Finish(ref command); _sleepGraphBodies = _highWater; _sleepEdgeCount = ContactPointCount + _jointHighWater; _wakeAllSleep = false;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
        DownloadSpatial(_spatialSummary!, 0, 16);
        var mapped = SDL.MapGPUTransferBuffer(Device, _spatialDownload!.DangerousGetHandle(), false);
        if (mapped == 0) throw GPUPhysicsDevice.Failure("map restored sleep status");
        try
        {
            var stats = (uint*)mapped;
            if (stats[0] != 0) { _failed = true; throw new InvalidOperationException("Restored GPU connectivity is invalid."); }
            ActiveSimulationBodyCount = checked((int)stats[1]); PublishedActiveBodyCount = checked((int)stats[2]); PublishedIslandCount = checked((int)stats[3]);
        }
        finally { SDL.UnmapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle()); }
        _sleepBodyVersion = _sleepShapeVersion = -1;
    }
    internal void ValidatePortablePiece(ShapeHandle shape, int piece)
    {
        Validate(shape); var source = _shapeSlots[shape.Index].Geometry!.Source!; var geometry = source.GetGeometry();
        var count = geometry.Kind == PhysicsShapeGeometry.ShapeKind.ConcavePolygon ? geometry.Points.Length / 2 : 1;
        PhysicsSnapshotReader.Require(piece >= 0 && piece < count);
    }
    internal int ReadPortableOneWays(Span<PortableOneWay> destination)
    {
        EnsureAccess();
        if (destination.Length < _oneWayHistoryCount) throw new ArgumentException("One-way snapshot destination is too small.", nameof(destination));
        if (_oneWayHistoryCount == 0) return 0;
        DownloadSpatial(_oneWayHistoryGPU!, 0, checked((uint)(_oneWayHistoryCount * 48)));
        var mapped = SDL.MapGPUTransferBuffer(Device, _spatialDownload!.DangerousGetHandle(), false);
        if (mapped == 0) { _failed = true; throw GPUPhysicsDevice.Failure("map portable one-way history"); }
        try
        {
            var values = new ReadOnlySpan<PortableOneWayRecord>((void*)mapped, _oneWayHistoryCount); var count = 0;
            foreach (ref readonly var p in values)
            {
                if (p.A >= _shapeHighWater || p.B >= _shapeHighWater) continue;
                ref readonly var a = ref _shapeSlots[p.A]; ref readonly var b = ref _shapeSlots[p.B];
                if (!a.Alive || !b.Alive || a.Generation != p.GenerationA || b.Generation != p.GenerationB || a.Revision != p.ShapeRevisionA ||
                    b.Revision != p.ShapeRevisionB || (uint)a.Geometry!.Revision != p.GeometryRevisionA || (uint)b.Geometry!.Revision != p.GeometryRevisionB) continue;
                destination[count++] = new(new((int)p.A, p.GenerationA, _identity), (int)p.PieceA, new((int)p.B, p.GenerationB, _identity), (int)p.PieceB, p.Decision == 2);
            }
            return count;
        }
        finally { SDL.UnmapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle()); }
    }
    private static uint PortablePairHash(uint first, uint second)
    {
        var key = ((ulong)second << 32) | first; key ^= key >> 33; key = unchecked(key * 0xff51afd7ed558ccdUL);
        key ^= key >> 33; key = unchecked(key * 0xc4ceb9fe1a85ec53UL); return (uint)(key ^ (key >> 33));
    }
    private void ImportPortableOneWays(ReadOnlySpan<PortableOneWay> source)
    {
        _oneWayHistoryCount = 0;
        if (source.IsEmpty) return;
        EnsureOneWay(); var tableSize = Capacity(checked(source.Length * 2));
        Grow(ref _oneWayHistoryGPU, ref _oneWayHistoryCapacity, source.Length, 48, false);
        Grow(ref _oneWayHistoryTableGPU, ref _oneWayHistoryTableCapacity, tableSize, 4, false);
        if (_portableOneWays.Length < source.Length) Array.Resize(ref _portableOneWays, source.Length);
        if (_portableOneWayTable.Length < tableSize) Array.Resize(ref _portableOneWayTable, tableSize);
        _portableOneWayTable.AsSpan(0, tableSize).Fill(uint.MaxValue);
        for (var i = 0; i < source.Length; i++)
        {
            var p = source[i]; var a = p.A; var b = p.B; var pa = p.PieceA; var pb = p.PieceB;
            if (a.Index > b.Index) { (a, b) = (b, a); (pa, pb) = (pb, pa); }
            ref readonly var sa = ref _shapeSlots[a.Index]; ref readonly var sb = ref _shapeSlots[b.Index];
            var row = new PortableOneWayRecord
            {
                A = (uint)a.Index,
                B = (uint)b.Index,
                GenerationA = a.Generation,
                GenerationB = b.Generation,
                PieceA = (uint)pa,
                PieceB = (uint)pb,
                Decision = p.Allowed ? 2u : 1u,
                ShapeRevisionA = sa.Revision,
                ShapeRevisionB = sb.Revision,
                GeometryRevisionA = (uint)sa.Geometry!.Revision,
                GeometryRevisionB = (uint)sb.Geometry!.Revision
            };
            _portableOneWays[i] = row;
            var at = (PortablePairHash(row.A, row.B) ^ PortablePairHash(row.GenerationA, row.GenerationB) ^ PortablePairHash(row.PieceA, row.PieceB)) & (uint)(tableSize - 1);
            while (_portableOneWayTable[at] != uint.MaxValue) at = (at + 1) & (uint)(tableSize - 1);
            _portableOneWayTable[at] = (uint)i;
        }
        var recordBytes = checked(source.Length * 48); var tableBytes = checked(tableSize * 4); var bytes = checked(recordBytes + tableBytes);
        GrowTransfer(ref _spatialUpload, ref _spatialUploadBytes, bytes, SDL.GPUTransferBufferUsage.Upload);
        var mapped = SDL.MapGPUTransferBuffer(Device, _spatialUpload!.DangerousGetHandle(), false);
        if (mapped == 0) throw GPUPhysicsDevice.Failure("map portable one-way upload");
        try
        {
            var target = new Span<byte>((void*)mapped, bytes);
            MemoryMarshal.AsBytes(_portableOneWays.AsSpan(0, source.Length)).CopyTo(target);
            MemoryMarshal.AsBytes(_portableOneWayTable.AsSpan(0, tableSize)).CopyTo(target[recordBytes..]);
        }
        finally { SDL.UnmapGPUTransferBuffer(Device, _spatialUpload.DangerousGetHandle()); }
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire portable one-way upload");
        try
        {
            var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw GPUPhysicsDevice.Failure("begin portable one-way upload");
            UploadSpatial(copy, _oneWayHistoryGPU!, 0, (uint)recordBytes); UploadSpatial(copy, _oneWayHistoryTableGPU!, (uint)recordBytes, (uint)tableBytes);
            SDL.EndGPUCopyPass(copy); Finish(ref command); UploadBytes += bytes;
            _oneWayHistoryCount = source.Length; _oneWayReadCapacity = tableSize; _contactPairVersion = -1;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }
}
