namespace Electron2D;

public sealed partial class ShaderMaterial
{
    private bool _validatingTextures;
    private Resource?[]? _textureScratch;

    /// <summary>Replaces every borrowed ordinary-image override in a fixed descriptor array.</summary>
    /// <param name="name">The exact reflected sampled-image array name.</param>
    /// <param name="textures">Exactly the reflected element count. Null elements select their respective shader defaults.</param>
    /// <remarks>Validates the entire input before committing and emits Changed once. Copies references without taking ownership.
    /// Prepared updates allocate no managed memory. Sampling type, ordinary/layered role and descriptor array shape must match.</remarks>
    /// <exception cref="ArgumentException">The name, image role, scalar type, descriptor shape or array length differs.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned, validation replaces the program or reenters an array update.</exception>
    /// <exception cref="ObjectDisposedException">The material, shader or supplied image is disposed.</exception>
    public void SetShaderTextureParameterArray(string name, ReadOnlySpan<Texture?> textures) => SetTextureArray(name, textures, false);

    /// <summary>Replaces every borrowed layered-image override in a fixed descriptor array.</summary>
    /// <param name="name">The exact reflected sampled-image array name.</param>
    /// <param name="textures">Exactly the reflected element count. Null elements select their respective shader defaults.</param>
    /// <remarks>Validates all elements before committing and emits Changed once. References are copied and remain caller-owned.</remarks>
    /// <exception cref="ArgumentException">The name, image role, scalar type, descriptor shape or array length differs.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned, validation replaces the program or reenters an array update.</exception>
    /// <exception cref="ObjectDisposedException">The material, shader or supplied image is disposed.</exception>
    public void SetShaderLayeredParameterArray(string name, ReadOnlySpan<TextureLayered?> textures) => SetTextureArray(name, textures, true);

    /// <summary>Returns a copied fixed array of borrowed ordinary-image overrides.</summary>
    /// <param name="name">The exact reflected sampled-image array name.</param>
    /// <returns>One override per element; null selects that element's shader default.</returns>
    /// <exception cref="ArgumentException">The name, image role or descriptor shape differs.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public Texture?[] GetShaderTextureParameterArray(string name) => GetTextureArray<Texture>(name, false);

    /// <summary>Returns a copied fixed array of borrowed layered-image overrides.</summary>
    /// <param name="name">The exact reflected sampled-image array name.</param>
    /// <returns>One override per element; null selects that element's shader default.</returns>
    /// <exception cref="ArgumentException">The name, image role or descriptor shape differs.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public TextureLayered?[] GetShaderLayeredParameterArray(string name) => GetTextureArray<TextureLayered>(name, true);

    /// <summary>Copies ordinary-image overrides into exact-length caller storage without prepared allocation.</summary>
    /// <param name="name">The exact reflected sampled-image array name.</param>
    /// <param name="destination">Storage with exactly the reflected array length; null elements select shader defaults.</param>
    /// <exception cref="ArgumentException">The name, image role, descriptor shape or destination length differs.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public void CopyShaderTextureParameterArray(string name, Span<Texture?> destination) => CopyTextureArray(name, destination, false);

    /// <summary>Copies layered-image overrides into exact-length caller storage without prepared allocation.</summary>
    /// <param name="name">The exact reflected sampled-image array name.</param>
    /// <param name="destination">Storage with exactly the reflected array length; null elements select shader defaults.</param>
    /// <exception cref="ArgumentException">The name, image role, descriptor shape or destination length differs.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public void CopyShaderLayeredParameterArray(string name, Span<TextureLayered?> destination) => CopyTextureArray(name, destination, true);

    private static int TextureArraySlot(MaterialState state, string name, int length = -1, bool? layered = null)
    {
        var slot = state.Program.FindTexture(name); var descriptor = state.Program.Textures[slot]; descriptor.RequireBindingArray(true);
        if (layered.HasValue) descriptor.RequireShape(layered.Value);
        if (length >= 0 && length != descriptor.Count) throw new ArgumentException($"Texture parameter '{name}' requires {descriptor.Count} elements.", nameof(length));
        return slot;
    }
    private void SetTextureArray<T>(string name, ReadOnlySpan<T?> textures, bool layered) where T : Resource
    {
        lock (_gate)
        {
            var state = RequireState(); var slot = TextureArraySlot(state, name, textures.Length, layered);
            if (_validatingTextures) throw new InvalidOperationException("Texture validation cannot reenter an array update.");
            var scratch = _textureScratch ??= new Resource?[16]; _validatingTextures = true;
            try
            {
                for (var i = 0; i < textures.Length; i++) scratch[i] = textures[i];
                foreach (var texture in scratch.AsSpan(0, textures.Length)) state.Program.Textures[slot].Validate(texture);
                if (!ReferenceEquals(state, RequireState())) throw new InvalidOperationException("Texture validation replaced the material program.");
                CommitTextures(state, slot, scratch.AsSpan(0, textures.Length));
            }
            finally { Array.Clear(scratch); _validatingTextures = false; }
        }
        EmitChanged();
    }
    private static void CommitTextures(MaterialState state, int slot, ReadOnlySpan<Resource?> values)
    {
        foreach (var texture in values) if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(values));
        values.CopyTo(state.Textures.AsSpan(slot, values.Length));
    }
    private T?[] GetTextureArray<T>(string name, bool layered) where T : Resource
    {
        lock (_gate)
        {
            var state = RequireState(); var slot = TextureArraySlot(state, name, layered: layered); var values = new T?[state.Program.Textures[slot].Count];
            for (var i = 0; i < values.Length; i++) values[i] = (T?)state.Textures[slot + i]; return values;
        }
    }
    private void CopyTextureArray<T>(string name, Span<T?> destination, bool layered) where T : Resource
    {
        lock (_gate)
        {
            var state = RequireState(); var slot = TextureArraySlot(state, name, destination.Length, layered);
            for (var i = 0; i < destination.Length; i++) destination[i] = (T?)state.Textures[slot + i];
        }
    }
    internal void SetSampledArray(string name, ReadOnlySpan<RID> textures)
    {
        lock (_gate)
        {
            var state = RequireState(); var slot = TextureArraySlot(state, name, textures.Length);
            if (_validatingTextures) throw new InvalidOperationException("Texture validation cannot reenter an array update.");
            var scratch = _textureScratch ??= new Resource?[16]; _validatingTextures = true;
            try
            {
                for (var i = 0; i < textures.Length; i++)
                {
                    var texture = textures[i].IsValid() ? RenderingTextureRegistry.ResolveResource(textures[i]) : null;
                    scratch[i] = texture;
                }
                foreach (var texture in scratch.AsSpan(0, textures.Length)) state.Program.Textures[slot].Validate(texture);
                if (!ReferenceEquals(state, RequireState())) throw new InvalidOperationException("Texture validation replaced the material program.");
                CommitTextures(state, slot, scratch.AsSpan(0, textures.Length));
            }
            finally { Array.Clear(scratch); _validatingTextures = false; }
        }
        EmitChanged();
    }
    internal RID[] GetSampledArray(string name)
    {
        lock (_gate)
        {
            var state = RequireState(); var slot = TextureArraySlot(state, name); var values = new RID[state.Program.Textures[slot].Count];
            CopySampledArray(name, values); return values;
        }
    }
    internal void CopySampledArray(string name, Span<RID> destination)
    {
        lock (_gate)
        {
            var state = RequireState(); var slot = TextureArraySlot(state, name, destination.Length);
            Span<RID> values = stackalloc RID[16];
            for (var i = 0; i < destination.Length; i++) values[i] = state.Textures[slot + i]?.GetRID() ?? default;
            values[..destination.Length].CopyTo(destination);
        }
    }
}
