namespace Electron2D;

public sealed partial class ShaderMaterial
{
    private void ResetParameter(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        lock (_gate)
        {
            var state = RequireState();
            if (state.Program.Uniforms.TryGetValue(name, out var uniform))
            {
                uniform.InitializeDefault(state.Buffers[uniform.Buffer].AsSpan(uniform.Offset, uniform.StorageSize));
                state.UniformOverrides.Remove(name);
            }
            else
            {
                if (_validatingTextures) throw new InvalidOperationException("Texture validation cannot reenter a parameter reset.");
                var slot = state.Program.FindTexture(name);
                Array.Clear(state.Textures, slot, state.Program.Textures[slot].Count);
            }
        }
        EmitChanged();
    }
}
