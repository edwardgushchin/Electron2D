using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

if (args is ["--application", var appOutput, var appBackend]) return ApplicationTests.Run(IOPath.GetFullPath(appOutput), appBackend);
if (args is ["--agent", var agentOutput, var agentBackend]) return AgentTests.Run(IOPath.GetFullPath(agentOutput), agentBackend);
if (args is ["--fault", var faultOutput]) return FaultedWorkspaceChecks.Run(IOPath.GetFullPath(faultOutput));
ProjectionTests.Run();

var output = IOPath.GetFullPath(args[0]); Directory.CreateDirectory(output);
var backend = args.Length > 1 ? args[1] : "gpu";
var project = SessionProject.Create(IOPath.Combine(output, "Game"), AppContext.BaseDirectory);
var path = IOPath.Combine(project.Root, project.CodeFile); var original = File.ReadAllText(path).Replace("\n", "\r\n", StringComparison.Ordinal); File.WriteAllText(path, original);
using var session = project.OpenSession(); using var agent = session.Connect("programmatic client");
using var regular = new FontFile(); regular.LoadDynamicFont(IOPath.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf"));
using var mono = new SystemFont { FontNames = ["monospace"] };
using var window = new Window { Title = "Electron2D project workflow verification", Size = new(1280, 900) };
var workspace = new ProjectWorkspace(project, session, AppContext.BaseDirectory, regular, mono); window.AddChild(workspace); workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
var code = (CodeEdit)workspace.FindChild("Code", owned: false)!; var frames = 0; var checks = 0; Exception? failure = null;
Guid move = default; string? manual = null; Vector2? click = null; Key? historyKey = null; var finishedJobFrame = 0; var clearedFrame = 0;
void Check(bool valid, string message) { checks++; if (!valid) throw new InvalidOperationException(message); }
void Frame()
{
    try
    {
        frames++;
        if (frames == 2)
        {
            Check(!agent.InspectCode().IsDirty, "Opening the code view changed CRLF source.");
            code.GrabFocus(); code.SetCaretLine(0); code.SetCaretColumn(0); code.InsertTextAtCaret("// manual input\n");
            // TextChanged is still queued here. The operation ingress must see the user's visible text first.
            manual = agent.InspectCode().Text;
            Check(manual.StartsWith("// manual input\r\n", StringComparison.Ordinal), "Deferred text signal lost pending manual input or CRLF policy.");
            var square = agent.InspectScene().Nodes.Single(n => n.Name == "Square"); var pose = square.Transform!.Value; pose.Origin.X = 90;
            ((Electron2D.Tree)workspace.FindChild("SceneTree", owned: false)!).GetRoot()!.Collapsed = true;
            move = agent.Apply("agent moves square", new SessionTransformEdit(square.ID, square.Revision, pose)).OperationID;
            Check(agent.InspectCode().Text == manual, "Programmatic scene edit overwrote manual source.");
        }
        if (frames == 5)
        {
            Check(code.Text.StartsWith("// manual input\n", StringComparison.Ordinal), "Code view did not retain manual input.");
            Check(code.GetCaretLine() == 1, "Unrelated scene refresh reset the source caret.");
            var treeRoot = ((Electron2D.Tree)workspace.FindChild("SceneTree", owned: false)!).GetRoot()!;
            Check(treeRoot.Collapsed, "Scene refresh reset an unaffected collapsed tree row."); treeRoot.Collapsed = false;
            agent.Undo(move); Check(agent.InspectCode().Text == manual, "Undo overwrote manual source."); agent.Redo(move);
            using var image = window.GetTexture().GetImage()!; image.SavePNG(IOPath.Combine(output, "workspace.png"));
            var left = (int)code.GlobalPosition.X + 4; var bottom = (int)(code.GlobalPosition.Y + code.Size.Y) + 14;
            var clear = Color.FromHTML("#241B2C"); var outside = true;
            for (var y = bottom; y < Math.Min(bottom + 14, window.Size.Y); y++)
                for (var x = left; x < left + 36; x++) { var pixel = image.GetPixel(x, y); outside &= Math.Abs(pixel.R - clear.R) < .02f && Math.Abs(pixel.G - clear.G) < .02f && Math.Abs(pixel.B - clear.B) < .02f; }
            Check(outside, "Code gutters drew below the source control's visible region.");
            window.Size = new(1450, 940);
        }
        if (frames == 8)
        {
            using var image = window.GetTexture().GetImage()!; image.SavePNG(IOPath.Combine(output, "workspace-resized.png"));
            Check(workspace.Size.X >= 1400, "Workspace did not follow the resized window.");
            var tree = (Electron2D.Tree)workspace.FindChild("SceneTree", owned: false)!; var square = tree.GetRoot()!.GetFirstChild()!; var area = tree.GetItemAreaRect(square);
            var point = tree.GlobalPosition + area.Position + area.Size * .5f;
            click = point;
        }
        if (frames == 10)
        {
            var x = (SpinBox)workspace.FindChild("PositionX", owned: false)!; x.Value = 110;
            Check(agent.InspectScene().Nodes.Single(n => n.Name == "Square").Transform!.Value.Origin.X == 110, "Inspector did not mutate the shared scene.");
            code.GrabFocus(); historyKey = Key.Z;
        }
        if (frames == 12)
        {
            Check(agent.InspectScene().Nodes.Single(n => n.Name == "Square").Transform!.Value.Origin.X == 90, "Ctrl-Z did not undo the shared inspector operation.");
            Check(agent.InspectCode().Text == manual, "Keyboard undo overwrote unrelated source."); historyKey = Key.Y;
        }
        if (frames == 14)
        {
            Check(agent.InspectScene().Nodes.Single(n => n.Name == "Square").Transform!.Value.Origin.X == 110, "Ctrl-Y did not redo shared history.");
            ((SpinBox)workspace.FindChild("Frames", owned: false)!).Value = 2;
            ((OptionButton)workspace.FindChild("RunMode", owned: false)!).Select(backend == "gpu" ? 1 : 2);
            var run = (Button)workspace.FindChild("Run", owned: false)!; click = run.GlobalPosition + run.Size * .5f;
        }
        if (frames == 18)
        {
            var node = agent.InspectScene().Nodes.Single(n => n.Name == "Square"); var pose = node.Transform!.Value; pose.Origin.X = 120;
            agent.Apply("later scene edit while building", new SessionTransformEdit(node.ID, node.Revision, pose));
        }
        var captured = (TextureRect)workspace.FindChild("Capture", owned: false)!;
        var jobStatus = (Label)workspace.FindChild("JobStatus", owned: false)!;
        if (frames > 18 && finishedJobFrame == 0 && captured.Texture is not null)
        {
            Check(jobStatus.Text.StartsWith("Completed", StringComparison.Ordinal), "Editor displayed an uncompleted result as successful.");
            Check(((Label)workspace.FindChild("CaptureStatus", owned: false)!).Text.Contains("stale", StringComparison.Ordinal), "Capture missed its stale attribution.");
            using var rendered = ((ImageTexture)captured.Texture).GetImage()!;
            Check(rendered.GetPixel(110, 40).B > .6f && rendered.GetPixel(120, 40).B < .6f, "Editor capture did not use the exact earlier snapshot.");
            finishedJobFrame = frames;
        }
        if (finishedJobFrame > 0 && frames == finishedJobFrame + 3)
        {
            using var image = window.GetTexture().GetImage()!; image.SavePNG(IOPath.Combine(output, "workspace-result.png"));
            var clearButton = (Button)workspace.FindChild("ClearResults", owned: false)!; click = clearButton.GlobalPosition + clearButton.Size * .5f; clearedFrame = frames;
        }
        if (clearedFrame > 0 && frames == clearedFrame + 3)
        {
            Check(captured.Texture is null && !Directory.EnumerateDirectories(IOPath.Combine(project.Root, ".electron2d", "jobs")).Any(), "Clear results retained a native texture or generated payload.");
            workspace.PrepareClose(); window.Tree!.Quit();
        }
        if (frames > 5000) throw new TimeoutException("Workspace did not reach completion.");
    }
    catch (Exception error) { failure = error; window.Tree!.Quit(1); }
}
window.Ready += _ => { RenderingServer.SetDefaultClearColor(Color.FromHTML("#241B2C")); RenderingServer.FramePostDraw += Frame; };
try
{
    var exit = Engine.Run((_, _) =>
    {
        if (historyKey is { } key)
        {
            historyKey = null;
            using var input = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true, ControlPressed = true };
            Input.ParseInputEvent(input);
        }
        if (click is not { } point) return;
        click = null;
        using var down = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = point, GlobalPosition = point };
        using var up = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = point, GlobalPosition = point };
        Input.ParseInputEvent(down); Input.ParseInputEvent(up);
    }, window);
    if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    if (exit != 0) throw new InvalidOperationException("Native editor failed.");
}
finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Frame; }
workspace.RetireJobsAsync().GetAwaiter().GetResult();
Check(agent.InspectScene().IsDirty && agent.InspectCode().IsDirty, "Editor verification implicitly saved a document.");
Console.WriteLine($"PASS {checks} workspace assertions, {frames} real {backend} frames, native images, unsaved shared edits, pending-input ordering and CRLF preservation.");
return 0;
