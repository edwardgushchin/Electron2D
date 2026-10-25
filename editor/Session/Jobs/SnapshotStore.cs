using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace Electron2D.Editor;

internal sealed record SnapshotManifest(int Version, Guid ID, Guid SceneID, Guid CodeID, ulong SceneRevision,
    ulong CodeRevision, string SHA256, SessionSnapshotFile[] Files);
internal sealed record CapturedProject(string Root, string ScenePath, string CodePath, Guid SceneID, Guid CodeID,
    ulong SceneRevision, ulong CodeRevision, byte[] Scene, NodeState[] Nodes, byte[] Code,
    SessionSnapshotFile[] SourceFiles, SessionSnapshotFile[] SDKFiles, SessionJobOptions Options);

internal static class SnapshotStore
{
    internal const long SourceLimit = 128L * 1024 * 1024;
    internal const long SDKLimit = 256L * 1024 * 1024;
    private static readonly string[] Ignored = [".git", ".electron2d", ".session-sdk", "bin", "obj"];

    internal static SessionSnapshotFile[] Inventory(string root, bool sdk)
    {
        var files = new List<SessionSnapshotFile>(); long total = 0;
        Walk(root);
        if (sdk && new[] { "Electron2D.dll", "Electron2D.Editor.Session.dll", "Electron2D.Editor.Session.runtimeconfig.json" }.Any(name => !files.Any(f => f.Path == name)))
            throw new JobFailure("INVALID_SDK", 2, "The SDK must contain the qualified runtime and session assemblies.");
        return files.OrderBy(f => f.Path, StringComparer.Ordinal).ToArray();
        void Walk(string directory)
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory).Order(StringComparer.Ordinal))
            {
                var name = IOPath.GetFileName(entry);
                if (Ignored.Contains(name, StringComparer.Ordinal)) continue;
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    if (sdk)
                    {
                        var relative = IOPath.GetRelativePath(root, entry).Replace('\\', '/');
                        var native = "runtimes/" + System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier + "/native";
                        if (relative != "runtimes" && relative != "runtimes/" + System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier && relative != native) continue;
                    }
                    if ((attributes & FileAttributes.ReparsePoint) != 0) throw new JobFailure("UNSAFE_PATH", 2, "Project snapshots cannot traverse symbolic links.");
                    Walk(entry); continue;
                }
                if (sdk && name is not ("Electron2D.dll" or "Electron2D.Editor.Session.dll" or "Electron2D.Editor.Session.runtimeconfig.json") && !name.Contains(".so", StringComparison.Ordinal)) continue;
                if (!sdk && (attributes & FileAttributes.ReparsePoint) != 0) throw new JobFailure("UNSAFE_PATH", 2, "Project snapshot files cannot be symbolic links.");
                var length = new FileInfo(entry).Length;
                total = checked(total + length);
                if (files.Count >= 4096 || length > (sdk ? SDKLimit : SourceLimit) || total > (sdk ? SDKLimit : SourceLimit))
                    throw new JobFailure("SNAPSHOT_TOO_LARGE", 2, "Snapshot file/count budget exceeded.");
                files.Add(new(IOPath.GetRelativePath(root, entry), length, JobProtocol.Hash(entry)));
            }
        }
    }

    internal static void VerifyInventory(string root, SessionSnapshotFile[] expected, bool sdk)
    {
        if (!Inventory(root, sdk).SequenceEqual(expected)) throw new JobFailure("INPUT_CHANGED", 2, "Project or SDK files changed while the snapshot was materialized; retry from current inputs.");
    }

    internal static SnapshotManifest Materialize(CapturedProject captured, Guid id, string input, string sdkVersion, CancellationToken cancellation)
    {
        Directory.CreateDirectory(input);
        foreach (var file in captured.SourceFiles)
        {
            cancellation.ThrowIfCancellationRequested();
            var from = JobProtocol.ChildPath(captured.Root, file.Path); var to = JobProtocol.ChildPath(input, file.Path);
            CopyChecked(from, to, file, cancellation);
        }
        var sdk = IOPath.Combine(input, ".session-sdk"); Directory.CreateDirectory(sdk);
        foreach (var file in captured.SDKFiles)
        {
            cancellation.ThrowIfCancellationRequested();
            CopyChecked(IOPath.Combine(captured.Options.SDKDirectory, file.Path), IOPath.Combine(sdk, file.Path), file, cancellation);
        }
        VerifyInventory(captured.Root, captured.SourceFiles, false); VerifyInventory(captured.Options.SDKDirectory, captured.SDKFiles, true);
        SessionStorage.AtomicWrite(JobProtocol.ChildPath(input, captured.ScenePath), captured.Scene);
        SessionStorage.AtomicWrite(JobProtocol.ChildPath(input, captured.CodePath), captured.Code);
        SessionStorage.AtomicWrite(IOPath.Combine(input, ".session-nodes.json"), JsonSerializer.SerializeToUtf8Bytes(captured.Nodes, SessionJSON.Default.NodeStateArray));
        var global = "{\"sdk\":{\"version\":\"" + sdkVersion + "\",\"rollForward\":\"disable\",\"allowPrerelease\":true}}";
        File.WriteAllText(IOPath.Combine(input, "global.json"), global, new UTF8Encoding(false));
        RewriteProject(JobProtocol.ChildPath(input, captured.Options.ProjectFile), captured.Options.SDKDirectory);
        var files = Inventory(input, false);
        // SDK inputs are deliberately included although their directory is hidden from normal project discovery.
        files = [.. files, .. captured.SDKFiles.Select(f => f with { Path = IOPath.Combine(".session-sdk", f.Path) })];
        files = files.OrderBy(f => f.Path, StringComparer.Ordinal).ToArray();
        var hash = Digest(id, captured.SceneID, captured.CodeID, captured.SceneRevision, captured.CodeRevision, files);
        var manifest = new SnapshotManifest(2, id, captured.SceneID, captured.CodeID, captured.SceneRevision, captured.CodeRevision, hash, files);
        foreach (var file in files) File.SetAttributes(JobProtocol.ChildPath(input, file.Path), FileAttributes.ReadOnly);
        return manifest;
    }

    private static void CopyChecked(string from, string to, SessionSnapshotFile file, CancellationToken cancellation)
    {
        Directory.CreateDirectory(IOPath.GetDirectoryName(to)!);
        using (var source = new FileStream(from, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read))
        using (var target = new FileStream(to, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None))
        {
            if (source.Length != file.Size) throw new JobFailure("INPUT_CHANGED", 2, "An input file changed size.");
            var buffer = new byte[65536]; int read;
            while ((read = source.Read(buffer)) != 0) { cancellation.ThrowIfCancellationRequested(); target.Write(buffer, 0, read); }
        }
        if (JobProtocol.Hash(to) != file.SHA256) throw new JobFailure("INPUT_CHANGED", 2, "An input file changed content.");
    }

    internal static string Digest(Guid id, Guid sceneID, Guid codeID, ulong sceneRevision, ulong codeRevision, SessionSnapshotFile[] files)
    {
        var data = new StringBuilder().Append(id).Append('\n').Append(sceneID).Append('\n').Append(codeID).Append('\n')
            .Append(sceneRevision).Append('\n').Append(codeRevision).Append('\n');
        foreach (var file in files) data.Append(file.Path.Replace('\\', '/')).Append('\0').Append(file.Size).Append('\0').Append(file.SHA256).Append('\n');
        return SessionStorage.Hash(Encoding.UTF8.GetBytes(data.ToString())).ToLowerInvariant();
    }

    internal static void Verify(SnapshotManifest manifest, string input, Guid id, string hash)
    {
        if (manifest.Version != 2 || manifest.ID != id || manifest.SHA256 != hash || manifest.Files.Length is 0 or > 8192 ||
            Digest(id, manifest.SceneID, manifest.CodeID, manifest.SceneRevision, manifest.CodeRevision, manifest.Files) != hash ||
            manifest.Files.Select(f => f.Path).Distinct(StringComparer.Ordinal).Count() != manifest.Files.Length)
            throw new JobFailure("INVALID_SNAPSHOT", 4, "Snapshot identity/inventory is invalid.");
        var actual = Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories).Select(p => IOPath.GetRelativePath(input, p)).Order(StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(manifest.Files.Select(f => f.Path).Order(StringComparer.Ordinal)))
            throw new JobFailure("SNAPSHOT_CHANGED", 4, "Frozen input membership changed.");
        foreach (var file in manifest.Files)
        {
            var path = JobProtocol.ChildPath(input, file.Path);
            if (new FileInfo(path).Length != file.Size || JobProtocol.Hash(path) != file.SHA256)
                throw new JobFailure("SNAPSHOT_CHANGED", 4, "A frozen input changed before execution.");
        }
    }

    private static void RewriteProject(string project, string sdkDirectory)
    {
        using var reader = XmlReader.Create(project, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var document = XDocument.Load(reader); reader.Close();
        if (document.Root?.Name.LocalName != "Project" || document.Root.Attribute("Sdk")?.Value != "Microsoft.NET.Sdk")
            throw new JobFailure("PROJECT_PROFILE_UNAVAILABLE", 7, "Snapshot jobs currently require one ordinary Microsoft.NET.Sdk executable project.");
        if (document.Descendants().Any(e => e.Name.LocalName is "ProjectReference" or "PackageReference" or "Import" or "Target" or "UsingTask"))
            throw new JobFailure("PROJECT_CLOSURE_UNAVAILABLE", 7, "Additional projects/packages/imports/tasks require an explicit frozen dependency closure; this profile uses the qualified binary SDK.");
        var properties = document.Descendants().Where(e => e.Parent?.Name.LocalName == "PropertyGroup").ToArray();
        if (properties.SingleOrDefault(e => e.Name.LocalName == "TargetFramework")?.Value != "net10.0" ||
            properties.SingleOrDefault(e => e.Name.LocalName == "OutputType")?.Value != "Exe" ||
            properties.Any(e => e.Name.LocalName is "TargetFrameworks" or "RuntimeIdentifier" or "RuntimeIdentifiers" or "SelfContained" or "PublishAot" or "DirectoryBuildPropsPath" or "DirectoryBuildTargetsPath"))
            throw new JobFailure("PROJECT_PROFILE_UNAVAILABLE", 7, "This profile requires a framework-dependent net10.0 executable without external build policy overrides.");
        var references = document.Descendants().Where(e => e.Name.LocalName == "Reference").ToArray();
        foreach (var reference in references)
        {
            var name = reference.Attribute("Include")?.Value;
            if (name is not ("Electron2D" or "Electron2D.Editor.Session")) throw new JobFailure("PROJECT_CLOSURE_UNAVAILABLE", 7, "A non-SDK assembly reference has no captured dependency closure.");
            var hint = reference.Elements().SingleOrDefault(e => e.Name.LocalName == "HintPath") ?? throw new JobFailure("INVALID_PROJECT", 2, "SDK references require a literal HintPath.");
            if (IOPath.GetFullPath(hint.Value, IOPath.GetDirectoryName(project)!) != IOPath.Combine(sdkDirectory, name + ".dll"))
                throw new JobFailure("INVALID_SDK", 2, "The project's assembly reference differs from the selected SDK.");
            hint.Value = IOPath.GetRelativePath(IOPath.GetDirectoryName(project)!, IOPath.Combine(IOPath.GetDirectoryName(project)!, ".session-sdk", name + ".dll"));
        }
        if (!references.Any(r => r.Attribute("Include")?.Value == "Electron2D") || !references.Any(r => r.Attribute("Include")?.Value == "Electron2D.Editor.Session"))
            throw new JobFailure("INVALID_SDK", 2, "Both public SDK assemblies must be referenced.");
        foreach (var attribute in document.Descendants().Attributes().Where(a => a.Name.LocalName is "Include" or "Update" or "Remove"))
        {
            if (attribute.Value.StartsWith(sdkDirectory + IOPath.DirectorySeparatorChar, StringComparison.Ordinal))
                attribute.Value = ".session-sdk/" + attribute.Value[(sdkDirectory.Length + 1)..];
            else if (IOPath.IsPathRooted(attribute.Value) || attribute.Value.Split('/', '\\').Contains("..") || attribute.Value.Contains("$(", StringComparison.Ordinal))
                throw new JobFailure("PROJECT_CLOSURE_UNAVAILABLE", 7, "A project item resolves outside the frozen input profile.");
        }
        document.Save(project);
    }
}

internal static class SceneOverlay
{
    internal static void Apply(Node root, NodeState[] nodes)
    {
        var index = 0; var parents = new Dictionary<Guid, Node>(); Visit(root);
        if (index != nodes.Length) throw new JobFailure("INVALID_SCENE_OVERLAY", 4, "The snapshot scene identity catalog differs from its archive.");
        void Visit(Node node)
        {
            if (index >= nodes.Length) throw new JobFailure("INVALID_SCENE_OVERLAY", 4, "The snapshot scene has unexpected nodes.");
            var state = nodes[index++];
            if (state.Name != node.Name || state.TypeName != node.GetType().FullName ||
                (node.Parent is null ? state.ParentID != Guid.Empty : !parents.TryGetValue(state.ParentID, out var parent) || parent != node.Parent))
                throw new JobFailure("INVALID_SCENE_OVERLAY", 4, "The snapshot's scene identity/hierarchy changed.");
            if (state.Transform is { } pose)
            {
                if (node is not Entity entity || !pose.IsFinite()) throw new JobFailure("INVALID_SCENE_OVERLAY", 4, "Invalid typed Entity pose overlay.");
                entity.Transform = pose;
            }
            parents.Add(state.ID, node); foreach (var child in node.GetChildren()) Visit(child);
        }
    }
}
