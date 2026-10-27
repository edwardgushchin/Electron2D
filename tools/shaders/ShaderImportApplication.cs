using IOPath = System.IO.Path;
using System.Diagnostics;
using Electron2D;
using SDL3;
using System.Runtime.InteropServices;

namespace Electron2D.Tools;

/// <summary>Runs the shared offline SPIR-V importer from a compiled application host.</summary>
/// <remarks>A host may register its compiled resource factories before invoking this tool-layer entry point.
/// It uses the same validation, atomic output, native compiler assets and exit codes as the command-line executable.
/// This type belongs to the import tool assembly; game rendering requires only previously compiled resources.</remarks>
public static class ShaderImportApplication
{
    private static int _running;
    /// <summary>Imports one module or canvas graph using a copied command-line argument array.</summary>
    /// <param name="arguments">The ordinary shader-import command-line arguments.</param>
    /// <returns>Zero on success, one on import failure, or two for an invalid command shape.</returns>
    /// <remarks>Register custom resource types in this process before calling. The host supplies the pinned native toolchain.
    /// Console output retains the command-line diagnostic format. Concurrent and reentrant calls reject; invoke one import at a time in a dedicated host process.</remarks>
    /// <exception cref="ArgumentNullException">The argument array is null.</exception>
    /// <exception cref="ArgumentException">An argument element or input path is invalid.</exception>
    /// <exception cref="InvalidOperationException">Another import is running in this process.</exception>
    public static async Task<int> RunAsync(string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var args = (string[])arguments.Clone();
        if (args.Any(a => a is null)) throw new ArgumentException("Import arguments cannot contain null elements.", nameof(arguments));
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0) throw new InvalidOperationException("Only one offline import may run in a process at a time.");
        try
        {
            if (args.Length == 4 && args[3] == "--visual-shader") return await ImportVisualGraph(args);
            return await ImportModule(args);
        }
        finally { Volatile.Write(ref _running, 0); }
    }

    static async Task<int> ImportModule(string[] args, VisualShader? graph = null, IReadOnlyList<VisualShaderImage>? images = null, IReadOnlyList<VisualShaderBlockName>? blocks = null, CanvasStageSource? light = null, CanvasStageSource? surface = null)
    {
        var particleImport = args.Length is 4 or 5 && args[3] == "--particles";
        var blitImport = args.Length is 4 or 5 && args[3] == "--texture-blit";
        var deviceImport = args.Length is 4 or 5 && args[3] == "--device";
        if ((!deviceImport && !blitImport && !particleImport && args.Length != 3) || (blitImport && args[1] != "fragment") || (particleImport && args[1] != "compute") || args[1] is not ("vertex" or "fragment" or "compute") || args[1] == "compute" && !deviceImport && !particleImport)
        {
            Console.Error.WriteLine("Usage: ShaderImport <source.hlsl|source.glsl|source.spv> <vertex|fragment|compute> <output.spv> [--device|--texture-blit|--particles [include-root]]\n       ShaderImport <graph.e2dres> <graph|canvas|particles|texture-blit> <output.e2dres> --visual-shader");
            return 2;
        }
        var input = IOPath.GetFullPath(args[0]);
        var output = IOPath.GetFullPath(args[2]);
        var stage = args[1] == "compute" ? RenderingDevice.ShaderStage.Compute : args[1] == "fragment" ? RenderingDevice.ShaderStage.Fragment : RenderingDevice.ShaderStage.Vertex;
        var includeRoot = args.Length == 5 ? IOPath.GetFullPath(args[4]) : IOPath.GetDirectoryName(input);
        var temporary = IOPath.Combine(IOPath.GetTempPath(), "electron2d-shader-" + Guid.NewGuid().ToString("N") + ".spv");
        var debugTemporary = temporary + ".debug";
        try
        {
            Dictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)>? sourceTypes = null;
            string? hlslSource = null;
            await RequireVersion("spirv-val", "SPIRV-Tools v2026.4");
            var extension = IOPath.GetExtension(input).ToLowerInvariant();
            using var builtIns = extension is ".hlsl" or ".glsl" ? new BuiltInIncludeFiles() : null;
            switch (extension)
            {
                case ".hlsl":
                    var source = File.ReadAllText(input);
                    // Keep original source locations in diagnostics reported by the native compiler.
                    var escaped = input.Replace("\\", "/").Replace("\"", "\\\"");
                    hlslSource = "#line 1 \"" + escaped + "\"\n" + source;
                    var code = CompileHLSL(hlslSource,
                        stage, includeRoot, builtIns!.DirectoryPath, deviceImport, particleImport, out sourceTypes);
                    File.WriteAllBytes(temporary, code);
                    break;
                case ".glsl":
                    await RequireVersion("glslangValidator", "Glslang Version: 11:16.6.0");
                    Console.Write(await Execute("glslangValidator", "-V", "--target-env", "vulkan1.0", "-S",
                        args[1] == "compute" ? "comp" : args[1] == "fragment" ? "frag" : "vert", "-I" + includeRoot, "-I" + builtIns!.DirectoryPath, "-e", "main", "-o", temporary, input));
                    if (deviceImport && HasByteTypes(File.ReadAllBytes(temporary)))
                    {
                        // Native byte storage needs modern storage classes to retain independent buffer/uniform features.
                        Console.Write(await Execute("glslangValidator", "-V", "--target-env", "vulkan1.2", "-S",
                            args[1] == "compute" ? "comp" : args[1] == "fragment" ? "frag" : "vert", "-I" + includeRoot, "-I" + builtIns!.DirectoryPath, "-e", "main", "-o", temporary, input));
                    }
                    if (!deviceImport)
                    {
                        Console.Write(await Execute("glslangValidator", "-V", "--target-env", "vulkan1.0", "-S",
                            args[1] == "compute" ? "comp" : args[1] == "fragment" ? "frag" : "vert", "-I" + includeRoot, "-I" + builtIns!.DirectoryPath, "-e", "main", "-gV", "-o", debugTemporary, input));
                        sourceTypes = ShaderSourceTypes.GLSL(ShaderSourceTypes.CanonicalizeGraphBlocks(File.ReadAllBytes(debugTemporary), blocks ?? [], args[1] == "fragment", particleImport ? Shader.Mode.Particles : blitImport ? Shader.Mode.TextureBlit : Shader.Mode.CanvasItem), args[1] == "fragment", particleImport);
                    }
                    break;
                case ".spv":
                    File.Copy(input, temporary);
                    break;
                default: throw new ArgumentException("The supported source languages are HLSL and GLSL; external compilers must supply SPIR-V.");
            }
            var result = ShaderSourceTypes.CanonicalizeGraphBlocks(File.ReadAllBytes(temporary), blocks ?? [], args[1] == "fragment", particleImport ? Shader.Mode.Particles : blitImport ? Shader.Mode.TextureBlit : Shader.Mode.CanvasItem);
            if (sourceTypes is not null)
            {
                result = ShaderSourceTypes.Annotate(result, args[1] == "fragment", sourceTypes, particleImport ? Shader.Mode.Particles : blitImport ? Shader.Mode.TextureBlit : Shader.Mode.CanvasItem);
                File.WriteAllBytes(temporary, result);
            }
            if (!deviceImport)
            {
                if (hlslSource is not null)
                {
                    var directory = Directory.GetCurrentDirectory();
                    try
                    {
                        Directory.SetCurrentDirectory(builtIns!.DirectoryPath);
                        result = ShaderSourceTypes.AddHLSLDefaults(result, hlslSource, stage, includeRoot, particleImport ? Shader.Mode.Particles : blitImport ? Shader.Mode.TextureBlit : Shader.Mode.CanvasItem);
                    }
                    finally { Directory.SetCurrentDirectory(directory); }
                }
                result = ShaderSourceTypes.AddConfiguredDefaults(result, input, args[1] == "fragment", particleImport ? Shader.Mode.Particles : blitImport ? Shader.Mode.TextureBlit : Shader.Mode.CanvasItem);
                if (graph is not null)
                {
                    result = ShaderSourceTypes.AddGraphParameters(result, graph, args[1] == "fragment");
                    result = ShaderSourceTypes.AddGraphImages(result, images ?? [], args[1] == "fragment", graph.GetMode());
                    if (light is not null) result = CanvasLightProgram.Annotate(result, light);
                    if (surface is not null) result = CanvasSurfaceProgram.Annotate(result, surface);
                }
                File.WriteAllBytes(temporary, result);
            }
            Console.Write(await Execute("spirv-val", "--target-env", deviceImport && HasByteTypes(result) ? "vulkan1.2" : "vulkan1.0", temporary));
            if (deviceImport) RenderingDevice.ValidateSourceModule(result, stage);
            else ShaderCompiler.ValidateInterface(result, fragment: args[1] == "fragment", mode: particleImport ? Shader.Mode.Particles : blitImport ? Shader.Mode.TextureBlit : Shader.Mode.CanvasItem);
            if (File.Exists(output) && new FileInfo(output).Length == result.Length &&
                File.ReadAllBytes(output).AsSpan().SequenceEqual(result))
            {
                Console.WriteLine($"Unchanged {input} ({args[1]}) -> {output}: {result.Length} bytes");
                return 0;
            }
            Directory.CreateDirectory(IOPath.GetDirectoryName(output)!);
            var adjacent = output + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(adjacent, result);
                File.Move(adjacent, output, overwrite: true);
            }
            finally { File.Delete(adjacent); }
            Console.WriteLine($"Imported {input} ({args[1]}) -> {output}: {result.Length} bytes");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"{input} ({args[1]}): {error.Message}");
            return 1;
        }
        finally { File.Delete(temporary); File.Delete(debugTemporary); }

    }

    static async Task<string> Execute(string executable, params string[] arguments)
    {
        var tool = IOPath.Combine(AppContext.BaseDirectory, "toolchain", "bin", executable);
        if (!File.Exists(tool))
            throw new FileNotFoundException($"The pinned shader import tool '{executable}' is missing. Run python3 tools/shaders/build_toolchain.py before building or publishing ShaderImport.", tool);
        var start = new ProcessStartInfo(tool) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Cannot start {executable}.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var diagnostic = (await stdout) + (await stderr);
        if (process.ExitCode != 0) throw new InvalidOperationException($"{executable} failed ({process.ExitCode}):\n{diagnostic}");
        return diagnostic;
    }

    static bool HasByteTypes(byte[] code) => Electron2D.MaterialLightingProgram.Module.Read(code).Any(i => Electron2D.MaterialLightingProgram.Op(i) == 21 && i[2] == 8);

    static byte[] CompileHLSL(string source, RenderingDevice.ShaderStage stage, string? includeDirectory, string builtInDirectory, bool deviceImport, bool particleImport,
        out Dictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)>? types)
    {
        var previousDirectory = Directory.GetCurrentDirectory();
        try
        {
            // The HLSL frontend exposes one include root; its working directory supplies the embedded library.
            Directory.SetCurrentDirectory(builtInDirectory);
            if (!ShaderCross.Init()) throw new InvalidOperationException("Cannot initialize shadercross: " + SDL.GetError());
            try
            {
                var code = ShaderSourceTypes.CompileHLSL(source, stage, includeDirectory, allowNative16: deviceImport);
                // Scalar programs retain the established import payload; descriptor arrays preserve their source shape.
                if (!ShaderSourceTypes.HasDescriptorArrays(code) && !Electron2D.MaterialLightingProgram.Module.Read(code).Any(i => Electron2D.MaterialLightingProgram.Op(i) is 21 or 22 && i[2] == 16)) code = CompileDeviceHLSL(source, stage, includeDirectory);
                types = deviceImport ? null : ShaderSourceTypes.HLSL(source, stage == RenderingDevice.ShaderStage.Fragment, includeDirectory, code, particleImport);
                return code;
            }
            finally { ShaderCross.Quit(); }
        }
        finally { Directory.SetCurrentDirectory(previousDirectory); }
    }

    static byte[] CompileDeviceHLSL(string source, RenderingDevice.ShaderStage stage, string? includeDirectory)
    {
        var memory = ShaderCross.CompileSPIRVFromHLSL(source, "main",
            stage == RenderingDevice.ShaderStage.Compute ? ShaderCross.ShaderStage.Compute : stage == RenderingDevice.ShaderStage.Fragment ? ShaderCross.ShaderStage.Fragment : ShaderCross.ShaderStage.Vertex, out var size, includeDirectory);
        if (memory == 0) throw new ArgumentException("HLSL compilation failed: " + SDL.GetError(), nameof(source));
        try
        {
            if (size > 16 * 1024 * 1024) throw new ArgumentException("Shader bytecode exceeds 16 MiB.");
            var code = new byte[checked((int)size)]; Marshal.Copy(memory, code, 0, code.Length); return code;
        }
        finally { SDL.Free(memory); }
    }

    static async Task RequireVersion(string executable, string expected)
    {
        var version = await Execute(executable, "--version");
        var firstLine = version.Split('\n')[0].TrimEnd('\r');
        if (firstLine != expected && !firstLine.StartsWith(expected + " ", StringComparison.Ordinal))
            throw new InvalidOperationException($"The shader toolchain requires {expected}; reported version:\n{version}");
    }

    static async Task<int> ImportVisualGraph(string[] args)
    {
        var input = IOPath.GetFullPath(args[0]);
        var output = IOPath.GetFullPath(args[2]);
        var temporary = IOPath.Combine(IOPath.GetTempPath(), "electron2d-graph-" + Guid.NewGuid().ToString("N"));
        try
        {
            if (args[1] is not ("graph" or "canvas" or "particles" or "texture-blit") || !output.EndsWith(".e2dres", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Visual shader import requires a supported graph domain and an .e2dres destination.");
            using var graph = ResourceLoader.Load<VisualShader>(input, ResourceLoader.CacheMode.IgnoreDeep);
            using var sources = graph.GenerateDomainSources();
            if (args[1] != "graph" && args[1] != (sources.Mode == Shader.Mode.CanvasItem ? "canvas" : sources.Mode == Shader.Mode.Particles ? "particles" : "texture-blit"))
                throw new ArgumentException("The requested graph domain differs from its authored mode.");
            Directory.CreateDirectory(temporary);
            if (sources.Mode == Shader.Mode.CanvasItem)
            {
                var vertex = IOPath.Combine(temporary, "graph.vert.glsl"); var fragment = IOPath.Combine(temporary, "graph.frag.glsl");
                File.WriteAllText(vertex, sources.Vertex); File.WriteAllText(fragment, sources.Code);
                if (await ImportModule([vertex, "vertex", vertex + ".spv"], graph, sources.Images, sources.Blocks) != 0 ||
                    await ImportModule([fragment, "fragment", fragment + ".spv"], graph, sources.Images, sources.Blocks, sources.Light, sources.Surface) != 0)
                    throw new InvalidDataException("Generated canvas graph source failed compilation or interface validation.");
                graph.SetSPIRV(File.ReadAllBytes(vertex + ".spv"), File.ReadAllBytes(fragment + ".spv"));
            }
            else
            {
                var particle = sources.Mode == Shader.Mode.Particles;
                var source = IOPath.Combine(temporary, particle ? "graph.comp.glsl" : "graph.frag.glsl"); File.WriteAllText(source, sources.Code);
                if (await ImportModule([source, particle ? "compute" : "fragment", source + ".spv", particle ? "--particles" : "--texture-blit"], graph, sources.Images, sources.Blocks) != 0)
                    throw new InvalidDataException("Generated graph source failed compilation or interface validation.");
                graph.SetSPIRV(File.ReadAllBytes(source + ".spv"), sources.Mode);
            }
            var compiled = graph.GetProgram();
            foreach (var image in sources.Images.Where(i => i.Default is not null).DistinctBy(i => i.Name))
            {
                if (!compiled.Textures.Any(t => t.Name == image.Name && t.IsMaterialTexture)) continue;
                if (image.IsArray) graph.SetDefaultLayeredTextureParameter(image.Name, (TextureLayered?)image.Default);
                else graph.SetDefaultTextureParameter(image.Name, (Texture?)image.Default);
            }
            Directory.CreateDirectory(IOPath.GetDirectoryName(output)!);
            var identity = Environment.GetEnvironmentVariable("ELECTRON2D_SHADER_IMPORT_OUTPUT_IDENTITY");
            if (!string.IsNullOrEmpty(identity))
            {
                identity = IOPath.GetFullPath(identity);
                var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (!identity.EndsWith(".e2dres", StringComparison.OrdinalIgnoreCase) || !string.Equals(IOPath.GetDirectoryName(identity), IOPath.GetDirectoryName(output), comparison))
                    throw new InvalidDataException("Staged graph publication requires an adjacent final resource identity.");
                // The SDK stages to a unique path; retain the destination's persistent UID.
                var uid = ResourceArchive.ReadUID(identity);
                if (uid != ResourceUID.InvalidID) ResourceUID.SetID(uid, output);
            }
            ResourceArchive.Save(graph, output, SaverFlags.BundleResources | SaverFlags.Compress, skipUnchanged: true);
            Console.WriteLine($"Imported visual shader {input} -> {output}");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"{input} (visual-shader): {error.Message}");
            return 1;
        }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
    }
}
