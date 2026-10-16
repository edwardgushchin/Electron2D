using System.Globalization;
using Kind = Electron2D.RenderingServer.GlobalShaderParameterType;

namespace Electron2D;

internal sealed partial class ShaderGlobalParameters
{
    private ShaderGlobalParameters? _scene;
    private readonly Dictionary<string, PropertyDescriptor> _sceneDescriptors = new(StringComparer.Ordinal);
    internal IEnumerable<PropertyDescriptor> SceneDescriptors => _sceneDescriptors.Values;
    internal T? SceneValue<T>(string name) where T : unmanaged => GetType(name) == Kind.Max ? null : Get<T>(name);
    internal float[]? SceneMatrix(string name) => GetType(name) == Kind.Max ? null : GetMatrix(name);
    internal Resource? SceneImage(string name, bool layered) => GetType(name) == Kind.Max ? null : GetTexture(name, layered);
    internal void SceneSet<T>(string name, Kind kind, T? value) where T : unmanaged
    {
        ValidateName(name); Validate<T>(kind);
        if (value is not { } data) { SceneRemove(name); return; }
        if (GetType(name) == Kind.Max) Add(name, kind, data);
        else { RequireKind(name, kind); Set(name, data); }
        if (!_sceneDescriptors.ContainsKey(name)) _sceneDescriptors.Add(name, NumericDescriptor<T>(name, kind));
    }
    internal void SceneSetMatrix(string name, Kind kind, ReadOnlySpan<float> value)
    {
        ValidateName(name);
        if (kind is not (Kind.Mat3 or Kind.Mat4)) throw new ArgumentException("A numeric matrix requires Mat3 or Mat4.", nameof(kind));
        if (value.IsEmpty) { SceneRemove(name); return; }
        if (GetType(name) == Kind.Max) AddMatrix(name, kind, value);
        else { RequireKind(name, kind); SetMatrix(name, value); }
        if (!_sceneDescriptors.ContainsKey(name)) _sceneDescriptors.Add(name, MatrixDescriptor(name, kind));
    }
    internal void SceneSetImage(string name, Resource? value, bool layered)
    {
        ValidateName(name);
        if (value is null) { SceneRemove(name); return; }
        var kind = layered ? Kind.Sampler2DArray : Kind.Sampler2D;
        if (GetType(name) == Kind.Max) AddTexture(name, value, layered);
        else { RequireKind(name, kind); SetTexture(name, value, layered); }
        if (!_sceneDescriptors.ContainsKey(name)) _sceneDescriptors.Add(name, ImageDescriptor(name, kind));
    }
    private void SceneRemove(string name) { Remove(name); _sceneDescriptors.Remove(name); }
    private void RequireKind(string name, Kind kind)
    { if (GetType(name) != kind) throw new ArgumentException("Remove the authored parameter before changing its logical role.", nameof(kind)); }
    private void ValidateSceneRegistration(string name, Kind kind)
    {
        if (_scene is null || !_scene._entries.TryGetValue(name, out var source)) return;
        if (source.Kind != kind) throw new ArgumentException("The registered role conflicts with the active scene override.", nameof(kind));
        source.ValidateApply();
    }
    internal void ValidateSceneValue(string name, Kind kind)
    {
        if (_entries.TryGetValue(name, out var entry) && entry.Kind != kind)
            throw new ArgumentException($"Scene global '{name}' has a different registered logical role.", nameof(kind));
    }
    internal void BindScene(ShaderGlobalParameters? source)
    {
        if (ReferenceEquals(source, _scene)) return;
        var previous = _scene; _scene = null;
        if (previous is not null) foreach (var name in previous._entries.Keys)
                if (_entries.TryGetValue(name, out var entry)) entry.Reset();
        if (source is not null)
        {
            foreach (var pair in source._entries)
                if (_entries.ContainsKey(pair.Key)) { ValidateSceneValue(pair.Key, pair.Value.Kind); pair.Value.ValidateApply(); }
            foreach (var pair in source._entries) if (_entries.ContainsKey(pair.Key)) pair.Value.Apply(this, pair.Key);
        }
        _scene = source;
    }
    internal void RefreshSceneValue(string name)
    {
        if (!_entries.TryGetValue(name, out var target)) return;
        if (_scene?._entries.TryGetValue(name, out var source) == true)
        { ValidateSceneValue(name, source.Kind); source.Apply(this, name); }
        else target.Reset();
    }
    internal IEnumerable<PropertyDescriptor> DeclaredSceneDescriptors(ShaderGlobalParameters authored)
    {
        foreach (var pair in _entries)
            if (!authored._entries.ContainsKey(pair.Key)) yield return pair.Value.SceneDescriptor(pair.Key, stored: false);
    }
    private static string SceneName(string name, Kind kind) => "params/" + ((int)kind).ToString(CultureInfo.InvariantCulture) + "/" + name;
    private static PropertyDescriptor NumericDescriptor<T>(string name, Kind kind, bool stored = true) where T : unmanaged =>
        new PropertyDescriptor<ShaderGlobalsOverride, T?>(SceneName(name, kind), node => node.GetShaderParameter<T>(name),
            (node, value) => node.SetShaderParameter(name, kind, value), _ => null, stored: stored);
    private static PropertyDescriptor MatrixDescriptor(string name, Kind kind, bool stored = true) =>
        new PropertyDescriptor<ShaderGlobalsOverride, float[]?>(SceneName(name, kind), node => node.GetShaderParameterMatrix(name),
            (node, value) => node.SetShaderParameter(name, kind, value.AsSpan()), _ => null, stored: stored);
    private static PropertyDescriptor ImageDescriptor(string name, Kind kind, bool stored = true) => kind == Kind.Sampler2D
        ? new PropertyDescriptor<ShaderGlobalsOverride, Texture?>(SceneName(name, kind), node => node.GetShaderParameterTexture(name),
            (node, value) => node.SetShaderParameter(name, value), _ => null, stored: stored)
        : new PropertyDescriptor<ShaderGlobalsOverride, TextureLayered?>(SceneName(name, kind), node => node.GetShaderParameterLayeredTexture(name),
            (node, value) => node.SetShaderParameter(name, value), _ => null, stored: stored);
    internal static PropertyDescriptor? StoredSceneParameter(Node node, string path, Type type)
    {
        const string prefix = "params/";
        if (node is not ShaderGlobalsOverride || !path.StartsWith(prefix, StringComparison.Ordinal)) return null;
        var separator = path.IndexOf('/', prefix.Length);
        if (separator < 0 || !int.TryParse(path.AsSpan(prefix.Length, separator - prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var id)) return null;
        var kind = (Kind)id; var name = path[(separator + 1)..]; ValidateName(name);
        PropertyDescriptor? descriptor = kind switch
        {
            Kind.Bool => NumericDescriptor<bool>(name, kind),
            Kind.BVec2 or Kind.BVec3 or Kind.BVec4 or Kind.Int => NumericDescriptor<int>(name, kind),
            Kind.UInt => NumericDescriptor<uint>(name, kind),
            Kind.Float => NumericDescriptor<float>(name, kind),
            Kind.IVec2 or Kind.UVec2 => NumericDescriptor<Vector2i>(name, kind),
            Kind.IVec3 or Kind.UVec3 => NumericDescriptor<Vector3i>(name, kind),
            Kind.IVec4 or Kind.UVec4 => NumericDescriptor<Vector4i>(name, kind),
            Kind.Rect2i => NumericDescriptor<Rect2i>(name, kind),
            Kind.Vec2 => NumericDescriptor<Vector2>(name, kind),
            Kind.Vec3 => NumericDescriptor<Vector3>(name, kind),
            Kind.Vec4 => NumericDescriptor<Vector4>(name, kind),
            Kind.Color => NumericDescriptor<Color>(name, kind),
            Kind.Rect2 => NumericDescriptor<Rect2>(name, kind),
            Kind.Mat2 or Kind.Transform2D => NumericDescriptor<Transform>(name, kind),
            Kind.Mat3 or Kind.Mat4 => MatrixDescriptor(name, kind),
            Kind.Sampler2D or Kind.Sampler2DArray => ImageDescriptor(name, kind),
            _ => null
        };
        return descriptor?.ValueType == type && descriptor.Name == path ? descriptor : null;
    }
}
