using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class MultiMeshGPUTests
{
    private static void Contracts(RenderingDevice device)
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad()); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [Vector2.Zero, Vector2.Right, Vector2.Down] });
        for (var schema = 0; schema < 4; schema++)
        {
            var colors = (schema & 1) != 0; var custom = (schema & 2) != 0; var stride = 8 + (colors ? 4 : 0) + (custom ? 4 : 0);
            var owned = RenderingServer.MultiMeshCreate(); RenderingServer.MultiMeshAllocateData(owned, 2, colors, custom, true); RenderingServer.MultiMeshSetMesh(owned, mesh.GetRID());
            var storage = RenderingServer.MultiMeshGetBufferRDRID(owned); var command = RenderingServer.MultiMeshGetCommandBufferRDRID(owned);
            Check(device.BufferGetData(storage).Length == 2 * stride * 4, "Exported storage retains the complete canonical schema.");
            var initial = MemoryMarshal.Cast<byte, uint>(device.BufferGetData(command)).ToArray(); Check(initial.SequenceEqual(new uint[] { 6, 0, 0, 0, 0, 3, 0, 0, 0, 0 }), "Both indexed and sequential surfaces initialize count and zero instance fields.");
            var data = Enumerable.Range(0, stride * 2).Select(i => (float)(i + 1)).ToArray(); device.BufferUpdate(storage, 0, (uint)(data.Length * 4), MemoryMarshal.AsBytes(data.AsSpan()));
            var pose = new Transform(0, new(12, 16)); RenderingServer.MultiMeshInstanceSetTransform2D(owned, 0, pose); if (custom) RenderingServer.MultiMeshInstanceSetCustomData(owned, 1, Colors.Blue);
            var actual = MemoryMarshal.Cast<byte, float>(device.BufferGetData(storage)).ToArray();
            var expected = (float[])data.Clone(); new float[] { 1, 0, 0, 12, 0, 1, 0, 16 }.CopyTo(expected, 0); if (custom) new float[] { 0, 0, 1, 1 }.CopyTo(expected, stride + 8 + (colors ? 4 : 0));
            Check(actual.SequenceEqual(expected), "Disjoint authored writes preserve GPU-only records and untouched channels.");
            RenderingServer.MultiMeshSetVisibleInstances(owned, 1); var visible = MemoryMarshal.Cast<byte, uint>(device.BufferGetData(command)); Check(visible[1] == 1 && visible[6] == 1, "Visibility updates each surface command.");
            RenderingServer.MultiMeshSetVisibleInstances(owned, -1); visible = MemoryMarshal.Cast<byte, uint>(device.BufferGetData(command)); Check(visible[1] == 2 && visible[6] == 2, "All-visible uses capacity instead of an unsigned sentinel.");
            RenderingServer.MultiMeshAllocateData(owned, 2, colors, custom, true); Check(RenderingServer.MultiMeshGetBufferRDRID(owned) == storage, "Identical allocation preserves storage and GPU data.");
            var list = device.ComputeListBegin(); Reject<InvalidOperationException>(() => RenderingServer.MultiMeshGetBufferRDRID(owned)); device.ComputeListEnd();
            Task.Run(() => Reject<InvalidOperationException>(() => RenderingServer.MultiMeshGetBufferRDRID(owned))).GetAwaiter().GetResult();
            using var replacement = (ArrayMesh)mesh.Duplicate(); RenderingServer.MultiMeshSetMesh(owned, replacement.GetRID()); Reject<ArgumentException>(() => device.BufferGetData(command));
            var nextCommand = RenderingServer.MultiMeshGetCommandBufferRDRID(owned); Check(nextCommand != command && RenderingServer.MultiMeshGetBufferRDRID(owned) == storage, "Mesh replacement changes command ownership while preserving raw instance data.");
            Check(MemoryMarshal.Cast<byte, uint>(device.BufferGetData(nextCommand))[1] == 0, "Mesh replacement initializes new command counts to zero."); replacement.SurfaceSetName(0, "changed content"); Check(RenderingServer.MultiMeshGetCommandBufferRDRID(owned) == nextCommand, "Nonstructural mesh revisions preserve commands."); device.FreeRID(command); command = nextCommand; replacement.Dispose(); Reject<ArgumentException>(() => device.BufferGetData(command)); Check(device.BufferGetData(storage).Length == stride * 8, "Child mesh disposal expires commands and preserves independent raw instance storage."); RenderingServer.MultiMeshSetMesh(owned, default); Check(!RenderingServer.MultiMeshGetCommandBufferRDRID(owned).IsValid(), "Clearing the mesh removes command storage.");
            RenderingServer.MultiMeshAllocateData(owned, 2, colors, custom, false); Reject<ArgumentException>(() => device.BufferGetData(storage)); Reject<ArgumentException>(() => device.BufferGetData(command)); device.FreeRID(storage); device.FreeRID(command);
            storage = RenderingServer.MultiMeshGetBufferRDRID(owned); Check(storage.IsValid() && !RenderingServer.MultiMeshGetCommandBufferRDRID(owned).IsValid(), "Mode changes expire old storage and remove indirect commands.");
            RenderingServer.MultiMeshAllocateData(owned, 0, colors, custom); Check(!RenderingServer.MultiMeshGetBufferRDRID(owned).IsValid(), "Zero capacity has no native storage."); Reject<ArgumentException>(() => device.BufferGetData(storage)); device.FreeRID(storage); RenderingServer.FreeRID(owned);
        }
        using var borrowed = new MultiMesh { Mesh = mesh, InstanceCount = 1 }; var rid = RenderingServer.MultiMeshGetBufferRDRID(borrowed.GetRID());
        using (var local = RenderingServer.CreateLocalRenderingDevice()!) Reject<ArgumentException>(() => local.BufferGetData(rid));
        borrowed.InstanceCount = 2; Reject<ArgumentException>(() => device.BufferGetData(rid)); device.FreeRID(rid);
        var current = RenderingServer.MultiMeshGetBufferRDRID(borrowed.GetRID());
        using var copiedState = new MultiMesh { Mesh = mesh, InstanceCount = 2 }; copiedState.SetInstanceTransform2D(0, new(0, new(21, 13))); borrowed.CopyFromResource(copiedState);
        Check(RenderingServer.MultiMeshGetBufferRDRID(borrowed.GetRID()) == current && MemoryMarshal.Cast<byte, float>(device.BufferGetData(current)).SequenceEqual(copiedState.Buffer), "Compatible state copy retains native identity and uploads new authored records.");
        using var freshCopy = (MultiMesh)borrowed.Duplicate(); Check(freshCopy.GPUState is null, "Fresh copies own no device state.");
        borrowed.Dispose(); Reject<ArgumentException>(() => device.BufferGetData(current)); device.FreeRID(current);
        Reject<ArgumentException>(() => RenderingServer.MultiMeshGetBufferRDRID(default));
        History(device, mesh); Callbacks(device); Retirement(device, mesh);
    }
    private static void History(RenderingDevice device, Mesh mesh)
    {
        using var source = new MultiMesh { Mesh = mesh, UseColors = true, UseCustomData = true, InstanceCount = 2 }; source.SetInterpolationEnabled(true);
        var current = RenderingServer.MultiMeshGetBufferRDRID(source.GetRID()); var previous = source.GPUState!.Previous;
        var a = Enumerable.Range(0, 32).Select(i => (float)i).ToArray(); var b = a.Select(i => i + 100).ToArray();
        void Write(float[] data) => device.BufferUpdate(current, 0, 128, MemoryMarshal.AsBytes(data.AsSpan()));
        float[] Read(RID rid) => MemoryMarshal.Cast<byte, float>(device.BufferGetData(rid)).ToArray();
        Write(a); RenderingMultiMeshRegistry.PhysicsTick(true); Write(b); RenderingMultiMeshRegistry.PhysicsTick(false);
        Check(Read(previous).SequenceEqual(a) && Read(current).SequenceEqual(b), "Physics history snapshots actual native current records before a device write.");
        source.ResetInstancePhysicsInterpolation(1); var expected = (float[])a.Clone(); b.AsSpan(16).CopyTo(expected.AsSpan(16)); Check(Read(previous).SequenceEqual(expected), "Per-instance reset preserves unrelated native history.");
        source.ResetInstancesPhysicsInterpolation(); source.SetBufferInterpolated(a, b); Check(Read(current).SequenceEqual(a) && Read(previous).SequenceEqual(b), "Explicit snapshots replace pending resets atomically.");
        Write(b); source.ResetInstancesPhysicsInterpolation(); Check(Read(previous).SequenceEqual(b), "Full reset copies GPU values without uploading the authored cache.");
        source.Dispose(); device.FreeRID(current);
    }

    private static void Compatibility()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(Colors.White));
        using var borrowed = new MultiMesh { Mesh = mesh, InstanceCount = 1 }; borrowed.SetInstanceTransform2D(0, Transform.Identity);
        using var window = new Window { Size = new(96, 64) }; var draw = new Draw(); window.AddChild(draw); var frames = 0; RID owned = default;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetRenderingDevice() is null && !RenderingServer.MultiMeshGetBufferRDRID(borrowed.GetRID()).IsValid() && !RenderingServer.MultiMeshGetCommandBufferRDRID(borrowed.GetRID()).IsValid(), "Compatibility returns empty device identities.");
            owned = RenderingServer.MultiMeshCreate(); RenderingServer.MultiMeshAllocateData(owned, 1, true); RenderingServer.MultiMeshSetMesh(owned, mesh.GetRID()); RenderingServer.MultiMeshInstanceSetTransform2D(owned, 0, new(0, new(8, 8))); RenderingServer.MultiMeshInstanceSetColor(owned, 0, Colors.Red);
            Reject<NotSupportedException>(() => RenderingServer.MultiMeshAllocateData(owned, 2, false, true, true)); Check(RenderingServer.MultiMeshGetInstanceCount(owned) == 1 && RenderingServer.MultiMeshInstanceGetColor(owned, 0) == Colors.Red, "Unsupported allocation preserves the previous schema and data.");
            Check(!RenderingServer.MultiMeshGetBufferRDRID(owned).IsValid() && !RenderingServer.MultiMeshGetCommandBufferRDRID(owned).IsValid(), "Owned Compatibility buffers also remain absent."); Reject<ArgumentException>(() => RenderingServer.MultiMeshGetBufferRDRID(default)); draw.Instances = owned;
            RenderingServer.FramePostDraw += () => { using var pixels = window.GetTexture().GetImage()!; Check(pixels.GetPixel(12, 12).R > .9f, "Rejected indirect mode preserves actual Compatibility drawing."); if (++frames == 2) { RenderingServer.FreeRID(owned); window.Tree!.Quit(); } };
        };
        Check(Engine.Run(window) == 0 && frames == 2, "Compatibility rejection and retained output completed."); Console.WriteLine("GPU buffer Compatibility: empty getters, atomic indirect rejection and retained native drawing passed.");
    }

    private sealed class CallbackMesh : Mesh
    {
        internal Action? Callback;
        internal void Touch() => EmitChanged();
        protected override int OnGetSurfaceCount() => 1;
        protected override MeshSurfaceData OnSurfaceGetArrays(int index) => MeshTests.Quad(Colors.White);
        protected override int OnSurfaceGetArrayLen(int index) { Callback?.Invoke(); return 4; }
        protected override int OnSurfaceGetArrayIndexLen(int index) => 6;
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int index) => PrimitiveType.Triangles;
        protected override ArrayFormat OnSurfaceGetFormat(int index) => ArrayFormat.Vertex | ArrayFormat.Index;
        protected override Material? OnSurfaceGetMaterial(int index) => null;
        protected override void OnSurfaceSetMaterial(int index, Material? material) { }
    }
    private static void Callbacks(RenderingDevice device)
    {
        using var mesh = new CallbackMesh(); var source = RenderingServer.MultiMeshCreate(); RenderingServer.MultiMeshAllocateData(source, 1, useIndirect: true); RenderingServer.MultiMeshSetMesh(source, mesh.GetRID());
        mesh.Callback = () => Reject<InvalidOperationException>(() => RenderingServer.MultiMeshGetCommandBufferRDRID(source)); var commands = RenderingServer.MultiMeshGetCommandBufferRDRID(source); Check(commands.IsValid(), "Preparation rejects callback reentry and still completes its outer query.");
        mesh.Callback = () => RenderingServer.MultiMeshSetVisibleInstances(source, 0); mesh.Touch(); Reject<InvalidOperationException>(() => RenderingServer.MultiMeshGetCommandBufferRDRID(source));
        mesh.Callback = null; Check(RenderingServer.MultiMeshGetCommandBufferRDRID(source) == commands, "Unstable callback failure permits a stable retry without replacing matching geometry."); RenderingServer.FreeRID(source); device.FreeRID(commands);
    }

    private static void Retirement(RenderingDevice device, Mesh mesh)
    {
        var entries = (System.Collections.IDictionary)typeof(RenderingDevice).GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(device)!; var initial = entries.Count;
        for (var i = 0; i < 64; i++)
        {
            var source = RenderingServer.MultiMeshCreate(); RenderingServer.MultiMeshAllocateData(source, 1, useIndirect: true); RenderingServer.MultiMeshSetMesh(source, mesh.GetRID()); var commands = RenderingServer.MultiMeshGetCommandBufferRDRID(source); RenderingServer.FreeRID(source); device.FreeRID(commands);
            Check(entries.Count == initial, "Unexported instance/history storage retires without retaining device metadata per disposed resource.");
        }
    }

}
