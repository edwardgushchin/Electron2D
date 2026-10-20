using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private void PrepareInputShadow(Entry entry)
    {
        var info = entry.Texture!;
        if (!NeedsGraphicsSnapshot(entry) && ((info.Usage & TextureUsageBits.InputAttachment) == 0 || info.Type != TextureType.TextureArray && (info.Usage & TextureUsageBits.Sampling) == 0)) return;
        entry.InputShadow = new(SDL.CreateGPUTexture(Device, new()
        {
            Type = SDL.GPUTextureType.TextureType2D,
            Format = NativeFormat(info.Format),
            Width = info.Width,
            Height = info.Height,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = ExactSamples(info.Format, info.Samples),
            Usage = SDL.GPUTextureUsageFlags.GraphicsStorageRead
        }), h => SDL.ReleaseGPUTexture(Device, h), _context.Handle);
    }
    private void CaptureInputAttachments(FramebufferPassLayout layout)
    {
        foreach (var index in layout.Inputs)
        {
            if (index < 0) continue;
            var entry = Get(_drawTarget!.Textures[index], Kind.Texture);
            if (entry.InputShadow is null) continue;
            var copy = SDL.BeginGPUCopyPass(Commands()); if (copy == 0) throw GPUPhysicsDevice.Failure("snapshot framebuffer input");
            SDL.CopyGPUTextureToTexture(copy, new() { Texture = entry.NativeHandle, Layer = (uint)_drawView }, new() { Texture = entry.InputShadow.DangerousGetHandle() }, entry.Texture!.Width, entry.Texture.Height, 1, false);
            SDL.EndGPUCopyPass(copy);
        }
    }
    private static void ValidateInputLayout(Entry program, FramebufferPassLayout layout)
    {
        foreach (var binding in program.Layout)
        {
            if (binding.Type != UniformType.InputAttachment) continue;
            if (binding.Attachment < 0 || binding.Attachment >= layout.Inputs.Length || layout.Inputs[binding.Attachment] < 0) throw new ArgumentException("A shader input attachment has no matching framebuffer slot.");
            var index = layout.Inputs[binding.Attachment];
            if (PixelType(layout.Formats[index]) != binding.PixelType || (layout.AttachmentSamples[index] != TextureSamples.Samples1) != binding.Multisample) throw new ArgumentException("The input attachment scalar type or multisample role disagrees with the shader.");
        }
    }
    private nint InputAttachment(Binding descriptor, Entry resource)
    {
        var layout = DrawPassLayout;
        if (descriptor.Attachment < 0 || descriptor.Attachment >= layout.Inputs.Length || layout.Inputs[descriptor.Attachment] < 0 ||
            Get(_drawTarget!.Textures[layout.Inputs[descriptor.Attachment]], Kind.Texture).NativeHandle != resource.NativeHandle)
            throw new InvalidOperationException("The bound input must be the attachment selected by the active pass.");
        return resource.InputShadow?.DangerousGetHandle() ?? resource.NativeHandle;
    }
}
