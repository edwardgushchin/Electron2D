using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private int _graphicsSnapshotCount;
    private readonly List<Entry> _graphicsSnapshots = new(8);
    private static bool NeedsGraphicsSnapshot(Entry entry) => entry.Kind == Kind.Texture && entry.Texture is { Type: TextureType.Texture } info && (info.Usage & (TextureUsageBits.Sampling | TextureUsageBits.Storage)) == (TextureUsageBits.Sampling | TextureUsageBits.Storage);
    private bool CaptureGraphicsImages()
    {
        if (_graphicsSnapshotCount == 0) return false;
        _graphicsSnapshots.Clear();
        try
        {
            foreach (var binding in PipelineProgram(_graphicsPipeline!).Layout)
            {
                if (binding.Type != UniformType.Image) continue;
                if (!_sets.TryGetValue(binding.Set, out var set) || set.Shader != _graphicsPipeline!.Shader) throw new InvalidOperationException("Bind every graphics resource set first.");
                var ids = set.Bindings[binding.Slot]; ValidateBinding(binding, ids); var entry = Get(ids[0]);
                if (ActiveAttachment(entry)) throw new InvalidOperationException("An active output cannot be read as storage.");
                if (NeedsGraphicsSnapshot(entry) && !_graphicsSnapshots.Contains(entry)) _graphicsSnapshots.Add(entry);
            }
            if (_graphicsSnapshots.Count == 0) return false;
            SDL.EndGPURenderPass(_drawPass); _drawPass = 0;
            try
            {
                foreach (var entry in _graphicsSnapshots)
                {
                    var copy = SDL.BeginGPUCopyPass(Commands()); if (copy == 0) throw GPUPhysicsDevice.Failure("snapshot graphics image");
                    SDL.CopyGPUTextureToTexture(copy, new() { Texture = entry.NativeHandle }, new() { Texture = entry.InputShadow!.DangerousGetHandle() }, entry.Texture!.Width, entry.Texture.Height, 1, false);
                    SDL.EndGPUCopyPass(copy);
                }
                BeginDrawPass(false); return true;
            }
            catch { _activeList = 0; _drawTarget = null; _graphicsPipeline = null; _sets.Clear(); ResetPush(); throw; }
        }
        finally { _graphicsSnapshots.Clear(); }
    }
}
