using System.Diagnostics;
using Electron2D;
using Flags = Electron2D.Node.DuplicateFlags;

internal static partial class NodeDuplicationTests
{
    private static bool _registered;
    private static Actor FileActor() => new();
    private static void Register()
    {
        if (_registered) return;
        ResourceFileTypes.RegisterNode("tests.node-duplicate.actor", FileActor); _registered = true;
    }
    private static void Files()
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-duplicate-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try { Process("produce", folder); Process("read", folder); }
        finally { Directory.Delete(folder, true); }
    }
    private static void Process(string mode, string folder)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        if ((System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "").Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(NodeDuplicationTests).Assembly.Location);
        foreach (var key in start.Environment.Keys.Where(key => key.StartsWith("ELECTRON2D_TEST_", StringComparison.Ordinal)).ToArray()) start.Environment.Remove(key);
        start.ArgumentList.Add("--node-duplication"); start.ArgumentList.Add(mode); start.ArgumentList.Add(folder);
        using var process = System.Diagnostics.Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) { process.Kill(entireProcessTree: true); throw new TimeoutException("Duplicate scene child timed out."); }
        Check(process.ExitCode == 0, "Fresh process " + mode + ": " + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }
    internal static void Child(string mode, string folder)
    {
        Register(); var path = System.IO.Path.Combine(folder, "copy.e2dscene");
        if (mode == "produce")
        {
            using var original = new Actor { Name = "Author", Score = 77 }; var child = new Actor { Name = "Child", Score = 8 }; original.AddChild(child); child.Owner = original; original.Link = child; child.Link = original;
            using var image = Image.CreateEmpty(12, 12, false, Image.Format.Rgba8); image.Fill(Colors.White);
            var texture = ImageTexture.CreateFromImage(image); texture.ResourceLocalToScene = true; original.Asset = texture; original.AdoptSceneResources([texture]);
            var visual = new Sprite { Name = "Visual", Texture = texture, Centered = false }; original.AddChild(visual); visual.Owner = original;
            using var copy = (Actor)original.Duplicate(); original.Dispose(); Check(!texture.IsDisposed && copy.Asset == texture, "Authoring duplicate retains owned resources"); copy.Name = "Saved";
            foreach (var node in copy.GetChildren()) node.Owner = copy;
            using var packed = new PackedScene(); packed.Pack(copy); ResourceSaver.Save(packed, path); return;
        }
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); var source = (Actor)scene.Instantiate();
        using var duplicate = (Actor)source.Duplicate(Flags.Scripts); source.Dispose(); scene.Dispose();
        Check(duplicate.Score == 77 && duplicate.Link == duplicate.GetNode("Child") && ((Actor)duplicate.Link!).Link == duplicate && duplicate.Asset is { IsDisposed: false }, "Fresh load, values, references and ownership");
        using var tree = new SceneTree(duplicate); tree.ProcessFrame(1f / 60); Check(duplicate.Readied == 1 && duplicate.Calls == 1 && ((Actor)duplicate.Link!).Calls == 1, "Fresh-process headless lifecycle and simulation");
    }
}
