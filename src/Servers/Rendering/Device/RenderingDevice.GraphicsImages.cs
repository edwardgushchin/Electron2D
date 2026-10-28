using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private int? _imageSupport;
    private readonly List<Entry> _generalImages = new(16);
    private bool CaptureNativeImages()
    {
        _graphicsSnapshots.Clear();
        try
        {
            foreach (var binding in PipelineProgram(_graphicsPipeline!).Layout)
            {
                if (binding.Type != UniformType.Image) continue;
                if (!_sets.TryGetValue(binding.Set, out var set) || set.Shader != _graphicsPipeline!.Shader) throw new InvalidOperationException("Bind every graphics image set first.");
                var ids = set.Bindings[binding.Slot]; ValidateBinding(binding, ids); var entry = Get(ids[ResourceID(binding)]);
                if (ActiveAttachment(entry)) throw new InvalidOperationException("An active output cannot be bound as graphics storage.");
                var exists = false; foreach (var candidate in _graphicsSnapshots) if (candidate.NativeHandle == entry.NativeHandle) { exists = true; break; }
                if (!exists) _graphicsSnapshots.Add(entry);
            }
            EndDrawPass();
            try
            {
                CaptureInputAttachments(DrawPassLayout);
                _generalImages.AddRange(_graphicsSnapshots);
                try { TransitionGraphicsImages(true); }
                catch { _generalImages.Clear(); throw; }
                BeginDrawPass(false, false); return true;
            }
            catch
            {
                try { RestoreGraphicsImages(); }
                finally { _activeList = 0; _drawTarget = null; _graphicsPipeline = null; _sets.Clear(); ResetPush(); }
                throw;
            }
        }
        finally { _graphicsSnapshots.Clear(); }
    }
    private void TransitionGraphicsImages(bool general)
    {
        var textures = stackalloc nint[16];
        for (var i = 0; i < _generalImages.Count; i++) textures[i] = _generalImages[i].NativeHandle;
        if (NativeImageTransition(Device, 1, _command, textures, (uint)_generalImages.Count, general ? 1u : 0u) != 1) throw new InvalidOperationException("The native graphics image layout transition failed.");
    }
    private void RestoreGraphicsImages()
    {
        if (_generalImages.Count == 0) return;
        try { TransitionGraphicsImages(false); }
        finally { _generalImages.Clear(); }
    }
    private void PrepareNativeImages(Entry program)
    {
        if (program.GraphicsImages == 0) return;
        var textures = stackalloc nint[16];
        for (var i = 0; i < _generalImages.Count; i++) textures[i] = _generalImages[i].NativeHandle;
        if (NativePrepareImages(Device, 1, _command, program.GraphicsImages, textures, (uint)_generalImages.Count) != 1) throw new InvalidOperationException("The native graphics image descriptors could not be prepared.");
    }
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_image_support")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeImageSupport(nint device, uint abi);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_images_pipeline")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint NativeImagesPipeline(nint device, uint abi, in SDL.GPUGraphicsPipelineCreateInfo info, float lineWidth, uint discard, in NativeMultisampleState samples, in NativeDepthStencilState depth, uint images);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_image_transition")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeImageTransition(nint device, uint abi, nint commands, nint* textures, uint count, uint general);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_bind_image")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeBindImage(nint device, uint abi, nint commands, nint texture, uint slot, uint fragment, uint storage);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_prepare_images")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativePrepareImages(nint device, uint abi, nint commands, uint masks, nint* textures, uint count);
}
