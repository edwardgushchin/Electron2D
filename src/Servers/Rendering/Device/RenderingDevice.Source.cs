using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using IOPath = System.IO.Path;

namespace Electron2D;

public sealed partial class RenderingDevice
{
    /// <summary>Identifies the two supported authoring languages.</summary>
    public enum ShaderLanguage
    {
        /// <summary>GLSL source compiled by the pinned import frontend.</summary>
        GLSL = 0,
        /// <summary>HLSL source compiled by the pinned import frontend.</summary>
        HLSL = 1
    }
    private readonly Dictionary<string, byte[]> _sourceCache = [];
    private int _sourceCacheBytes;
    /// <summary>Compiles copied stage sources through the separately installed authoring tool.</summary>
    /// <param name="shaderSource">Non-null vertex, fragment and compute sources with one language.</param>
    /// <param name="allowCache">Permits bounded successful-module caching; false bypasses both lookup and storage. Sources with preprocessing dependencies are compiled every time.</param>
    /// <returns>A new resource with independent stage bytecode and compiler diagnostics. Failed stages remain empty; other stages are still attempted.</returns>
    /// <remarks>This owner-thread cold authoring operation creates no GPU program and does not submit or complete pending work. Configure ELECTRON2D_SHADER_IMPORT_TOOL with the published ShaderImport executable, or install it beside the host. Relative includes resolve from the current project directory. Games loading compiled modules require no source compiler.</remarks>
    /// <exception cref="NotSupportedException">The separate import executable is unavailable.</exception>
    /// <exception cref="ArgumentException">The language/source payload is invalid.</exception>
    public RDShaderSPIRV ShaderCompileSPIRVFromSource(RDShaderSource shaderSource, bool allowCache = true)
    {
        Access(); ArgumentNullException.ThrowIfNull(shaderSource); var language = shaderSource.Language;
        if (!Enum.IsDefined(language)) throw new ArgumentOutOfRangeException(nameof(shaderSource));
        var sources = new[] { shaderSource.SourceVertex, shaderSource.SourceFragment, shaderSource.SourceCompute };
        foreach (var source in sources) if (source.Length > 1_048_576 || source.Contains('\0')) throw new ArgumentException("Shader sources require bounded text without embedded nulls.", nameof(shaderSource));
        var result = new RDShaderSPIRV(); if (sources.All(s => s.Length == 0)) return result;
        try
        {
            var tool = IOPath.GetFullPath(Environment.GetEnvironmentVariable("ELECTRON2D_SHADER_IMPORT_TOOL") ?? IOPath.Combine(AppContext.BaseDirectory, "Electron2D.ShaderImport" + (OperatingSystem.IsWindows() ? ".exe" : "")));
            if (!File.Exists(tool)) throw new NotSupportedException("Install the shader authoring tool and configure ELECTRON2D_SHADER_IMPORT_TOOL before compiling source.");
            tool = File.ResolveLinkTarget(tool, returnFinalTarget: true)?.FullName ?? tool;
            var identity = allowCache ? CompilerIdentity(tool) : ""; var includeRoot = ProjectSettings.GlobalizePath("res://");
            ShaderStage[] stages = [ShaderStage.Vertex, ShaderStage.Fragment, ShaderStage.Compute];
            for (var i = 0; i < sources.Length; i++)
            {
                if (sources[i].Length == 0) continue;
                var cacheable = allowCache && sources[i].Split('\n').All(line => !line.Contains('#') || Regex.IsMatch(line, @"^\s*#\s*version\s+\d+\s*$", RegexOptions.CultureInvariant));
                var key = identity + ":" + (int)language + ":" + (int)stages[i] + ":" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sources[i])));
                if (cacheable && _sourceCache.TryGetValue(key, out var cached)) { result.SetStageBytecode(stages[i], cached); continue; }
                try
                {
                    var code = CompileSource(tool, language, stages[i], sources[i], includeRoot); ValidateSourceModule(code, stages[i]); result.SetStageBytecode(stages[i], code);
                    if (cacheable)
                    {
                        if (_sourceCache.Count >= 16 || _sourceCacheBytes + code.Length > 16 * 1024 * 1024) { _sourceCache.Clear(); _sourceCacheBytes = 0; }
                        _sourceCache[key] = code; _sourceCacheBytes += code.Length;
                    }
                }
                catch (Exception error) when (error is ArgumentException or InvalidOperationException or IOException or TimeoutException or System.ComponentModel.Win32Exception or NotSupportedException or OverflowException)
                { result.SetStageCompileError(stages[i], stages[i] + ": " + error.Message); }
            }
            return result;
        }
        catch { result.Dispose(); throw; }
    }
    private static string CompilerIdentity(string tool)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); var directory = IOPath.GetDirectoryName(tool)!;
        var files = Directory.EnumerateFiles(directory, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint, IgnoreInaccessible = false, MaxRecursionDepth = 16 }).Append(tool).Distinct(StringComparer.Ordinal).Where(path => path == tool || (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) || (path.EndsWith(".so", StringComparison.OrdinalIgnoreCase) || IOPath.GetFileName(path).Contains(".so.", StringComparison.Ordinal)) || path.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase) || path.Contains(IOPath.DirectorySeparatorChar + "toolchain" + IOPath.DirectorySeparatorChar, StringComparison.Ordinal)).Order(StringComparer.Ordinal);
        var buffer = new byte[64 * 1024];
        foreach (var file in files)
        { var name = Encoding.UTF8.GetBytes(IOPath.GetRelativePath(directory, file)); hash.AppendData(BitConverter.GetBytes(name.Length)); hash.AppendData(name); using var input = File.OpenRead(file); hash.AppendData(BitConverter.GetBytes(input.Length)); int read; while ((read = input.Read(buffer)) != 0) hash.AppendData(buffer.AsSpan(0, read)); }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    private static byte[] CompileSource(string tool, ShaderLanguage language, ShaderStage stage, string source, string includeRoot, int timeoutMS = 60_000)
    {
        var directory = Directory.CreateTempSubdirectory("electron2d-source-").FullName;
        try
        {
            var input = IOPath.Combine(directory, stage.ToString().ToLowerInvariant() + (language == ShaderLanguage.GLSL ? ".glsl" : ".hlsl")); var output = IOPath.Combine(directory, "module.spv"); File.WriteAllText(input, source, new UTF8Encoding(false));
            var start = new ProcessStartInfo(tool) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = includeRoot };
            foreach (var argument in new[] { input, stage.ToString().ToLowerInvariant(), output, "--device", includeRoot }) start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start the shader authoring tool.");
            var stdout = ReadDiagnostic(process.StandardOutput); var stderr = ReadDiagnostic(process.StandardError);
            if (!process.WaitForExit(timeoutMS)) { process.Kill(entireProcessTree: true); process.WaitForExit(); Task.WaitAll(stdout, stderr); throw new TimeoutException("Shader authoring exceeded sixty seconds."); }
            var diagnostic = stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult();
            if (process.ExitCode != 0) throw new InvalidOperationException(diagnostic.Length == 0 ? "Shader authoring failed with exit code " + process.ExitCode + "." : diagnostic);
            if (!File.Exists(output) || new FileInfo(output).Length is < 20 or > 16 * 1024 * 1024) throw new ArgumentException("The shader authoring tool produced no bounded SPIR-V module.");
            return File.ReadAllBytes(output);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
    private static async Task<string> ReadDiagnostic(StreamReader stream)
    {
        var output = new StringBuilder(); var buffer = new char[4096]; int read;
        while ((read = await stream.ReadAsync(buffer).ConfigureAwait(false)) != 0) if (output.Length < 1_048_576) output.Append(buffer, 0, Math.Min(read, 1_048_576 - output.Length));
        return output.ToString();
    }
    internal static void ValidateSourceModule(byte[] code, ShaderStage stage)
    { var module = new SpecializationModule(code, [], []); RemapCore(code.ToArray(), stage, module.HasSpecialization); }
}
