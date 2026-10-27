using Electron2D;
using System.Diagnostics;
using Path = System.IO.Path;

internal static partial class ShaderSamplerChecks
{
    private static void Archives(Shader shader)
    {
        var folder = Directory.CreateTempSubdirectory("electron2d-sampler-").FullName;
        try
        {
            var path = Path.Combine(folder, "sampler.e2dres"); ResourceSaver.Save(shader, path);
            ArchiveChild(path);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(Path.GetFullPath(Environment.GetCommandLineArgs()[0]));
            start.ArgumentList.Add("archive-child"); start.ArgumentList.Add(path);
            using var child = Process.Start(start)!;
            var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(entireProcessTree: true); child.WaitForExit(); throw new InvalidOperationException("Archive child timed out."); }
            Check(child.ExitCode == 0, "Fresh process sampler archive: " + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(folder, recursive: true); }
    }
    private static void ArchiveChild(string path)
    {
        using var shader = ResourceLoader.Load<Shader>(path, ResourceLoader.CacheMode.IgnoreDeep);
        using var state = shader.GetTextureSamplerState("TEXTURE")!;
        Check(state.MinLOD == 1 && state.MaxLOD == 2 && state.LODBias == .5f && state.RepeatU == RenderingDevice.SamplerRepeatMode.MirroredRepeat && state.UseAnisotropy && state.AnisotropyMax == 4 && state.BorderColor == RenderingDevice.SamplerBorderColor.FloatOpaqueWhite, "Physical shader archive restores sampler metadata after its program.");
        Console.WriteLine("Fresh sampler archive restored.");
    }
}
