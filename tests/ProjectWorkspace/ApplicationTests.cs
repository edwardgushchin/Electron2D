using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

internal static class ApplicationTests
{
    internal static int Run(string output, string backend)
    {
        Directory.CreateDirectory(output); var firstRoot = IOPath.Combine(output, "First");
        var second = SessionProject.Create(IOPath.Combine(output, "Second"), AppContext.BaseDirectory);
        using var mark = ResourceLoader.Load<ImageTexture>(Asset("mark-dark.svg")); using var sparkle = ResourceLoader.Load<ImageTexture>(Asset("sparkle.svg")); using var icon = ResourceLoader.Load<ImageTexture>(Asset("app-icon.svg"));
        using var semibold = new FontFile(); semibold.LoadDynamicFont(Asset("IBMPlexSans-SemiBold.ttf"));
        using var regular = new FontFile(); regular.LoadDynamicFont(Asset("IBMPlexSans-Regular.ttf"));
        using var application = new EditorApplication(mark, sparkle, icon, semibold, regular, null);
        var window = application.Window; window.Size = new(1400, 980);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        var directory = (LineEdit)window.FindChild("ProjectDirectory", owned: false)!; Vector2? click = null; var frame = 0; var checks = 0; Exception? failure = null; CodeEdit? source = null;
        void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
        void Click(string name) { var button = (Button)window.FindChild(name, owned: false)!; click = button.GlobalPosition + button.Size * .5f; }
        void Drawn()
        {
            try
            {
                frame++;
                if (frame == 3)
                {
                    Check(window.FindChild("Workspace", owned: false) is null, "Startup unexpectedly acquired a project.");
                    using var image = window.GetTexture().GetImage()!; image.SavePNG(IOPath.Combine(output, "home.png"));
                    directory.Text = firstRoot; Click("CreateProject");
                }
                if (frame == 7)
                {
                    source = (CodeEdit)window.FindChild("Code", owned: false)!; Check(source is not null, "Create did not open a shared workspace.");
                    source!.InsertTextAtCaret("// unsaved application view\n");
                    directory.Text = IOPath.Combine(output, "missing"); Click("OpenProject");
                }
                if (frame == 11)
                {
                    Check(ReferenceEquals(source, window.FindChild("Code", owned: false)) && source!.Text.Contains("unsaved application view", StringComparison.Ordinal), "Failed open replaced the live document or its pending text.");
                    using var image = window.GetTexture().GetImage()!; image.SavePNG(IOPath.Combine(output, "open-error-preserves-view.png"));
                    directory.Text = firstRoot; Click("OpenProject");
                }
                if (frame == 15)
                {
                    Check(ReferenceEquals(source, window.FindChild("Code", owned: false)), "Reopening the current project replaced its view.");
                    directory.Text = second.Root; Click("OpenProject");
                }
                if (frame == 20)
                {
                    Check(!ReferenceEquals(source, window.FindChild("Code", owned: false)), "Opening another project retained the old view.");
                    using var recovered = SessionProject.Open(firstRoot).OpenSession(); using var client = recovered.Connect("verification");
                    Check(client.InspectCode().Text.Contains("unsaved application view", StringComparison.Ordinal) && client.InspectCode().IsDirty, "Project switch did not preserve committed unsaved source/release its owner.");
                    using var image = window.GetTexture().GetImage()!; image.SavePNG(IOPath.Combine(output, "second-project.png"));
                    window.Tree!.Quit();
                }
            }
            catch (Exception error) { failure = error; window.Tree!.Quit(1); }
        }
        window.Ready += _ => RenderingServer.FramePostDraw += Drawn;
        try
        {
            var exit = Engine.Run((_, _) =>
            {
                if (click is not { } point) return; click = null;
                using var down = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = point, GlobalPosition = point };
                using var up = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = point, GlobalPosition = point };
                Input.ParseInputEvent(down); Input.ParseInputEvent(up);
            }, window);
            if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            Check(exit == 0, "Application run failed.");
        }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Drawn; application.Dispose(); application.RetireJobsAsync().GetAwaiter().GetResult(); }
        using var reopened = second.OpenSession(); Check(reopened.ProjectRoot == second.Root, "Closing did not release the current owner.");
        Console.WriteLine($"PASS {checks} application assertions, {frame} native {backend} frames, create/open/switch/error/recovery."); return 0;
    }
    private static string Asset(string file) => IOPath.Combine(AppContext.BaseDirectory, "Assets", file);
}
