using SDL3;

namespace Electron2D;

/// <summary>Owns a renderer-independent compute-device reference and offline physics pipelines.</summary>
internal sealed class GPUPhysicsDevice : IDisposable
{
    internal RenderHandle Handle { get; }
    internal nint Device => Handle.DangerousGetHandle();
    internal string Driver { get; }
    internal string DeviceName { get; }
    private bool _disposed;

    internal GPUPhysicsDevice()
    {
        var previous = DisplayServer.PrepareVideoEnvironment();
        var initialized = false;
        RenderHandle? device = null;
        try
        {
            if (!SDL.InitSubSystem(SDL.InitFlags.Video)) throw Failure("initialize GPU video support");
            initialized = true;
            if (previous is not null && SDL.GetCurrentVideoDriver() != "wayland")
            {
                DisplayServer.RestoreVideoEnvironment(previous); previous = null;
            }
            Handle = device = RenderingServer.Service?.RetainComputeDevice() ??
                new RenderHandle(SDL.CreateGPUDevice(ShaderCompiler.GetFormats(), false, null), SDL.DestroyGPUDevice);
            Driver = SDL.GetGPUDeviceDriver(Device) ?? "unknown";
            DeviceName = SDL.GetStringProperty(SDL.GetGPUDeviceProperties(Device), SDL.Props.GPUDeviceNameString, "unknown") ?? "unknown";
        }
        catch
        {
            device?.Dispose();
            if (initialized) SDL.QuitSubSystem(SDL.InitFlags.Video);
            DisplayServer.RestoreVideoEnvironment(previous);
            throw;
        }
    }

    internal RenderHandle CreatePipeline(string name)
    {
        using var source = typeof(GPUPhysicsDevice).Assembly.GetManifestResourceStream("Electron2D.PhysicsShaders." + name)
            ?? throw new InvalidOperationException("The physics shader is missing: " + name);
        using var bytes = new MemoryStream(); source.CopyTo(bytes);
        return new(ShaderCompiler.CreateComputePipeline(Device, bytes.ToArray()), handle => SDL.ReleaseGPUComputePipeline(Device, handle), Handle);
    }

    internal static InvalidOperationException Failure(string operation) => new($"GPU physics failed to {operation}: {SDL.GetError()}");

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; Handle.Dispose(); SDL.QuitSubSystem(SDL.InitFlags.Video);
    }
}
