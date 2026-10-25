using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

if (args is ["--owner-child", var root, var marker]) return JobContracts.OwnerChild(root, marker);
if (args is ["--input-contracts", var inputEvidence]) return JobContracts.Run(IOPath.GetFullPath(inputEvidence), true);
if (args is ["--contracts", var evidence]) return JobContracts.Run(IOPath.GetFullPath(evidence));

var output = args.Length > 0 ? IOPath.GetFullPath(args[0]) : IOPath.Combine(IOPath.GetTempPath(), "electron2d-jobs-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(output);
var project = SessionProject.Create(IOPath.Combine(output, "Game"), AppContext.BaseDirectory);
using var session = project.OpenSession(); using var human = session.Connect("human"); using var agent = session.Connect("agent");
var code = human.InspectCode(); human.Apply("unsaved green", new SessionCodeEdit(code.ID, code.Revision, code.Text.Replace("Colors.CornflowerBlue", "Colors.LimeGreen", StringComparison.Ordinal)));
var scene = agent.InspectScene(); var square = scene.Nodes.Single(n => n.Name == "Square"); var pose = square.Transform!.Value; pose.Origin.X = 80;
agent.Apply("unsaved pose", new SessionTransformEdit(square.ID, square.Revision, pose));
using (var preview = human.CreatePreview(human.InspectScene().Revision))
    if (((Entity)preview.GetNode("Square")).Position.X != 80) throw new InvalidOperationException("Preview does not show shared unsaved pose.");
scene = human.InspectScene(); code = human.InspectCode();
var backend = args.Skip(1).FirstOrDefault() ?? "headless";
var options = new SessionJobOptions(SessionJobKind.Run, project.ProjectFile, AppContext.BaseDirectory,
    backend == "headless" ? SessionRunMode.Headless : SessionRunMode.Rendered, Steps: 2, Backend: backend == "headless" ? "gpu" : backend,
    CaptureSteps: backend == "headless" ? [] : [2]);
var job = human.StartJob(options, scene.Revision, code.Revision);
// The authoring owner stays available while a different process builds the captured input.
square = human.InspectScene().Nodes.Single(n => n.Name == "Square"); pose = square.Transform!.Value; pose.Origin.X = 100;
human.Apply("later manual pose", new SessionTransformEdit(square.ID, square.Revision, pose));
if (!job.IsStale) throw new InvalidOperationException("Running results were not marked stale after a later manual edit.");
var result = job.Completion.GetAwaiter().GetResult();
Console.WriteLine($"{result.Status} exit={result.ExitCode} report={result.ReportPath}");
foreach (var diagnostic in result.Diagnostics) Console.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
if (result.Status != SessionJobStatus.Completed) return 1;
if (result.Snapshot?.SceneRevision != scene.Revision || !human.InspectScene().IsDirty || !human.InspectCode().IsDirty) throw new InvalidOperationException("Snapshot changed persisted revisions or lost unsaved inputs.");
if (result.Observations.Count != 2 || result.Observations.Any(o => o.Name != "square-x" || o.Number != 80) || result.Assertions.Any(a => !a.Passed)) throw new InvalidOperationException("Typed results do not describe the exact unsaved pose.");
if (backend != "headless")
{
    using var captured = Image.LoadFromFile(result.Artifacts.Single(a => a.Kind == "image").Path);
    var pixel = captured.GetPixel(80, 40); var old = captured.GetPixel(40, 40);
    if (pixel.G < .7f || pixel.R > .4f || old.G > .7f) throw new InvalidOperationException($"Capture did not use the unsaved code+scene: {pixel}, old {old}.");
}
Console.WriteLine("PASS unsaved snapshot, shared preview, fresh process, actual requested output, editable owner, stale result.");
job.DisposeAsync().AsTask().GetAwaiter().GetResult();
return 0;
