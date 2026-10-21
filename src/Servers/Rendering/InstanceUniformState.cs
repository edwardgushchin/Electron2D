namespace Electron2D;

internal sealed class InstanceUniformState
{
    private abstract class Value
    {
        internal ShaderUniform? Declaration;
        internal bool Stored = true;
        internal PropertyDescriptor? PreparedDescriptor;
        internal abstract Type Type { get; }
        internal abstract void Write(ShaderUniform uniform, Span<byte> target);
        internal abstract PropertyDescriptor Descriptor(string name);
        internal bool Matches(ShaderUniform uniform)
        {
            if (Declaration is not null && !Declaration.SameType(uniform)) return false;
            if (Type != uniform.Type && !(Type == typeof(Color) && (uniform.Type == typeof(Vector3) || uniform.Type == typeof(Vector4))) && !(Type == typeof(Rect2) && uniform.Type == typeof(Vector4))) return false;
            Declaration ??= uniform;
            return true;
        }
    }
    private sealed class Value<T>(T value) : Value where T : unmanaged
    {
        internal T Data = value;
        internal override Type Type => typeof(T);
        internal override void Write(ShaderUniform uniform, Span<byte> target) => uniform.Write(target, in Data);
        internal override PropertyDescriptor Descriptor(string name) => new PropertyDescriptor<CanvasItem, T?>("instance_shader_parameters/" + name, item => item.GetStoredInstanceParameter<T>(name), (item, value) => item.SetInstanceShaderParameter<T>(name, value), _ => null, stored: true) { StoredSetter = (item, value) => item.RestoreInstanceParameter<T>(name, value) };
    }
    private readonly Dictionary<string, Value> _values = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PropertyDescriptor> _descriptors = new(StringComparer.Ordinal);
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ShaderProgram, byte[]?[]> _buffers = new();
    internal IEnumerable<PropertyDescriptor> StoredDescriptors => _descriptors.Values;
    internal bool HasStored(string name) => _values.TryGetValue(name, out var value) && value.Stored;
    internal T? GetStored<T>(string name, ShaderProgram? program) where T : unmanaged
    {
        if (!_values.TryGetValue(name, out var value) || !value.Stored) return null;
        if (program?.InstanceUniforms.TryGetValue(name, out var declaration) == true && !value.Matches(declaration)) return null;
        if (value is Value<T> exact) return exact.Data;
        return Get<T>(name, program);
    }
    internal bool Set<T>(string name, T? value, ShaderProgram? program) where T : unmanaged
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (value is not { } data) { if (_values.TryGetValue(name, out var cleared)) cleared.Stored = false; return _descriptors.Remove(name); }
        Supported<T>(); ShaderMaterial.ValidateValue(in data);
        ShaderUniform? uniform = null;
        program?.InstanceUniforms.TryGetValue(name, out uniform);
        if (uniform is not null && !uniform.Accepts<T>()) throw new ArgumentException("The instance parameter has a different logical type.", nameof(value));
        if (_values.TryGetValue(name, out var existing) && existing is Value<T> typed)
        {
            var added = !typed.Stored; typed.Data = data; typed.Declaration = uniform; typed.Stored = true;
            if (added) _descriptors[name] = typed.PreparedDescriptor!;
            return added;
        }
        var replacement = new Value<T>(data) { Declaration = uniform }; replacement.PreparedDescriptor = replacement.Descriptor(name);
        _values[name] = replacement; _descriptors[name] = replacement.PreparedDescriptor; return true;
    }
    internal T? Get<T>(string name, ShaderProgram? program, bool defaults = false) where T : unmanaged
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name); Supported<T>();
        ShaderUniform? uniform = null;
        program?.InstanceUniforms.TryGetValue(name, out uniform);
        if (uniform is not null && !uniform.Accepts<T>()) throw new ArgumentException("The instance parameter has a different logical type.", nameof(name));
        if (!defaults && _values.TryGetValue(name, out var stored) && stored.Stored)
        {
            if (uniform is not null)
            {
                if (!stored.Matches(uniform)) return Default<T>(uniform);
                Span<byte> bytes = stackalloc byte[16]; bytes.Clear(); stored.Write(uniform, bytes[..uniform.ElementSize]); return uniform.Read<T>(bytes[..uniform.ElementSize]);
            }
            if (stored is Value<T> typed) return typed.Data;
            throw new ArgumentException("The stored instance value has a different type.", nameof(name));
        }
        return uniform is null ? null : Default<T>(uniform);
    }
    private static T Default<T>(ShaderUniform uniform) where T : unmanaged => uniform.ReadDefault<T>();
    internal byte[]?[] Prepare(ShaderProgram program)
    {
        var buffers = _buffers.GetValue(program, static current =>
        {
            var result = new byte[]?[current.BufferSizes.Length];
            foreach (var uniform in current.InstanceUniforms.Values) result[uniform.Buffer] ??= new byte[current.BufferSizes[uniform.Buffer]];
            return result;
        });
        foreach (var buffer in buffers) buffer?.AsSpan().Clear();
        foreach (var uniform in program.InstanceUniforms.Values)
        {
            var target = buffers[uniform.Buffer]!.AsSpan(uniform.Offset, uniform.ElementSize);
            uniform.InitializeDefault(target);
            if (_values.TryGetValue(uniform.Name, out var value) && value.Stored && value.Matches(uniform)) value.Write(uniform, target);
        }
        return buffers;
    }
    internal static PropertyDescriptor? StoredParameter(Node node, string name, Type type)
    {
        const string prefix = "instance_shader_parameters/";
        if (node is not CanvasItem || !name.StartsWith(prefix, StringComparison.Ordinal) || name.Length == prefix.Length) return null;
        var parameter = name[prefix.Length..];
        return type == typeof(float?) ? new Value<float>(default).Descriptor(parameter) :
            type == typeof(bool?) ? new Value<bool>(default).Descriptor(parameter) :
            type == typeof(int?) ? new Value<int>(default).Descriptor(parameter) :
            type == typeof(uint?) ? new Value<uint>(default).Descriptor(parameter) :
            type == typeof(Vector2?) ? new Value<Vector2>(default).Descriptor(parameter) :
            type == typeof(Vector3?) ? new Value<Vector3>(default).Descriptor(parameter) :
            type == typeof(Vector4?) ? new Value<Vector4>(default).Descriptor(parameter) :
            type == typeof(Vector2i?) ? new Value<Vector2i>(default).Descriptor(parameter) :
            type == typeof(Vector3i?) ? new Value<Vector3i>(default).Descriptor(parameter) :
            type == typeof(Vector4i?) ? new Value<Vector4i>(default).Descriptor(parameter) :
            type == typeof(Color?) ? new Value<Color>(default).Descriptor(parameter) :
            type == typeof(Rect2?) ? new Value<Rect2>(default).Descriptor(parameter) : null;
    }
    private static void Supported<T>() where T : unmanaged
    {
        var type = typeof(T);
        if (type != typeof(bool) && type != typeof(int) && type != typeof(uint) && type != typeof(float) && type != typeof(Vector2) && type != typeof(Vector3) && type != typeof(Vector4) && type != typeof(Color) && type != typeof(Rect2) && type != typeof(Vector2i) && type != typeof(Vector3i) && type != typeof(Vector4i)) throw new ArgumentException("Instance values require supported scalar or vector types.");
    }
}
