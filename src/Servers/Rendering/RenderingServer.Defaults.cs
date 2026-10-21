namespace Electron2D;

public sealed partial class RenderingServer
{
    private ShaderUniform DefaultUniform(RID shader, string name)
    {
        EnsureOwner(); ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var program = RenderingProgramRegistry<Shader>.Resolve(shader).GetProgram();
        if (program.Uniforms.TryGetValue(name, out var uniform) || program.InstanceUniforms.TryGetValue(name, out uniform)) return uniform;
        throw new ArgumentException("Shader has no numeric user uniform with this name.", nameof(name));
    }
    private ShaderUniform DefaultUniform<T>(RID shader, string name, bool array) where T : unmanaged
    {
        var uniform = DefaultUniform(shader, name);
        if (!uniform.Accepts<T>() || uniform.MatrixWidth > 2 || (uniform.ArrayLength != 0) != array)
            throw new ArgumentException("Shader default has a different logical type or scalar/array shape.", nameof(name));
        return uniform;
    }
    internal T ShaderGetParameterDefaultCore<T>(RID shader, string name) where T : unmanaged => DefaultUniform<T>(shader, name, false).ReadDefault<T>();
    internal T[] ShaderGetParameterDefaultArrayCore<T>(RID shader, string name) where T : unmanaged
    {
        var uniform = DefaultUniform<T>(shader, name, true); var result = new T[uniform.Count];
        for (var i = 0; i < result.Length; i++) result[i] = uniform.ReadDefault<T>(i);
        return result;
    }
    internal void ShaderGetParameterDefaultCore<T>(RID shader, string name, Span<T> destination) where T : unmanaged
    {
        var uniform = DefaultUniform<T>(shader, name, true);
        if (destination.Length != uniform.Count) throw new ArgumentException("Destination must match the reflected default array length.", nameof(destination));
        for (var i = 0; i < destination.Length; i++) destination[i] = uniform.ReadDefault<T>(i);
    }
    private ShaderUniform DefaultMatrixUniform(RID shader, string name)
    {
        var uniform = DefaultUniform(shader, name);
        if (uniform.MatrixWidth is not (3 or 4)) throw new ArgumentException("Shader default requires a numeric mat3/mat4 declaration.", nameof(name));
        return uniform;
    }
    internal float[] ShaderGetMatrixParameterDefaultCore(RID shader, string name)
    {
        var uniform = DefaultMatrixUniform(shader, name); var result = new float[uniform.DefaultWordWidth * uniform.Count];
        CopyMatrixDefault(uniform, result); return result;
    }
    internal void ShaderGetMatrixParameterDefaultCore(RID shader, string name, Span<float> destination)
    {
        var uniform = DefaultMatrixUniform(shader, name);
        if (destination.Length != uniform.DefaultWordWidth * uniform.Count) throw new ArgumentException("Destination must match all default matrix components.", nameof(destination));
        CopyMatrixDefault(uniform, destination);
    }
    private static void CopyMatrixDefault(ShaderUniform uniform, Span<float> destination)
    {
        Span<byte> storage = stackalloc byte[uniform.ElementSize];
        for (var i = 0; i < uniform.Count; i++)
        {
            storage.Clear(); uniform.WriteDefaultElement(storage, i);
            uniform.ReadMatrix(storage, destination.Slice(i * uniform.DefaultWordWidth, uniform.DefaultWordWidth));
        }
    }
}
