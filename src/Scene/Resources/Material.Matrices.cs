namespace Electron2D;

public sealed partial class ShaderMaterial
{
    /// <summary>Replaces a numeric mat3/mat4 uniform or its entire fixed array.</summary>
    /// <param name="name">The exact, case-sensitive material member name.</param>
    /// <param name="values">Copied column-major elements: nine for mat3, sixteen for mat4, concatenated for arrays.</param>
    /// <remarks>Every component must be finite. Shape and all values are validated before mutation; successful writes emit Changed.
    /// Values start at identity and migrate across compatible shader layouts. Prepared updates allocate no managed memory.</remarks>
    /// <exception cref="ArgumentException">The name, matrix role, component count or a value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public void SetShaderMatrixParameter(string name, ReadOnlySpan<float> values)
    {
        lock (_gate)
        {
            var state = RequireState(); var uniform = FindMatrix(state, name);
            var size = uniform.MatrixWidth * uniform.MatrixWidth;
            if (values.Length != size * uniform.Count) throw new ArgumentException("Component count must match the entire reflected matrix parameter.", nameof(values));
            foreach (var value in values) if (!float.IsFinite(value)) throw new ArgumentException("Matrix components must be finite.", nameof(values));
            for (var i = 0; i < uniform.Count; i++)
                uniform.WriteMatrix(state.Buffers[uniform.Buffer].AsSpan(uniform.Offset + i * uniform.Stride, uniform.ElementSize), values.Slice(i * size, size));
        }
        EmitChanged();
    }

    /// <summary>Returns an independent column-major copy of a numeric mat3/mat4 uniform or fixed array.</summary>
    /// <param name="name">The exact, case-sensitive material member name.</param>
    /// <returns>Nine/sixteen components per matrix, concatenated for arrays; initially identity for each element.</returns>
    /// <exception cref="ArgumentException">The name does not identify a numeric mat3/mat4 material parameter.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public float[] GetShaderMatrixParameter(string name)
    {
        lock (_gate)
        {
            var uniform = FindMatrix(RequireState(), name);
            var values = new float[uniform.Count * uniform.MatrixWidth * uniform.MatrixWidth];
            CopyShaderMatrixParameter(name, values);
            return values;
        }
    }

    /// <summary>Copies a numeric mat3/mat4 uniform or fixed array into an exact-size destination without allocation after preparation.</summary>
    /// <param name="name">The exact, case-sensitive material member name.</param>
    /// <param name="destination">Receives column-major values: nine/sixteen per matrix, concatenated for arrays.</param>
    /// <exception cref="ArgumentException">The name, matrix role or destination length is invalid; the destination is unchanged.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public void CopyShaderMatrixParameter(string name, Span<float> destination)
    {
        lock (_gate)
        {
            var state = RequireState(); var uniform = FindMatrix(state, name);
            var size = uniform.MatrixWidth * uniform.MatrixWidth;
            if (destination.Length != size * uniform.Count) throw new ArgumentException("Destination length must match the entire reflected matrix parameter.", nameof(destination));
            for (var i = 0; i < uniform.Count; i++)
                uniform.ReadMatrix(state.Buffers[uniform.Buffer].AsSpan(uniform.Offset + i * uniform.Stride, uniform.ElementSize), destination.Slice(i * size, size));
        }
    }

    private static ShaderUniform FindMatrix(MaterialState state, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!state.Program.Uniforms.TryGetValue(name, out var uniform) || uniform.MatrixWidth < 3)
            throw new ArgumentException($"Uniform '{name}' is not a numeric mat3/mat4 parameter.", nameof(name));
        return uniform;
    }
}
