using System.Security;
using System.Text.Json;

namespace Electron2D.Tools.Batch;

internal static class ProjectOperations
{
    internal static ProjectManifest Open(string directory)
    {
        var manifest = Protocol.Read(IOPath.Combine(directory, "electron2d.project.json"), ProtocolJSON.Default.ProjectManifest);
        if (manifest.Version != 1 || string.IsNullOrWhiteSpace(manifest.Project) || string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 128 || !manifest.Project.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || !File.Exists(Protocol.ChildPath(directory, manifest.Project)))
            throw new BatchFailure("INVALID_PROJECT", 2, "The project manifest version, name or project file is invalid.");
        return manifest;
    }
    internal static void Create(string directory, string sdk)
    {
        if (IOPath.Exists(directory)) throw new BatchFailure("PROJECT_EXISTS", 2, "new never replaces an existing path.");
        var batch = IOPath.Combine(sdk, "tools", "Batch", "Batch.csproj"); var native = IOPath.Combine(sdk, "tools", "native-consumer.props");
        if (!File.Exists(batch) || !File.Exists(native) || !File.Exists(IOPath.Combine(sdk, "Electron2D.csproj"))) throw new BatchFailure("INVALID_SDK", 2, "The source SDK is incomplete.");
        var name = IOPath.GetFileName(IOPath.TrimEndingDirectorySeparator(directory));
        if (string.IsNullOrWhiteSpace(name) || name.Length > 128) throw new BatchFailure("INVALID_PROJECT", 2, "Project names require 1..128 nonblank characters.");
        var parent = IOPath.GetDirectoryName(directory)!; Directory.CreateDirectory(parent);
        var temporary = IOPath.Combine(parent, ".electron2d-new-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temporary);
        try
        {
            var manifest = new ProjectManifest(1, name, "Game.csproj");
            File.WriteAllText(IOPath.Combine(temporary, "electron2d.project.json"), JsonSerializer.Serialize(manifest, ProtocolJSON.Default.ProjectManifest));
            File.WriteAllText(IOPath.Combine(temporary, "Game.csproj"), $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>
                  <ItemGroup><ProjectReference Include="{SecurityElement.Escape(IOPath.GetRelativePath(directory, batch))}" /></ItemGroup>
                  <Import Project="{SecurityElement.Escape(IOPath.GetRelativePath(directory, native))}" />
                </Project>
                """);
            using var source = typeof(ProjectOperations).Assembly.GetManifestResourceStream("Electron2D.Batch.Scenario.cs")!;
            using (var target = File.Create(IOPath.Combine(temporary, "Program.cs"))) source.CopyTo(target);
            File.WriteAllText(IOPath.Combine(temporary, "scenario.json"), """
                {"version":1,"mode":"Headless","steps":6,"fps":60,"width":64,"height":64,"seed":7,
                 "inputs":[{"step":1,"action":"move","pressed":true},{"step":4,"action":"move","pressed":false}]}
                """);
            File.WriteAllText(IOPath.Combine(temporary, ".gitignore"), ".electron2d/\nbin/\nobj/\n");
            Directory.Move(temporary, directory);
        }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
    }
    internal static async Task<string> Build(string directory, ProjectManifest manifest, string output, CancellationToken cancellation)
    {
        var project = Protocol.ChildPath(directory, manifest.Project); var artifacts = Protocol.ChildPath(directory, ".electron2d/build");
        var settings = new[] { "-p:UseSharedCompilation=false", "-nr:false", "-p:Configuration=Release", "-p:ArtifactsPath=" + artifacts };
        var exit = await ProcessJob.Run("dotnet", ["build", project, "--nologo", .. settings], directory, output, "build", 300000, cancellation);
        if (exit != 0) throw new BatchFailure("BUILD_FAILED", 3, $"Project build failed with exit code {exit}; inspect build logs.");
        exit = await ProcessJob.Run("dotnet", ["msbuild", project, "-nologo", "-getProperty:TargetPath", .. settings], directory, output, "target", 30000, cancellation);
        if (exit != 0) throw new BatchFailure("BUILD_FAILED", 3, "The project did not expose its executable target.");
        var target = File.ReadAllText(IOPath.Combine(output, "target.stdout.log")).Trim();
        target = IOPath.GetFullPath(target, directory);
        if (!File.Exists(target) || IOPath.GetExtension(target) != ".dll" || !File.Exists(IOPath.ChangeExtension(target, ".runtimeconfig.json")))
            throw new BatchFailure("BUILD_FAILED", 3, "The project must build a runnable managed executable.");
        return target;
    }
}
