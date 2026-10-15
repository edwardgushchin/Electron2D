using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Electron2D;
using SDL3;
using IOPath = System.IO.Path;

internal static class GPUPhysicsNativeMemoryTests
{
    private static readonly string[] Shaders = ["PhysicsResidentBodies", "PhysicsResidentFields", "PhysicsResidentJointEdits", "PhysicsResidentShapes", "PhysicsTree", "PhysicsResidentContacts", "PhysicsResidentSleep", "PhysicsResidentSolve", "PhysicsResidentUpdate", "PhysicsResidentGather", "PhysicsResidentColors", "PhysicsResidentColoredSolve", "PhysicsResidentPackSolver", "PhysicsResidentJoints"];

    internal static void Run(string mode, int cycles)
    {
        if (mode is not ("formats" or "reflection" or "device" or "pipelines" or "device-pipelines")) throw new ArgumentException("Unknown native-memory mode.", nameof(mode));
        if (cycles is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(cycles));
        RequireSoftwareICD();
        var code = Shaders.Select(name =>
        {
            using var source = typeof(GPUPhysicsDevice).Assembly.GetManifestResourceStream("Electron2D.PhysicsShaders." + name + ".comp.spv")!;
            using var bytes = new MemoryStream(); source.CopyTo(bytes); return bytes.ToArray();
        }).ToArray();
        _ = SDL.GetNumAllocations();
        GPUPhysicsDevice? retained = null;
        Ready("baseline", 0);
        try
        {
            if (mode == "pipelines") { retained = new(); RequireSoftware(retained); Ready("retained-device", 0); }
            for (var cycle = 1; cycle <= cycles; cycle++)
            {
                switch (mode)
                {
                    case "formats": if (ShaderCompiler.GetFormats() == 0) throw new InvalidOperationException("No formats."); break;
                    case "reflection": Reflect(code); break;
                    case "device": Device(false); break;
                    case "device-pipelines": Device(true); break;
                    case "pipelines": Pipelines(retained!); break;
                }
                Ready("cycle-" + cycle + "-retired", cycle);
            }
        }
        finally { retained?.Dispose(); }
        Ready("released", cycles);
    }

    private static void RequireSoftwareICD()
    {
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64) throw new PlatformNotSupportedException("Native-memory controls require Linux x64.");
        var path = Environment.GetEnvironmentVariable("VK_DRIVER_FILES");
        if (string.IsNullOrEmpty(path) || path != Environment.GetEnvironmentVariable("VK_ICD_FILENAMES") || Environment.GetEnvironmentVariable("SDL_GPU_DRIVER") != "vulkan" ||
            Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") != "offscreen" || Environment.GetEnvironmentVariable("VK_LOADER_LAYERS_DISABLE") != "~all~")
            throw new InvalidOperationException("Select one explicit software ICD with offscreen Vulkan and disabled loader layers before profiling.");
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        var library = json.RootElement.GetProperty("ICD").GetProperty("library_path").GetString()!;
        if (!IOPath.IsPathRooted(library)) library = IOPath.Combine(IOPath.GetDirectoryName(IOPath.GetFullPath(path))!, library);
        if (IOPath.GetFileName(library) != "libvulkan_lvp.so" || !File.Exists(library)) throw new InvalidOperationException("The controls require an existing lavapipe library.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Device(bool pipelines)
    {
        using var device = new GPUPhysicsDevice(); RequireSoftware(device);
        if (pipelines) Pipelines(device);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Pipelines(GPUPhysicsDevice device)
    {
        foreach (var shader in Shaders) { using var pipeline = device.CreatePipeline(shader + ".comp.spv"); }
        if (!SDL.WaitForGPUIdle(device.Device)) throw new InvalidOperationException(SDL.GetError());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void Reflect(byte[][] code)
    {
        foreach (var shader in code)
        {
            if (!ShaderCross.Init()) throw new InvalidOperationException(SDL.GetError());
            try
            {
                fixed (byte* pointer = shader)
                {
                    var value = ShaderCross.ReflectComputeSPIRV((nint)pointer, (nuint)shader.Length, 0);
                    if (value == 0) throw new InvalidOperationException(SDL.GetError());
                    try
                    {
                        var metadata = Marshal.PtrToStructure<ShaderCross.ComputePipelineMetadata>(value);
                        if (metadata.NumReadOnlyStorageBuffers > 8 || metadata.NumReadWriteStorageBuffers > 8) throw new InvalidDataException("Invalid metadata.");
                    }
                    finally { SDL.Free(value); }
                }
            }
            finally { ShaderCross.Quit(); }
        }
    }

    private static void RequireSoftware(GPUPhysicsDevice device)
    {
        if (device.Driver != "vulkan" || !device.DeviceName.Contains("llvmpipe", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The fixture requires the software device.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Ready(string phase, int cycle)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        Console.WriteLine(JsonSerializer.Serialize(new { Phase = phase, Cycle = cycle, ProcessMemory = ProcessMemory.Capture(), OutstandingSDLAllocations = SDL.GetNumAllocations() }));
        if (Console.ReadLine() != "next") throw new IOException("Collector disconnected.");
    }
}
