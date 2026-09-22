using System.Diagnostics;
using Electron2D;
using SDL3;
using System.Runtime.InteropServices;

if (args.Length != 3 || args[1] is not ("vertex" or "fragment"))
{
    Console.Error.WriteLine("Usage: ShaderImport <source.hlsl|source.glsl|source.spv> <vertex|fragment> <output.spv>");
    return 2;
}
var input = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[2]);
var temporary = Path.Combine(Path.GetTempPath(), "electron2d-shader-" + Guid.NewGuid().ToString("N") + ".spv");
try
{
    await RequireVersion("spirv-val", "SPIRV-Tools v2026.3");
    switch (Path.GetExtension(input).ToLowerInvariant())
    {
        case ".hlsl":
            var source = File.ReadAllText(input);
            // Keep original source locations in diagnostics reported by the native compiler.
            var escaped = input.Replace("\\", "/").Replace("\"", "\\\"");
            var code = CompileHLSL("#line 1 \"" + escaped + "\"\n" + source,
                fragment: args[1] == "fragment", includeDirectory: Path.GetDirectoryName(input));
            File.WriteAllBytes(temporary, code);
            break;
        case ".glsl":
            await RequireVersion("glslangValidator", "Glslang Version: 11:16.4.0");
            Console.Write(await Execute("glslangValidator", "-V", "--target-env", "vulkan1.0", "-S",
                args[1] == "fragment" ? "frag" : "vert", "-e", "main", "-o", temporary, input));
            break;
        case ".spv":
            File.Copy(input, temporary);
            break;
        default: throw new ArgumentException("The supported source languages are HLSL and GLSL; external compilers must supply SPIR-V.");
    }
    Console.Write(await Execute("spirv-val", "--target-env", "vulkan1.0", temporary));
    var result = File.ReadAllBytes(temporary);
    ShaderCompiler.ValidateInterface(result, fragment: args[1] == "fragment");
    if (File.Exists(output) && new FileInfo(output).Length == result.Length &&
        File.ReadAllBytes(output).AsSpan().SequenceEqual(result))
    {
        Console.WriteLine($"Unchanged {input} ({args[1]}) -> {output}: {result.Length} bytes");
        return 0;
    }
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
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
finally { File.Delete(temporary); }

static async Task<string> Execute(string executable, params string[] arguments)
{
    var tool = Path.Combine(AppContext.BaseDirectory, "toolchain", "bin", executable);
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

static byte[] CompileHLSL(string source, bool fragment, string? includeDirectory)
{
    if (!ShaderCross.Init()) throw new InvalidOperationException("Cannot initialize shadercross: " + SDL.GetError());
    try
    {
        var memory = ShaderCross.CompileSPIRVFromHLSL(source, "main",
            fragment ? ShaderCross.ShaderStage.Fragment : ShaderCross.ShaderStage.Vertex, out var size, includeDirectory);
        if (memory == 0) throw new ArgumentException("HLSL compilation failed: " + SDL.GetError(), nameof(source));
        try
        {
            if (size > 16 * 1024 * 1024) throw new ArgumentException("Shader bytecode exceeds 16 MiB.");
            var code = new byte[checked((int)size)];
            Marshal.Copy(memory, code, 0, code.Length);
            return code;
        }
        finally { SDL.Free(memory); }
    }
    finally { ShaderCross.Quit(); }
}

static async Task RequireVersion(string executable, string expected)
{
    var version = await Execute(executable, "--version");
    var firstLine = version.Split('\n')[0].TrimEnd('\r');
    if (firstLine != expected && !firstLine.StartsWith(expected + " ", StringComparison.Ordinal))
        throw new InvalidOperationException($"The shader toolchain requires {expected}; reported version:\n{version}");
}
