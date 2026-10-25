using System.Runtime.InteropServices;
using Kind = Electron2D.RenderingServer.GlobalShaderParameterType;

namespace Electron2D;

internal sealed partial class ShaderGlobalParameters
{
    private abstract class Entry(Kind kind)
    {
        internal readonly Kind Kind = kind;
        internal abstract void Write(ShaderUniform uniform, Span<byte> target);
        internal abstract void Apply(ShaderGlobalParameters target, string name);
        internal abstract PropertyDescriptor SceneDescriptor(string name, bool stored);
        internal abstract void Reset();
        internal virtual void ValidateApply() { }
    }
    private sealed class Value<T>(Kind kind, T value) : Entry(kind) where T : unmanaged
    {
        internal T Base = value;
        internal T? Override;
        internal override PropertyDescriptor SceneDescriptor(string name, bool stored) => NumericDescriptor<T>(name, Kind, stored);
        internal override void Apply(ShaderGlobalParameters target, string name) => target.SetOverride<T>(name, Base);
        internal override void Reset() => Override = null;
        internal override void Write(ShaderUniform uniform, Span<byte> target)
        {
            var data = Override ?? Base;
            if (Kind == Kind.Transform2D)
            {
                var t = MemoryMarshal.Read<Transform>(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in data, 1)));
                Span<float> matrix = stackalloc float[9] { t.X.X, t.X.Y, 0, t.Y.X, t.Y.Y, 0, t.Origin.X, t.Origin.Y, 1 };
                uniform.WriteMatrix(target, matrix);
            }
            else uniform.Write(target, in data);
        }
    }
    private sealed class Matrix(Kind kind, ReadOnlySpan<float> value) : Entry(kind)
    {
        internal readonly float[] Base = value.ToArray();
        internal readonly float[] Override = new float[value.Length];
        internal bool HasOverride;
        internal override PropertyDescriptor SceneDescriptor(string name, bool stored) => MatrixDescriptor(name, Kind, stored);
        internal override void Apply(ShaderGlobalParameters target, string name) => target.SetMatrix(name, Base, temporary: true);
        internal override void Reset() => HasOverride = false;
        internal override void Write(ShaderUniform uniform, Span<byte> target) => uniform.WriteMatrix(target, HasOverride ? Override : Base);
    }
    private sealed class ImageValue(Kind kind, Resource? value) : Entry(kind)
    {
        internal Resource? Base = value, Override;
        internal Resource? Effective => Override ?? Base;
        internal override PropertyDescriptor SceneDescriptor(string name, bool stored) => ImageDescriptor(name, Kind, stored);
        internal override void ValidateApply() => CheckImage(Base, Kind == Kind.Sampler2DArray);
        internal override void Apply(ShaderGlobalParameters target, string name) => target.SetTexture(name, Base, Kind == Kind.Sampler2DArray, temporary: true);
        internal override void Reset() => Override = null;
        internal override void Write(ShaderUniform uniform, Span<byte> target) => throw new InvalidOperationException("A sampled global is not a numeric uniform.");
    }
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    internal void Add<T>(string name, Kind kind, T value) where T : unmanaged
    {
        ValidateName(name); Validate<T>(kind); ShaderMaterial.ValidateValue(in value); ValidateSceneRegistration(name, kind);
        if (!_entries.TryAdd(name, new Value<T>(kind, value))) throw new ArgumentException("The global name is already registered.", nameof(name));
        if (_scene is not null) RefreshSceneValue(name);
    }
    internal void AddMatrix(string name, Kind kind, ReadOnlySpan<float> value)
    {
        ValidateName(name); ValidateMatrix(kind, value); ValidateSceneRegistration(name, kind);
        if (!_entries.TryAdd(name, new Matrix(kind, value))) throw new ArgumentException("The global name is already registered.", nameof(name));
        if (_scene is not null) RefreshSceneValue(name);
    }
    internal void AddTexture(string name, Resource? value, bool layered)
    {
        ValidateName(name); CheckImage(value, layered); ValidateSceneRegistration(name, layered ? Kind.Sampler2DArray : Kind.Sampler2D);
        if (!_entries.TryAdd(name, new ImageValue(layered ? Kind.Sampler2DArray : Kind.Sampler2D, value))) throw new ArgumentException("The global name is already registered.", nameof(name));
        if (_scene is not null) RefreshSceneValue(name);
    }
    internal T Get<T>(string name) where T : unmanaged => Typed<T>(name).Base;
    internal void Set<T>(string name, T value) where T : unmanaged
    { var entry = Typed<T>(name); ShaderMaterial.ValidateValue(in value); entry.Base = value; }
    internal void SetOverride<T>(string name, T? value) where T : unmanaged
    { var entry = Typed<T>(name); if (value is { } data) ShaderMaterial.ValidateValue(in data); entry.Override = value; }
    internal float[] GetMatrix(string name) => (float[])MatrixEntry(name).Base.Clone();
    internal void GetMatrix(string name, Span<float> destination)
    {
        var value = MatrixEntry(name).Base;
        if (value.Length != destination.Length) throw new ArgumentException("The matrix destination has a different element count.", nameof(destination));
        value.CopyTo(destination);
    }
    internal void SetMatrix(string name, ReadOnlySpan<float> value, bool temporary = false)
    {
        var entry = MatrixEntry(name);
        if (temporary && value.IsEmpty) { entry.HasOverride = false; return; }
        ValidateMatrix(entry.Kind, value);
        if (temporary) { value.CopyTo(entry.Override); entry.HasOverride = true; }
        else value.CopyTo(entry.Base);
    }
    internal Resource? GetTexture(string name, bool layered) => ImageEntry(name, layered).Base;
    internal void SetTexture(string name, Resource? value, bool layered, bool temporary = false)
    {
        var entry = ImageEntry(name, layered); CheckImage(value, layered);
        if (temporary) entry.Override = value; else entry.Base = value;
    }
    internal string[] Names() => _entries.Keys.Order(StringComparer.Ordinal).ToArray();
    internal Kind GetType(string name) { ValidateName(name); return _entries.TryGetValue(name, out var entry) ? entry.Kind : Kind.Max; }
    internal void Clear() { _scene = null; _entries.Clear(); _sceneDescriptors.Clear(); }
    internal void Remove(string name) { ValidateName(name); _entries.Remove(name); }
    private Entry Find(string name)
    { ValidateName(name); return _entries.TryGetValue(name, out var entry) ? entry : throw new ArgumentException($"No global shader parameter named '{name}' exists.", nameof(name)); }
    private Value<T> Typed<T>(string name) where T : unmanaged => Find(name) as Value<T> ?? throw new ArgumentException("The global parameter has a different carrier type.", nameof(name));
    private Matrix MatrixEntry(string name) => Find(name) as Matrix ?? throw new ArgumentException("The global parameter is not a numeric matrix.", nameof(name));
    private ImageValue ImageEntry(string name, bool layered)
    {
        var entry = Find(name);
        return entry is ImageValue image && image.Kind == (layered ? Kind.Sampler2DArray : Kind.Sampler2D) ? image : throw new ArgumentException("The global parameter has a different image role.", nameof(name));
    }
    internal static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name is "TIME" or "SCREEN_PIXEL_SIZE" or "MODEL_MATRIX" or "CANVAS_MATRIX" or "SCREEN_MATRIX" or "SDF_COORDS" or "NORMAL_MAP_FLAGS" or "TEXTURE_PIXEL_SIZE" or "TEXTURE" or "SCREEN_TEXTURE" or "SDF_TEXTURE" || name.StartsWith("GLOBAL_", StringComparison.Ordinal))
            throw new ArgumentException("The name is reserved by the canvas shader interface.", nameof(name));
    }
    private static void Validate<T>(Kind kind) where T : unmanaged
    {
        var carrier = kind switch
        {
            Kind.Bool => typeof(bool),
            Kind.BVec2 or Kind.BVec3 or Kind.BVec4 or Kind.Int => typeof(int),
            Kind.IVec2 or Kind.UVec2 => typeof(Vector2i),
            Kind.IVec3 or Kind.UVec3 => typeof(Vector3i),
            Kind.IVec4 or Kind.UVec4 => typeof(Vector4i),
            Kind.Rect2i => typeof(Rect2i),
            Kind.UInt => typeof(uint),
            Kind.Float => typeof(float),
            Kind.Vec2 => typeof(Vector2),
            Kind.Vec3 => typeof(Vector3),
            Kind.Vec4 => typeof(Vector4),
            Kind.Color => typeof(Color),
            Kind.Rect2 => typeof(Rect2),
            Kind.Mat2 or Kind.Transform2D => typeof(Transform),
            _ => null
        };
        if (carrier != typeof(T)) throw new ArgumentException("The global role requires a different typed carrier.", nameof(kind));
    }
    private static void ValidateMatrix(Kind kind, ReadOnlySpan<float> value)
    {
        var count = kind == Kind.Mat3 ? 9 : kind == Kind.Mat4 ? 16 : 0;
        if (count == 0 || count != value.Length) throw new ArgumentException("Numeric globals require nine Mat3 or sixteen Mat4 elements.", nameof(value));
        foreach (var element in value) if (!float.IsFinite(element)) throw new ArgumentException("Matrix elements must be finite.", nameof(value));
    }
    private static void CheckImage(Resource? value, bool layered)
    {
        if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value));
        if (value is not null && (layered ? value is not TextureLayered : value is not Texture)) throw new ArgumentException("The global image has a different shape.", nameof(value));
    }
    private static bool Matches(Kind kind, ShaderUniform u)
    {
        if (u.ArrayLength != 0) return false;
        if (u.BooleanWidth != 0) return kind == (u.BooleanWidth switch { 1 => Kind.Bool, 2 => Kind.BVec2, 3 => Kind.BVec3, _ => Kind.BVec4 });
        return kind switch
        {
            Kind.Int => u.Type == typeof(int),
            Kind.UInt => u.Type == typeof(uint),
            Kind.IVec2 => u.Type == typeof(Vector2i) && !u.Unsigned,
            Kind.UVec2 => u.Type == typeof(Vector2i) && u.Unsigned,
            Kind.IVec3 => u.Type == typeof(Vector3i) && !u.Unsigned,
            Kind.UVec3 => u.Type == typeof(Vector3i) && u.Unsigned,
            Kind.IVec4 or Kind.Rect2i => u.Type == typeof(Vector4i) && !u.Unsigned,
            Kind.UVec4 => u.Type == typeof(Vector4i) && u.Unsigned,
            Kind.Float => u.Type == typeof(float),
            Kind.Vec2 => u.Type == typeof(Vector2),
            Kind.Vec3 => u.Type == typeof(Vector3),
            Kind.Vec4 or Kind.Color or Kind.Rect2 => u.Type == typeof(Vector4),
            Kind.Mat2 => u.Type == typeof(Transform),
            Kind.Mat3 or Kind.Transform2D => u.MatrixColumns == 3 && u.MatrixRows == 3,
            Kind.Mat4 => u.MatrixColumns == 4 && u.MatrixRows == 4,
            _ => false
        };
    }
    internal void Validate(ShaderProgram program)
    {
        foreach (var u in program.GlobalUniforms.Values)
            if (!Matches(Find(u.Name).Kind, u)) throw new ArgumentException($"Global shader parameter '{u.Name}' has a different logical type.");
    }
    internal void Write(ShaderProgram program, byte[][] buffers)
    {
        foreach (var u in program.GlobalUniforms.Values)
        {
            var entry = Find(u.Name);
            if (!Matches(entry.Kind, u)) throw new ArgumentException($"Global shader parameter '{u.Name}' has a different logical type.");
            entry.Write(u, buffers[u.Buffer].AsSpan(u.Offset, u.ElementSize));
        }
    }
    internal Resource? Resolve(ShaderTexture texture)
    {
        var entry = ImageEntry(texture.GlobalName, texture.IsArray);
        var value = entry.Effective;
        if (value is null && texture.ImagePolicy is null) throw new InvalidOperationException($"Global image '{texture.GlobalName}' has no effective resource.");
        texture.Validate(value); return value;
    }
}
