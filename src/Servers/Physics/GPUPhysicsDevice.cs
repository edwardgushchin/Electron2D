using SDL3;

namespace Electron2D;

/// <summary>Owns a renderer-independent compute-device reference and offline physics pipelines.</summary>
internal sealed class GPUPhysicsDevice : IDisposable
{
    internal RenderHandle Handle { get; }
    internal nint Device => Handle.DangerousGetHandle();
    internal string Driver { get; }
    internal string DeviceName { get; }
    internal bool SupportsSizedPoints { get; }
    /// <summary>Observes cold pipeline creation for scoped executable ownership checks.</summary>
    internal static Action<string, int, int>? PipelineCreationForTesting { get; set; }
    private readonly int _ownerThreadID = Environment.CurrentManagedThreadId;
    private bool _disposed;

    internal GPUPhysicsDevice()
    {
        var previous = DisplayServer.PrepareVideoEnvironment();
        var initialized = false;
        RenderHandle? device = null;
        try
        {
            if (!DisplayServer.InitializeVideo()) throw Failure("initialize GPU video support");
            initialized = true;
            if (previous is not null && SDL.GetCurrentVideoDriver() != "wayland")
            {
                DisplayServer.RestoreVideoEnvironment(previous); previous = null;
            }
            var renderer = RenderingServer.Service;
            var shared = renderer?.RetainComputeDevice();
            if (shared is not null) { Handle = device = shared; SupportsSizedPoints = renderer!.SupportsSizedPoints; }
            else
            {
                var created = GPUDeviceFactory.Create(false); SupportsSizedPoints = created.SizedPoints;
                Handle = device = new RenderHandle(created.Device, SDL.DestroyGPUDevice);
            }
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
        PipelineCreationForTesting?.Invoke(name, _ownerThreadID, Environment.CurrentManagedThreadId);
        if (name is "PhysicsResidentMotionQueries0.comp.spv" or "PhysicsResidentMotionQueries1.comp.spv" or "PhysicsResidentMotionQueries2.comp.spv" or "PhysicsResidentMotionQueries3.comp.spv" ||
            (Driver is "metal" or "direct3d12") && name is "PhysicsResidentCCD.comp.spv" or
            "PhysicsResidentShapeQueries0.comp.spv" or "PhysicsResidentShapeQueries1.comp.spv" or
            "PhysicsResidentShapeQueries2.comp.spv" or "PhysicsResidentShapeQueries3.comp.spv")
            name = name.Replace(".spv", ".portable.spv", StringComparison.Ordinal);
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
