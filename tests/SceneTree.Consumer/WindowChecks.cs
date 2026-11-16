using Electron2D;

internal static class WindowChecks
{
    internal static void Run(string renderer)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
        var root = new Window { Title = "T-0073 SceneTree", Size = new(160, 120) };
        SceneTree? captured = null; var frames = 0; var exited = false;
        root.Ready += _ =>
        {
            var tree = root.Tree!; captured = tree;
            Check(tree.Root == root && tree.NodeCount == 1, "native root enters before ready");
            var child = new Node(); root.AddChild(child);
            Check(tree.NodeCount == 2, "native root add");
            child.QueueFree();
            tree.ProcessFrameStarted += _ =>
            {
                if (++frames == 2)
                {
                    Check(child.IsDisposed && tree.NodeCount == 1 && tree.Root == root, "native frame safe point preserves root");
                    tree.Quit();
                }
            };
            tree.NodeRemoved += (t, n) => { if (n == root) { Check(t.Root == root && t.NodeCount == 0, "native root final exit count"); exited = true; } };
        };
        Check(Engine.Run(root) == 0 && root.IsDisposed && captured!.Root == root && exited && frames >= 2, "native shutdown");
        Console.WriteLine("SceneTree Window " + renderer + " checks passed.");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
