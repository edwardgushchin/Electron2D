using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private Dictionary<(Entry Source, Entry Destination), RenderHandle[]>? _depthResolves;
    private bool DepthResolveSupported(DataFormat format)
    {
        if (!DepthFormat(format)) return false;
        var required = format == DataFormat.S8UINT ? 2 : StencilFormat(format) ? 3 : 1;
        return (SDL.GetNumberProperty(SDL.GetGPUDeviceProperties(Device), GPUDeviceFactory.DepthResolveProperty, 0) & required) == required;
    }
    private void RequireDepthResolve(DataFormat format)
    { if (!DepthResolveSupported(format)) throw new NotSupportedException("Depth/stencil resolve requires enabled native sample-zero modes and the Vulkan 1.2 aspect adapter."); }
    private RenderHandle[] PrepareDepthResolve(Entry source, Entry destination)
    {
        RequireDepthResolve(source.Texture!.Format);
        var key = (source, destination); var cache = _depthResolves ??= [];
        if (!cache.TryGetValue(key, out var targets))
        {
            targets = new RenderHandle[Math.Min(source.Texture.Layers, destination.Texture!.Layers)];
            try
            {
                for (uint layer = 0; layer < targets.Length; layer++)
                {
                    var from = GPUTextureViews.Location(Device, new() { Texture = source.NativeHandle, Layer = layer });
                    var to = GPUTextureViews.Location(Device, new() { Texture = destination.NativeHandle, Layer = layer });
                    var native = NativeDepthResolveCreate(Device, 1, in from, in to, source.Texture.Width, source.Texture.Height);
                    if (native == 0) throw GPUPhysicsDevice.Failure("prepare depth/stencil resolve attachments");
                    var device = Device;
                    targets[layer] = new(native, h => NativeDepthResolveRelease(device, 1, h), _context.Handle);
                }
                cache.Add(key, targets);
            }
            catch { foreach (var target in targets) target?.Dispose(); throw; }
        }
        return targets;
    }
    private void ResolveDepth(Entry source, Entry destination, int layer = -1)
    {
        var targets = PrepareDepthResolve(source, destination);
        var pass = SDL.BeginGPUCopyPass(Commands());
        if (pass == 0) throw GPUPhysicsDevice.Failure("begin depth/stencil resolve commands");
        try
        {
            var first = layer < 0 ? 0 : layer; var end = layer < 0 ? targets.Length : layer + 1;
            for (var i = first; i < end; i++)
                if (NativeDepthResolveRecord(Device, 1, _command, targets[i].DangerousGetHandle()) != 1) throw GPUPhysicsDevice.Failure("record depth/stencil resolve");
        }
        finally { SDL.EndGPUCopyPass(pass); }
    }
    private void ReleaseDepthResolves(Entry? entry = null)
    {
        if (_depthResolves is null) return;
        foreach (var pair in _depthResolves)
            if (entry is null || pair.Key.Source == entry || pair.Key.Destination == entry)
            {
                foreach (var target in pair.Value) target.Dispose();
                _depthResolves.Remove(pair.Key);
            }
    }
    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_depth_resolve_create")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint NativeDepthResolveCreate(nint device, uint abi, in SDL.GPUTextureLocation source, in SDL.GPUTextureLocation destination, uint width, uint height);
    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_depth_resolve_record")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeDepthResolveRecord(nint device, uint abi, nint commands, nint target);
    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_depth_resolve_release")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void NativeDepthResolveRelease(nint device, uint abi, nint target);
}
