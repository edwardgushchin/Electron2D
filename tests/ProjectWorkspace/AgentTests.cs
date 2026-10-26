using System.Diagnostics;
using System.Text.Json;
using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

internal static class AgentTests
{
    internal static int Run(string output, string backend)
    {
        Directory.CreateDirectory(output);
        var project = SessionProject.Create(IOPath.Combine(output, "Game"), AppContext.BaseDirectory);
        using var session = project.OpenSession(); using var human = session.Connect("acceptance observer");
        var savedSource = File.ReadAllText(IOPath.Combine(project.Root, project.CodeFile)); var savedScene = File.ReadAllBytes(IOPath.Combine(project.Root, project.SceneFile));
        using var regular = new FontFile(); regular.LoadDynamicFont(IOPath.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf"));
        using var mono = new SystemFont { FontNames = ["monospace"] };
        using var window = new Window { Title = "Electron2D real agent acceptance", Size = new(1480, 960) };
        var workspace = new ProjectWorkspace(project, session, AppContext.BaseDirectory, regular, mono); window.AddChild(workspace); workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var panel = Ancestor<AgentPanel>(workspace.FindChild("AgentTask", owned: false)!);
        var code = (CodeEdit)workspace.FindChild("Code", owned: false)!;
        var task = (TextEdit)workspace.FindChild("AgentTask", owned: false)!;
        var conversation = (TextEdit)workspace.FindChild("AgentConversation", owned: false)!;
        var timer = Stopwatch.StartNew(); var frames = 0; var checks = 0; var phase = 0; var manualConflict = false; var actualConflict = false; var readsSeen = 0; var stopSent = false; string? thread = null;
        var trace = new List<object>(); Exception? failure = null; string? queuedClick = null; var clickFrame = 0; SessionJob? firstJob = null, cancelledJob = null;
        void Check(bool valid, string message) { checks++; if (!valid) throw new InvalidOperationException(message); }
        void Click(string name) { queuedClick = name; clickFrame = frames; }
        void SourceComment(string text) { code.SetCaretLine(0); code.SetCaretColumn(0); code.InsertTextAtCaret("// " + text + "\n"); }
        void Phase(int value, string note) { phase = value; trace.Add(new { phase, note, seconds = timer.Elapsed.TotalSeconds, frames }); Console.WriteLine($"Phase {phase}: {note}"); }
        void Frame()
        {
            try
            {
                frames++;
                if (frames % 60 == 0) File.WriteAllText(IOPath.Combine(output, "conversation-live.txt"), conversation.Text);
                if (timer.Elapsed.TotalSeconds > 420) throw new TimeoutException("Real agent acceptance exceeded its seven-minute limit.");
                if (frames == 3)
                {
                    ((TabContainer)workspace.FindChild("InspectorTabs", owned: false)!).CurrentTab = 1;
                    SourceComment("human before agent");
                    Check(human.InspectCode().Text.StartsWith("// human before agent", StringComparison.Ordinal), "Pending native source was not visible to the shared owner.");
                    Phase(1, "prepare native agent panel");
                }
                if (frames == 7) Click("AgentConnect");
                if (frames == 12) { using var screen = window.GetTexture().GetImage()!; screen.SavePNG(IOPath.Combine(output, "agent-start.png")); }
                if (phase == 1 && panel.Executor.Status == SessionAgentStatus.Ready && !((Button)workspace.FindChild("AgentSend", owned: false)!).Disabled)
                {
                    thread = panel.Executor.ThreadID; Check(panel.Executor.IsConnected && !string.IsNullOrEmpty(thread), "Executor did not establish a real conversation.");
                    task.Text = $"Read the current shared context and C# buffer. Change Square's scene position to (80,40), and change its color in SceneLoaded from CornflowerBlue to LimeGreen. Preserve all existing manual comments. Do not save or reload files. Then start one rendered run on backend {backend}, two steps, capture step 2 at 320x180. Poll to terminal status, inspect assertions/diagnostics and retrieve the actual image with job_artifact. Report the snapshot ID and whether it is stale. A user may edit source after your read: on CONFLICT reread and preserve their text. Use only electron2d operations.";
                    Click("AgentSend"); Phase(2, "request unsaved scene/code changes and real capture");
                }
                if (phase == 2)
                {
                    actualConflict |= conversation.Text.Contains("CONFLICT · electron2d/code_edit", StringComparison.Ordinal);
                    var reads = conversation.Text.Split('\n').Count(line => line.Contains("Finished · electron2d/code_read · external:", StringComparison.Ordinal));
                    if (!actualConflict && reads > readsSeen) { readsSeen = reads; SourceComment("human during agent " + reads); manualConflict = true; trace.Add(new { note = "manual edit after agent code read", reads, frames }); }
                    firstJob ??= human.InspectJobs().FirstOrDefault();
                    if (firstJob is { Completion.IsCompleted: true } && panel.Executor.Status == SessionAgentStatus.Ready)
                    {
                        var result = firstJob.Completion.GetAwaiter().GetResult();
                        Check(result.Status == SessionJobStatus.Completed && result.Assertions.Count > 0 && result.Assertions.All(a => a.Passed), "Real agent snapshot did not pass actual game assertions.");
                        Check(actualConflict, "The integrated scenario did not exercise an overlapping stale source conflict.");
                        Check(manualConflict && human.InspectCode().Text.Contains("// human during agent", StringComparison.Ordinal) && human.InspectCode().Text.Contains("// human before agent", StringComparison.Ordinal), "Agent lost manual source edits.");
                        Check(human.InspectCode().Text.Contains("Colors.LimeGreen", StringComparison.Ordinal), "Agent did not change the live source color.");
                        Check(human.InspectScene().Nodes.Single(n => n.Name == "Square").Transform!.Value.Origin == new Vector2(80, 40), "Agent did not change the scene position.");
                        Check(conversation.Text.Contains("Finished · electron2d/job_artifact", StringComparison.Ordinal), "Agent did not retrieve actual rendered evidence.");
                        var artifact = result.Artifacts.Single(a => a.Kind == "image"); using var image = Image.LoadFromFile(artifact.Path);
                        var green = image.GetPixel(80, 40); Check(green.G > .7f && green.R < .4f && green.B < .4f && image.GetPixel(40, 40).G < .7f, "Capture did not render the intended unsaved pose and color.");
                        File.Copy(artifact.Path, IOPath.Combine(output, "agent-game.png"), true);
                        SourceComment("human after first snapshot"); Check(firstJob.IsStale, "Later manual edit did not mark old evidence stale.");
                        using var screen = window.GetTexture().GetImage()!; screen.SavePNG(IOPath.Combine(output, "agent-panel.png"));
                        task.Text = $"Start a rendered run on {backend} with 1000000 steps and 300000 ms deadline. Do not edit or save anything. Keep polling it until I interrupt. This task is a cancellation check.";
                        Click("AgentSend"); Phase(3, "start cancellable actual job");
                    }
                }
                if (phase == 3)
                {
                    cancelledJob ??= human.InspectJobs().FirstOrDefault(j => j != firstJob);
                    if (cancelledJob is not null && !stopSent && panel.Executor.Status == SessionAgentStatus.Running)
                    { SourceComment("human survives cancellation"); Click("AgentStop"); stopSent = true; Phase(4, "interrupt executor and its owned job"); }
                }
                if (phase == 4 && cancelledJob is { Completion.IsCompleted: true } && panel.Executor.Status == SessionAgentStatus.Ready)
                {
                    Check(cancelledJob.Completion.GetAwaiter().GetResult().Status == SessionJobStatus.Cancelled, "Stopping the agent left its job running or successful.");
                    Check(conversation.Text.Contains("Interrupted:", StringComparison.Ordinal), "Executor did not acknowledge turn interruption.");
                    Check(human.InspectCode().Text.Contains("human survives cancellation", StringComparison.Ordinal), "Cancellation lost a later manual edit.");
                    Click("AgentConnect"); Phase(5, "disconnect the real executor");
                }
                if (phase == 5 && panel.Executor.Status == SessionAgentStatus.Disconnected) { Click("AgentConnect"); Phase(6, "reconnect the same conversation"); }
                if (phase == 6 && panel.Executor.Status == SessionAgentStatus.Ready && !((Button)workspace.FindChild("AgentSend", owned: false)!).Disabled)
                {
                    Check(panel.Executor.ThreadID == thread, "Reconnect created a different conversation.");
                    task.Text = "Read shared context and the first 200 characters of the code. Confirm the current selection and manual comment 'human survives cancellation'. Do not edit, save, reload or start jobs. Reply RECONNECTED after you actually read them.";
                    Click("AgentSend"); Phase(7, "verify shared operations after reconnect");
                }
                if (phase == 7 && panel.Executor.Status == SessionAgentStatus.Ready && conversation.Text.Contains("RECONNECTED", StringComparison.Ordinal))
                {
                    Check(human.InspectScene().IsDirty && human.InspectCode().IsDirty, "Agent acceptance implicitly saved documents.");
                    Check(File.ReadAllText(IOPath.Combine(project.Root, project.CodeFile)) == savedSource && File.ReadAllBytes(IOPath.Combine(project.Root, project.SceneFile)).SequenceEqual(savedScene), "Agent wrote authoring files without publication rights.");
                    var tree = (Electron2D.Tree)workspace.FindChild("SceneTree", owned: false)!;
                    Check(tree.GetSelected()?.GetMetadata<Guid>(0) == human.InspectSelection().NodeID, "Native selection differs from shared context.");
                    File.WriteAllText(IOPath.Combine(output, "conversation.txt"), conversation.Text);
                    File.WriteAllText(IOPath.Combine(output, "receipt.json"), JsonSerializer.Serialize(new { checks, frames, seconds = timer.Elapsed.TotalSeconds, executor = "codex-cli 0.163.0", model = panel.Executor.Model, thread, backend, firstJob = firstJob!.ID, firstSnapshot = firstJob.SnapshotID, snapshotSHA256 = firstJob.Snapshot?.SHA256, cancelledJob = cancelledJob!.ID, trace }, new JsonSerializerOptions { WriteIndented = true }));
                    workspace.PrepareClose(); window.Tree!.Quit(); Phase(8, "all integrated assertions passed");
                }
                if (panel.Executor.Status == SessionAgentStatus.Failed) throw new InvalidOperationException("Real executor failed: " + string.Join(" | ", panel.Executor.DrainEvents().Select(e => e.Text)) + "\n" + conversation.Text[^Math.Min(conversation.Text.Length, 4096)..]);
            }
            catch (Exception error) { failure = error; File.WriteAllText(IOPath.Combine(output, "conversation-failed.txt"), conversation.Text); window.Tree!.Quit(1); }
        }
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        window.Ready += _ => { RenderingServer.SetDefaultClearColor(Color.FromHTML("#241B2C")); RenderingServer.FramePostDraw += Frame; };
        try
        {
            var exit = Engine.Run((_, _) => { if (queuedClick is not { } target || frames < clickFrame + 2) return;
                var button = (Button)workspace.FindChild(target, owned: false)!; if (button.Disabled) return;
                queuedClick = null; var point = button.GlobalPosition + button.Size * .5f;
                using var down = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = point, GlobalPosition = point };
                using var up = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = point, GlobalPosition = point };
                Input.ParseInputEvent(down); Input.ParseInputEvent(up); }, window);
            if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            Check(exit == 0 && phase == 8, "Native acceptance exited without completing the scenario.");
        }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Frame; workspace.Dispose(); workspace.RetireJobsAsync().GetAwaiter().GetResult(); }
        Console.WriteLine($"PASS {checks} real-agent native assertions, {frames} {backend} frames, {timer.Elapsed.TotalSeconds:F1}s."); return 0;
    }
    private static T Ancestor<T>(Node node) where T : Node { for (Node? current = node; current is not null; current = current.Parent) if (current is T found) return found; throw new InvalidOperationException("Missing panel."); }
}
