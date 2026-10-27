using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private bool _drawStorageWrites;
    private void ValidateGraphicsStorage(Binding[] layout)
    {
        var enabled = SDL.GetNumberProperty(SDL.GetGPUDeviceProperties(Device), GPUDeviceFactory.GraphicsStorageProperty, 0);
        foreach (var binding in layout)
        {
            if (!binding.Writable || binding.Stage == ShaderStage.Compute) continue;
            var feature = binding.Stage == ShaderStage.Vertex ? 1 : 2;
            if ((enabled & feature) == 0) throw new NotSupportedException($"Writable {binding.Stage} storage buffers require an enabled native graphics-storage feature.");
        }
    }
    private static bool WritesGraphicsStorage(Entry program)
    {
        foreach (var binding in program.Layout) if (binding.Writable && binding.Stage != ShaderStage.Compute) return true;
        return false;
    }
    private void MarkGraphicsStorage() => _drawStorageWrites |= WritesGraphicsStorage(PipelineProgram(_graphicsPipeline!));
    private void StorageBarrier()
    {
        if (NativeStorageBarrier(Device, 1, Commands()) != 1) throw new InvalidOperationException("The native graphics-storage barrier could not be recorded.");
    }
    private void EndDrawPass()
    {
        SDL.EndGPURenderPass(_drawPass); _drawPass = 0;
        var writes = _drawStorageWrites; _drawStorageWrites = false;
        try { if (writes) StorageBarrier(); }
        catch { _activeList = 0; _drawTarget = null; _graphicsPipeline = null; _drawVertices = null; _drawIndices = null; _sets.Clear(); ResetPush(); throw; }
    }
    private void PrepareGraphicsStorage()
    {
        var pending = _drawStorageWrites;
        if (!pending && !WritesGraphicsStorage(PipelineProgram(_graphicsPipeline!))) return;
        // Storage hazards require an external barrier; every active attachment is stored before restarting.
        EndDrawPass();
        try { if (!pending) StorageBarrier(); BeginDrawPass(false); }
        catch { _activeList = 0; _drawTarget = null; _graphicsPipeline = null; _sets.Clear(); ResetPush(); throw; }
    }
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_storage_barrier")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeStorageBarrier(nint device, uint abi, nint commands);
}
