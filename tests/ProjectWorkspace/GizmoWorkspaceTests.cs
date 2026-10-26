using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

internal static class GizmoWorkspaceTests
{
    private sealed class Unsupported : SkeletonModification
    {
        internal static int Executions;
        protected override void OnExecute(double delta) => Executions++;
        protected override Resource CreateDuplicateInstance() => new Unsupported();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
            => CopyModificationState((Unsupported)target);
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    internal static int Run(string output, string renderer)
    {
        Directory.CreateDirectory(output);
        var oldHint = Engine.IsEditorHint; Engine.IsEditorHint = true;
        var project = SessionProject.Create(IOPath.Combine(output, "Game"), AppContext.BaseDirectory);
        WriteRig(project, false);
        File.WriteAllText(IOPath.Combine(project.Root, project.CodeFile), "using Electron2D.Editor;\nreturn SessionRunHost.Run(args, CreateGame);\nstatic SessionScenario CreateGame() => new Game();\nsealed class Game : SessionScenario { }\n");
        var disk = File.ReadAllBytes(IOPath.Combine(project.Root, project.SceneFile));
        using var session = project.OpenSession(); using var agent = session.Connect("gizmo author");
        var rigInfo = agent.InspectScene().Nodes.Single(n => n.Name == "Rig");
        using (var preview = agent.CreatePreview(agent.InspectScene().Revision))
        {
            var previewRig = (Skeleton)preview.GetNode("Rig");
            Check(previewRig.ProcessMode == ProcessMode.Disabled && previewRig.GetModificationStack()!.GetModification(0)!.EditorDrawGizmo,
                "Passive projection retains editor settings and disables numerical processing");
        }
        ResourceFileTypes.RegisterResource("workspace.gizmo.Unsupported", CreateUnsupported);
        var rejected = SessionProject.Create(IOPath.Combine(output, "Rejected"), AppContext.BaseDirectory); WriteRig(rejected, true);
        using (var rejectedOwner = rejected.OpenSession())
        using (var client = rejectedOwner.Connect("preview policy"))
        {
            try { using var bad = client.CreatePreview(client.InspectScene().Revision); throw new Exception("Expected unsupported modification"); }
            catch (NotSupportedException error) { Check(error.Message.Contains("modification Unsupported", StringComparison.Ordinal), "Unknown callbacks have an explicit capability error"); }
            Check(Unsupported.Executions == 0, "Rejected preview never executes project modification code");
        }
        using var regular = new FontFile(); regular.LoadDynamicFont(IOPath.Combine(AppContext.BaseDirectory, "Assets/IBMPlexSans-Regular.ttf"));
        using var mono = new SystemFont { FontNames = ["monospace"] };
        using var window = new Window { Title = "Electron2D skeletal authoring verification", Size = new(1280, 900) };
        var workspace = new ProjectWorkspace(project, session, AppContext.BaseDirectory, regular, mono);
        window.AddChild(workspace); workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        var viewport = (SubViewport)workspace.FindChild("SceneViewport", owned: false)!;
        var frame = 0; var before = 0f; var complete = false; Exception? failure = null;
        window.Ready += _ => RenderingServer.FramePostDraw += Drawn;
        try
        {
            var exit = Engine.Run(window);
            if (failure is not null) throw new InvalidOperationException("Skeletal workspace verification failed", failure);
            Check(exit == 0 && complete, "Native workspace displays the authored skeletal scene");
        }
        finally
        {
            if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Drawn;
            Engine.IsEditorHint = oldHint;
            workspace.RetireJobsAsync().GetAwaiter().GetResult();
        }
        Check(File.ReadAllBytes(IOPath.Combine(project.Root, project.SceneFile)).SequenceEqual(disk), "Unsaved preview authoring preserves the exact scene file");
        Console.WriteLine($"Skeletal workspace {renderer}: initial owner, passive builtins, custom rejection, shared unsaved movement, reentry, actual canvas/window pixels and unchanged disk passed.");
        return 0;

        void Drawn()
        {
            try
            {
                if (++frame == 2)
                {
                    Check(ReferenceEquals(window.Tree!.EditedSceneRoot, viewport.GetChild(0)), "Initial workspace entry registers its existing preview owner");
                    before = OrangeLeft(); Capture("before");
                    var pose = rigInfo.Transform!.Value; pose.Origin.X += 36;
                    agent.Apply("unsaved skeletal position", new SessionTransformEdit(rigInfo.ID, rigInfo.Revision, pose));
                }
                else if (frame == 3)
                {
                    Check(Math.Abs(OrangeLeft() - before - 36) <= 2 && agent.InspectScene().IsDirty,
                        "The native preview follows the shared unsaved skeletal revision");
                    Capture("after");
                    window.RemoveChild(workspace); window.AddChild(workspace);
                    workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                }
                else if (frame == 4)
                {
                    Check(ReferenceEquals(window.Tree!.EditedSceneRoot, viewport.GetChild(0)) && Math.Abs(OrangeLeft() - before - 36) <= 2,
                        "Workspace reentry restores the editor owner and visible gizmos");
                    using var image = window.GetTexture().GetImage()!; image.SavePNG(IOPath.Combine(output, "workspace.png"));
                    complete = true; window.Tree.Quit();
                }
                Check(frame < 12, "Workspace fixture stays within its native frame bound");
            }
            catch (Exception error) { failure = error; window.Tree!.Quit(1); }
        }
        float OrangeLeft()
        {
            using var image = viewport.GetTexture().GetImage()!;
            var left = int.MaxValue; var count = 0;
            for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++)
                { var c = image.GetPixel(x, y); if (c.R > .5f && c.G > .2f && c.B < .2f) { left = Math.Min(left, x); count++; } }
            Check(count > 20, "The authored subviewport contains real skeletal guide pixels");
            return left * viewport.Size.X / (float)image.Width;
        }
        void Capture(string name) { using var image = viewport.GetTexture().GetImage()!; image.SavePNG(IOPath.Combine(output, name + ".png")); }
    }
    private static Unsupported CreateUnsupported() => new();
    private static void WriteRig(SessionProject project, bool custom)
    {
        using var root = new Node { Name = "Scene" };
        var rig = new Skeleton { Name = "Rig", Position = new(96, 72) }; root.AddChild(rig); rig.Owner = root;
        var bone = new Bone { Name = "Arm" }; bone.SetAutocalculateLengthAndAngle(false); bone.SetLength(24);
        rig.AddChild(bone); bone.Owner = root;
        using var stack = new SkeletonModificationStack { Enabled = true };
        using SkeletonModification mod = custom ? new Unsupported { EditorDrawGizmo = true } : new SkeletonModificationLookAt { BoneIndex = 0 };
        stack.AddModification(mod); rig.SetModificationStack(stack);
        ResourceSaver.SaveScene(root, IOPath.Combine(project.Root, project.SceneFile), SaverFlags.BundleResources);
    }
}
