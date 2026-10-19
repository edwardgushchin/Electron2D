namespace Electron2D;

internal sealed class CanvasParticles(RenderingParticleState source)
{
    private readonly List<CanvasMesh> _meshes = [];
    internal Rect2 Bounds() => source.Released ? default : source.CustomAABB;
    internal void Append(List<CanvasVertex> vertices, List<CanvasBatch> batches, Texture? texture, Transform transform, Color color,
        MaterialState? material, BlendMode blend, TextureFilter filter, TextureRepeat repeat, int anisotropy, Rect2i? clip, bool snap, bool linear, CanvasItem owner)
    {
        if (source.Released || source.Amount == 0) return;
        source.Owner.RequestParticleCanvas(source); var revision = source.Revision; source.Owner.BeginParticleScenePreparation(); source.Preparing = true;
        try
        {
            while (_meshes.Count < source.DrawPasses.Length) _meshes.Add(null!);
            var canvas = source.LocalCoordinates ? transform : transform * source.Emission.AffineInverse();
            var visibility = transform * source.CustomAABB;
            source.CanvasBasis = canvas;
            var sheet = owner.CanvasMaterial is CanvasItemMaterial fixedMaterial && fixedMaterial.ParticlesAnimation ? new Vector4(fixedMaterial.ParticlesAnimHFrames, fixedMaterial.ParticlesAnimVFrames, fixedMaterial.ParticlesAnimLoop ? 1 : 0, 1) : default;
            if (linear) color = CanvasGeometry.LinearColor(color);
            for (var pass = 0; pass < source.DrawPasses.Length; pass++)
            {
                var mesh = source.DrawPasses[pass]; if (mesh is null) continue; ObjectDisposedException.ThrowIf(mesh.IsDisposed, mesh);
                _meshes[pass] ??= new(mesh, Transform.Identity, Colors.White); _meshes[pass].Set(mesh, Transform.Identity, Colors.White);
                _meshes[pass].AppendParticles(vertices, batches, source, texture, canvas, visibility, color, material, blend, filter, repeat, anisotropy, clip, snap, linear, sheet, owner);
                if (source.Released || source.Revision != revision) throw new InvalidOperationException("Mesh callbacks changed particle geometry ownership.");
            }
        }
        finally { source.Preparing = false; source.Owner.EndParticleScenePreparation(); }
    }
}

internal readonly record struct ParticleGPUDraw(RenderingParticleState Source, ParticleGeometry Geometry, Transform Canvas, Color Color, Vector4 Sheet);

internal sealed unsafe class ParticleGeometry
{
    private MeshSurfaceData? _data;
    private long _revision = -1;
    internal int[] Words = [];
    internal RenderHandle? Buffer;
    private bool _dirty = true;
    internal void Set(MeshSurfaceData data, long revision, int vertexCount)
    {
        if (ReferenceEquals(_data, data) && _revision == revision) return;
        _data = data; _revision = revision; var size = Math.Max(16, checked(vertexCount * 16));
        if (Words.Length != size) Words = new int[size]; else Array.Clear(Words);
        if (data.Bones.Length != 0)
            for (var vertex = 0; vertex < vertexCount; vertex++)
                for (var slot = 0; slot < data.SkinSlots; slot++)
                { Words[vertex * 16 + slot] = data.Bones[vertex * data.SkinSlots + slot]; Words[vertex * 16 + 8 + slot] = BitConverter.SingleToInt32Bits(data.Weights[vertex * data.SkinSlots + slot]); }
        _dirty = true;
    }
    internal nint Prepare(ParticleGPUState state, RenderHandle device, nint command)
    {
        if (_dirty || Buffer is null)
        {
            Buffer?.Dispose(); var native = device.DangerousGetHandle();
            Buffer = new(SDL3.SDL.CreateGPUBuffer(native, new() { Size = checked((uint)Words.Length * 4), Usage = SDL3.SDL.GPUBufferUsageFlags.ComputeStorageRead }), h => SDL3.SDL.ReleaseGPUBuffer(native, h), device);
            state.Upload(command, Buffer, 0, System.Runtime.InteropServices.MemoryMarshal.AsBytes(Words.AsSpan())); _dirty = false;
        }
        return Buffer!.DangerousGetHandle();
    }
    internal void CancelPreparedUpload() => _dirty = true;
    internal bool HasSkin => _data?.Bones.Length > 0;
}
