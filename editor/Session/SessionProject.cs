using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Electron2D.Editor;

internal sealed record ProjectManifest(int Version, string Name, string Project, string Scene, string Code);

/// <summary>Describes an existing source project whose authoring documents belong to a shared session.</summary>
/// <remarks>The version-two manifest names one ordinary root .NET project, one typed scene and one C# buffer.
/// Opening does not activate gameplay. Binary SDK references are frozen by each job, not followed into a mutable
/// source SDK during compilation. Additional package/project/import closures require a separately verified profile.</remarks>
public sealed class SessionProject
{
    private SessionProject(string root, ProjectManifest manifest)
    { Root = root; Name = manifest.Name; ProjectFile = manifest.Project; SceneFile = manifest.Scene; CodeFile = manifest.Code; }
    /// <summary>Gets the canonical source root.</summary>
    public string Root { get; }
    /// <summary>Gets the manifest's display name.</summary>
    public string Name { get; }
    /// <summary>Gets the project-relative executable .csproj.</summary>
    public string ProjectFile { get; }
    /// <summary>Gets the project-relative typed scene.</summary>
    public string SceneFile { get; }
    /// <summary>Gets the project-relative shared C# source buffer.</summary>
    public string CodeFile { get; }
    /// <summary>Opens a version-two project description without creating a writer or starting a scene.</summary>
    /// <param name="directory">Existing project root.</param><returns>The validated immutable description.</returns>
    public static SessionProject Open(string directory)
    {
        var root = IOPath.TrimEndingDirectorySeparator(IOPath.GetFullPath(directory));
        var manifest = JobProtocol.Read(JobProtocol.ChildPath(root, "electron2d.project.json"), JobJSON.Default.ProjectManifest);
        if (manifest.Version != 2 || string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 128 ||
            IOPath.GetExtension(manifest.Project) != ".csproj" || IOPath.GetExtension(manifest.Code) != ".cs" || manifest.Scene == manifest.Code)
            throw new InvalidDataException("Invalid version-two project manifest.");
        foreach (var path in new[] { manifest.Project, manifest.Scene, manifest.Code })
            if (!File.Exists(JobProtocol.ChildPath(root, path))) throw new FileNotFoundException("A project input is missing.", path);
        return new(root, manifest);
    }
    /// <summary>Acquires the shared authoring owner for this description's scene and code.</summary>
    /// <returns>A caller-owned session; an existing live owner rejects explicitly.</returns>
    public ProjectSession OpenSession() => ProjectSession.Open(Root, SceneFile, CodeFile);

    /// <summary>Creates a minimal executable 2D source project in a new directory, using a qualified binary SDK.</summary>
    /// <param name="directory">New project directory; existing paths are preserved and rejected.</param>
    /// <param name="sdkDirectory">Output containing the runtime, session assembly and native libraries.</param><returns>The created description.</returns>
    public static SessionProject Create(string directory, string sdkDirectory)
    {
        directory = IOPath.TrimEndingDirectorySeparator(IOPath.GetFullPath(directory)); sdkDirectory = IOPath.TrimEndingDirectorySeparator(IOPath.GetFullPath(sdkDirectory));
        SnapshotStore.Inventory(sdkDirectory, true);
        if (IOPath.Exists(directory)) throw new IOException("The project directory already exists.");
        var name = IOPath.GetFileName(directory);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 128) throw new ArgumentException("Project names require 1–128 characters.", nameof(directory));
        var parent = IOPath.GetDirectoryName(directory)!; Directory.CreateDirectory(parent);
        var temporary = IOPath.Combine(parent, ".electron2d-create-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temporary);
        try
        {
            var manifest = new ProjectManifest(2, name, "Game.csproj", "scene.e2dscene", "Game.cs");
            SessionStorage.AtomicWrite(IOPath.Combine(temporary, "electron2d.project.json"), JsonSerializer.SerializeToUtf8Bytes(manifest, JobJSON.Default.ProjectManifest));
            var document = new XDocument(new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                new XElement("PropertyGroup", new XElement("OutputType", "Exe"), new XElement("TargetFramework", "net10.0"),
                    new XElement("ImplicitUsings", "enable"), new XElement("Nullable", "enable"), new XElement("TreatWarningsAsErrors", "true")),
                new XElement("ItemGroup", Reference("Electron2D"), Reference("Electron2D.Editor.Session"),
                    new XElement("None", new XAttribute("Include", IOPath.Combine(sdkDirectory, "lib*.so*")),
                        new XAttribute("Link", "%(Filename)%(Extension)"), new XAttribute("CopyToOutputDirectory", "PreserveNewest")),
                    new XElement("None", new XAttribute("Include", IOPath.Combine(sdkDirectory, "runtimes", System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier, "native", "lib*.so*")),
                        new XAttribute("Link", "%(Filename)%(Extension)"), new XAttribute("CopyToOutputDirectory", "PreserveNewest")))));
            document.Save(IOPath.Combine(temporary, "Game.csproj"));
            using (var root = new Entity { Name = "Scene" })
            {
                var square = new Polygon
                {
                    Name = "Square",
                    Position = new(40, 40),
                    Color = Colors.CornflowerBlue,
                    Vertices = [new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)]
                };
                root.AddChild(square); square.Owner = root;
                ResourceSaver.SaveScene(root, IOPath.Combine(temporary, manifest.Scene), SaverFlags.BundleResources);
            }
            File.WriteAllText(IOPath.Combine(temporary, manifest.Code), Template, new UTF8Encoding(false));
            File.WriteAllText(IOPath.Combine(temporary, ".gitignore"), ".electron2d/\nbin/\nobj/\n", new UTF8Encoding(false));
            Directory.Move(temporary, directory);
        }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
        return Open(directory);
        XElement Reference(string assembly) => new("Reference", new XAttribute("Include", assembly), new XElement("HintPath", IOPath.Combine(sdkDirectory, assembly + ".dll")));
    }

    private const string Template = """
        using Electron2D;
        using Electron2D.Editor;

        return SessionRunHost.Run(args, CreateGame);
        static SessionScenario CreateGame() => new Game();

        sealed class Game : SessionScenario
        {
            protected override void SceneLoaded(SessionRunContext context)
            {
                var square = (Polygon)context.Root.GetNode("Square");
                square.Color = Colors.CornflowerBlue;
            }
            protected override void AfterStep(SessionRunContext context)
            {
                var square = (Polygon)context.Root.GetNode("Square");
                context.Observe("square-x", square.Position.X);
                context.Check("finite-position", float.IsFinite(square.Position.X), "The snapshot pose must remain finite.");
                if (context.Step == context.StepBudget) context.Complete();
            }
        }
        """;
}
