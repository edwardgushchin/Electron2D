using System.Diagnostics;
using System.Reflection;
using System.Text;
using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

internal static class Program
{
    private static int _checks;
    private static void Check(bool condition, string message) { _checks++; if (!condition) throw new InvalidOperationException(message); }
    private static T Reject<T>(Action action) where T : Exception
    {
        _checks++;
        try { action(); } catch (T error) { return error; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
    private static Transform Pose(float x, float y = 0) => new(1, 0, 0, 1, x, y);
    private static string ScenePath(string root) => IOPath.Combine(root, "scene.e2dscene");
    private static string CodePath(string root) => IOPath.Combine(root, "Game.cs");
    private static ProjectSession Open(string root) => ProjectSession.Open(root, "scene.e2dscene", "Game.cs");
    private static SessionNode Find(SessionClient client, string name) => client.InspectScene().Nodes.Single(n => n.Name == name);
    private static Process Child(params string[] args)
    {
        var info = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        if (IOPath.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") info.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        foreach (var arg in args) info.ArgumentList.Add(arg);
        return Process.Start(info)!;
    }
    private static void Exit(Process child, int expected)
    {
        using (child)
        {
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Child owner did not finish."); }
            var output = child.StandardOutput.ReadToEnd() + child.StandardError.ReadToEnd();
            Check(child.ExitCode == expected, $"Child exit {child.ExitCode}, expected {expected}: {output}");
        }
    }
    private static string Fixture(string parent, string name, bool callbacks = false)
    {
        var root = IOPath.Combine(parent, name); Directory.CreateDirectory(root);
        using var scene = callbacks ? new CallbackEntity { Name = "Root" } : new Entity { Name = "Root" };
        var a = new Entity { Name = "A" }; var b = new Entity { Name = "B" }; var c = new Entity { Name = "C" };
        scene.AddChild(a); scene.AddChild(b); scene.AddChild(c); a.Owner = scene; b.Owner = scene; c.Owner = scene;
        ResourceSaver.SaveScene(scene, ScenePath(root), SaverFlags.BundleResources);
        File.WriteAllBytes(CodePath(root), [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes("// original\r\n")]);
        return root;
    }
    private static CallbackEntity CreateCallback() => new();
    public static int Main(string[] args)
    {
        ResourceFileTypes.RegisterNode("session.test.CallbackEntity", CreateCallback);
        if (args.Length > 0) return ChildMain(args);
        var parent = IOPath.Combine(IOPath.GetTempPath(), "electron2d-session-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(parent);
        try
        {
            ConcurrentEdits(Fixture(parent, "edits"));
            Hierarchy(Fixture(parent, "hierarchy"));
            AffineRecovery(Fixture(parent, "affine"));
            ExternalAndFailures(Fixture(parent, "external"));
            Callbacks(Fixture(parent, "callbacks", true));
            Processes(Fixture(parent, "processes"));
            Boundaries(Fixture(parent, "boundaries"));
            SaveCrash(Fixture(parent, "save-crash", true));
            HistoryBounds(Fixture(parent, "history-bounds"));
            RecoveryValidation(Fixture(parent, "invalid-recovery"));
            Console.WriteLine($"PASS project session: {_checks} checks; public runtime only; no native window or SceneTree."); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally { Directory.Delete(parent, true); }
    }
    private static int ChildMain(string[] args)
    {
        if (args[0] == "excluded") { try { using var owner = Open(args[1]); return 2; } catch (IOException) { return 0; } }
        using var session = Open(args[1]); using var client = session.Connect("fresh process");
        if (args[0] == "recover")
        {
            var a = Find(client, "A"); Check(a.Transform == Pose(73), "Crash recovery lost scene.");
            Check(client.InspectCode().Text == "// crash\r\n", "Crash recovery lost source.");
            var operation = Guid.Parse(args[2]); Check(client.InspectHistory().Single().ID == operation, "Recovered history is not discoverable."); client.Undo(operation);
            Check(Find(client, "A").Transform == Transform.Identity, "Recovered grouped undo failed.");
            Check(client.InspectCode().Text == "// original\r\n", "Recovered grouped code undo failed.");
            client.Redo(operation); client.Save(SessionDocumentKind.Scene, client.InspectScene().Revision);
            client.Save(SessionDocumentKind.Code, client.InspectCode().Revision); return 0;
        }
        if (args[0] == "save-crash")
        {
            var a = Find(client, "A"); client.Apply("before interrupted save", new SessionTransformEdit(a.ID, a.Revision, Pose(91)));
            CallbackEntity.BlockPost = true; client.Save(SessionDocumentKind.Scene, client.InspectScene().Revision); return 5;
        }
        if (args[0] == "save-recover")
        {
            var scene = client.InspectScene();
            Check(scene.IsDirty && scene.HasExternalChange && Find(client, "A").Transform == Pose(91), "Interrupted save lost recoverable state.");
            client.Refresh(SessionDocumentKind.Scene, scene.Revision, SessionExternalResolution.KeepLive);
            Check(Find(client, "A").Transform == Pose(91) && !client.InspectScene().HasExternalChange, "Interrupted-save reconciliation lost live data.");
            client.Save(SessionDocumentKind.Scene, client.InspectScene().Revision);
            Check(!client.InspectScene().IsDirty, "Interrupted-save continuation failed."); return 0;
        }
        if (args[0] == "disk")
        {
            Check(!client.InspectScene().IsDirty && !client.InspectCode().IsDirty, "Fresh-process save is dirty.");
            Check(File.ReadAllBytes(CodePath(args[1])).AsSpan().StartsWith(Encoding.UTF8.Preamble), "Save lost original UTF-8 BOM.");
            using var packed = ResourceLoader.Load<PackedScene>(ScenePath(args[1]), ResourceLoader.CacheMode.IgnoreDeep);
            using var root = packed.Instantiate(); Check(((Entity)root.GetChildren()[0]).Transform == Pose(73), "Disk scene was not saved."); return 0;
        }
        if (args[0] == "crash")
        {
            var a = Find(client, "A"); var code = client.InspectCode();
            var result = client.Apply("recoverable group", new SessionTransformEdit(a.ID, a.Revision, Pose(73)), new SessionCodeEdit(code.ID, code.Revision, "// crash\r\n"));
            Console.WriteLine(result.OperationID); Console.Out.Flush();
            Thread.Sleep(Timeout.Infinite); return 3;
        }
        return 4;
    }
    private static void ConcurrentEdits(string root)
    {
        Guid sceneID, codeID, aID; ulong finalRevision;
        using (var session = Open(root))
        {
            using var human = session.Connect("human"); using var agent = session.Connect("agent");
            var a = Find(agent, "A"); var b = Find(agent, "B"); var code = human.InspectCode();
            sceneID = human.InspectScene().ID; codeID = code.ID; aID = a.ID;
            var events = new List<string>(); session.Changed += r => events.Add(r.Origin);
            var manual = human.Apply("manual source", new SessionCodeEdit(code.ID, code.Revision, "// unsaved human\r\n"));
            var change = agent.Apply("move A", new SessionTransformEdit(a.ID, a.Revision, Pose(10, 2)));
            Check(Find(human, "A").Transform == Pose(10, 2), "Clients do not share scene state.");
            Check(agent.InspectCode().Text == "// unsaved human\r\n", "Agent lost unsaved manual buffer.");
            Check(File.ReadAllText(CodePath(root)) == "// original\r\n", "Apply implicitly saved code.");
            var conflict = Reject<SessionConflictException>(() => agent.Apply("stale move", new SessionTransformEdit(a.ID, a.Revision, Pose(20))));
            Check(conflict.ObjectID == a.ID && conflict.ActualRevision > conflict.ExpectedRevision, "Conflict lacks revision identity.");
            Reject<SessionConflictException>(() => agent.Apply("stale buffer", new SessionCodeEdit(code.ID, code.Revision, "lost human")));
            human.Apply("unrelated object", new SessionTransformEdit(b.ID, b.Revision, Pose(5)));
            var undo = human.Undo(change.OperationID);
            Check(Find(agent, "A").Transform == Transform.Identity && Find(agent, "B").Transform == Pose(5), "Selective undo changed unrelated object.");
            Check(agent.InspectCode().Text == "// unsaved human\r\n", "Selective undo overwrote manual code.");
            Check(undo.SceneRevision > change.SceneRevision, "Undo revision went backward.");
            agent.Redo(change.OperationID); Check(Find(human, "A").Transform == Pose(10, 2), "Redo failed.");
            a = Find(human, "A"); human.Apply("overlap", new SessionTransformEdit(a.ID, a.Revision, Pose(30)));
            Reject<SessionConflictException>(() => agent.Undo(change.OperationID));
            Check(events[0] == "human" && events[1] == "agent", "Origin/event order failed.");
            var before = human.InspectScene(); var beforeCode = human.InspectCode();
            Reject<ArgumentOutOfRangeException>(() => agent.Apply("invalid group", new SessionCodeEdit(beforeCode.ID, beforeCode.Revision, "bad"), new SessionTransformEdit(a.ID, Find(human, "A").Revision, Pose(float.NaN))));
            Check(human.InspectScene().Revision == before.Revision && human.InspectCode().Text == beforeCode.Text, "Failed group partly published.");
            var current = Find(human, "A"); var noOp = human.Apply("equal", new SessionTransformEdit(current.ID, current.Revision, current.Transform!.Value));
            Check(noOp.OperationID == Guid.Empty && human.InspectScene().Revision == before.Revision, "No-op changed history/revision.");
            finalRevision = human.InspectScene().Revision;
            Check(human.InspectCode().IsDirty && human.InspectScene().IsDirty, "Dirty state missing.");
            Check(manual.CodeRevision < finalRevision, "Expected shared monotonic clock.");
        }
        using (var session = Open(root))
        {
            using var client = session.Connect("reopen");
            Check(client.InspectScene().ID == sceneID && client.InspectCode().ID == codeID && Find(client, "A").ID == aID, "Recovery changed stable identities.");
            Check(client.InspectScene().Revision == finalRevision && client.InspectCode().Text == "// unsaved human\r\n", "Recovery lost revisions or unsaved buffer.");
        }
    }
    private static void AffineRecovery(string root)
    {
        var affine = new Transform(.37f, new Vector2(-2, 3), .23f, new Vector2(4, 5)); Guid id;
        using (var owner = Open(root))
        {
            using var client = owner.Connect("affine"); var a = Find(client, "A"); id = a.ID;
            client.Apply("skew/reflection", new SessionTransformEdit(a.ID, a.Revision, affine));
            var b = Find(client, "B"); client.Apply("next transaction", new SessionTransformEdit(b.ID, b.Revision, Pose(1)));
            Check(Find(client, "A").Transform == affine, "Staging rounded another object's exact affine pose.");
            client.Save(SessionDocumentKind.Scene, client.InspectScene().Revision);
            Check(!client.InspectScene().IsDirty, "Nontrivial saved affine scene remained dirty.");
        }
        using (var owner = Open(root))
        {
            using var client = owner.Connect("affine recovered"); var a = Find(client, "A");
            Check(a.ID == id && a.Transform == affine, "Recovery lost the exact affine overlay.");
            client.Apply("singular", new SessionTransformEdit(a.ID, a.Revision, default));
            var b = Find(client, "B"); client.Apply("after singular", new SessionTransformEdit(b.ID, b.Revision, Pose(2)));
            Check(Find(client, "A").Transform == default(Transform), "Finite singular authoring pose did not survive staging.");
        }
    }
    private static void Hierarchy(string root)
    {
        using var owner = Open(root); using var client = owner.Connect("hierarchy");
        var a = Find(client, "A"); var b = Find(client, "B");
        var rename = client.Apply("rename", new SessionRenameEdit(a.ID, a.Revision, "Renamed"));
        Check(Find(client, "Renamed").ID == a.ID, "Rename changed identity.");
        var move = client.Apply("reparent", new SessionReparentEdit(a.ID, client.InspectScene().Revision, b.ID));
        Check(Find(client, "Renamed").ParentID == b.ID, "Reparent failed.");
        client.Undo(move.OperationID); Check(Find(client, "Renamed").ParentID == b.ParentID, "Hierarchy undo failed.");
        client.Redo(move.OperationID); Check(Find(client, "Renamed").ParentID == b.ID, "Hierarchy redo failed.");
        Reject<SessionConflictException>(() => client.Undo(rename.OperationID));
        var before = client.InspectScene();
        Reject<InvalidOperationException>(() => client.Apply("cycle", new SessionReparentEdit(b.ID, before.Revision, a.ID)));
        Check(client.InspectScene().Revision == before.Revision, "Cycle partly published.");
        var c = Find(client, "C"); b = Find(client, "B");
        var swap = client.Apply("swap sibling names", new SessionRenameEdit(b.ID, b.Revision, "Temporary"), new SessionRenameEdit(c.ID, c.Revision, "B"), new SessionRenameEdit(b.ID, b.Revision, "C"));
        client.Undo(swap.OperationID); Check(Find(client, "B").ID == b.ID && Find(client, "C").ID == c.ID, "Grouped rename undo failed.");
        client.Redo(swap.OperationID); Check(Find(client, "C").ID == b.ID && Find(client, "B").ID == c.ID, "Grouped rename redo failed.");
        client.Undo(swap.OperationID);
        Reject<ArgumentException>(() => client.Apply("duplicate name", new SessionRenameEdit(b.ID, Find(client, "B").Revision, "C")));
        Reject<ArgumentException>(() => client.Apply("invalid name", new SessionRenameEdit(a.ID, Find(client, "Renamed").Revision, "bad/name")));
    }
    private static void ExternalAndFailures(string root)
    {
        using var owner = Open(root); using var client = owner.Connect("persistence");
        var code = client.InspectCode(); client.Apply("unsaved", new SessionCodeEdit(code.ID, code.Revision, "// live\r\n"));
        File.WriteAllText(CodePath(root), "// external\n"); code = client.InspectCode();
        Check(code.HasExternalChange, "External source change missing.");
        Reject<SessionConflictException>(() => client.Save(SessionDocumentKind.Code, code.Revision));
        Reject<SessionConflictException>(() => client.Refresh(SessionDocumentKind.Code, code.Revision));
        Check(client.InspectCode().Text == "// live\r\n" && File.ReadAllText(CodePath(root)) == "// external\n", "Conflict lost a side.");
        client.Refresh(SessionDocumentKind.Code, code.Revision, SessionExternalResolution.KeepLive);
        code = client.InspectCode(); Check(code.Text == "// live\r\n" && code.IsDirty && !code.HasExternalChange, "KeepLive lost buffer or failed to acknowledge disk.");
        client.Refresh(SessionDocumentKind.Code, code.Revision, SessionExternalResolution.UseDisk);
        code = client.InspectCode(); Check(code.Text == "// external\n" && !code.IsDirty, "Explicit reconciliation failed.");
        File.WriteAllText(CodePath(root), "// clean refresh\n"); client.Refresh(SessionDocumentKind.Code, code.Revision);
        code = client.InspectCode(); client.Apply("save source", new SessionCodeEdit(code.ID, code.Revision, "// saved\n"));
        client.Save(SessionDocumentKind.Code, client.InspectCode().Revision);
        Check(File.ReadAllText(CodePath(root)) == "// saved\n" && !client.InspectCode().IsDirty, "Code save failed.");
        var a = Find(client, "A"); client.Apply("save scene", new SessionTransformEdit(a.ID, a.Revision, Pose(9)));
        client.Save(SessionDocumentKind.Scene, client.InspectScene().Revision); Check(!client.InspectScene().IsDirty, "Saved scene is still dirty.");
        var statePath = IOPath.Combine(root, ".electron2d/session/state.json"); var backup = statePath + ".held";
        File.Move(statePath, backup); Directory.CreateDirectory(statePath);
        try
        {
            code = client.InspectCode(); Reject<IOException>(() => client.Apply("failed recovery", new SessionCodeEdit(code.ID, code.Revision, "lost")));
            Check(client.InspectCode().Text == "// saved\n" && client.InspectCode().Revision == code.Revision, "Failed recovery write published memory.");
            var node = Find(client, "A");
            Reject<IOException>(() => client.Apply("failed scene recovery", new SessionTransformEdit(node.ID, node.Revision, Pose(122))));
            Check(Find(client, "A").Transform == Pose(9) && Find(client, "A").Revision == node.Revision, "Failed scene recovery published memory.");
            Reject<IOException>(() => client.Save(SessionDocumentKind.Code, code.Revision));
            Check(File.ReadAllText(CodePath(root)) == "// saved\n", "Failed pre-save changed file.");
        }
        finally { Directory.Delete(statePath); File.Move(backup, statePath); }
        code = client.InspectCode(); var goodCode = File.ReadAllBytes(CodePath(root));
        File.WriteAllBytes(CodePath(root), [255, 254, 0]);
        Reject<DecoderFallbackException>(() => client.Refresh(SessionDocumentKind.Code, code.Revision));
        Check(client.InspectCode().Text == code.Text && client.InspectCode().Revision == code.Revision, "Malformed external source changed live data.");
        File.WriteAllBytes(CodePath(root), goodCode);
        if (OperatingSystem.IsLinux() && Environment.UserName != "root")
        {
            client.Apply("pending denied save", new SessionCodeEdit(code.ID, code.Revision, "// pending denied save"));
            var mode = File.GetUnixFileMode(root); File.SetUnixFileMode(root, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            try { Reject<UnauthorizedAccessException>(() => client.Save(SessionDocumentKind.Code, client.InspectCode().Revision)); }
            finally { File.SetUnixFileMode(root, mode); }
            Check(File.ReadAllBytes(CodePath(root)).AsSpan().SequenceEqual(goodCode) && client.InspectCode().IsDirty, "Denied file save lost prior disk/live data.");
        }
        File.Delete(ScenePath(root));
        Check(client.InspectScene().HasExternalChange, "Deleted scene not external.");
        Reject<SessionConflictException>(() => client.Save(SessionDocumentKind.Scene, client.InspectScene().Revision));
        Reject<FileNotFoundException>(() => client.Refresh(SessionDocumentKind.Scene, client.InspectScene().Revision));
    }
    private static void Callbacks(string root)
    {
        CallbackEntity.Activations = 0;
        using var owner = Open(root); using var client = owner.Connect("callbacks");
        Check(CallbackEntity.Activations == 0, "Opening authoring activated gameplay.");
        var initial = Find(client, "A"); CallbackEntity.FailConstruct = true;
        var constructorFailure = Reject<Exception>(() => client.Apply("constructor fails", new SessionTransformEdit(initial.ID, initial.Revision, Pose(88))));
        CallbackEntity.FailConstruct = false;
        Check(constructorFailure.ToString().Contains("constructor fixture", StringComparison.Ordinal) && Find(client, "A").Revision == initial.Revision, "Failed reconstruction published data.");
        var a = Find(client, "A"); var change = client.Apply("pose", new SessionTransformEdit(a.ID, a.Revision, Pose(4)));
        owner.Changed += _ => { Reject<InvalidOperationException>(() => client.Apply("reenter", new SessionTransformEdit(a.ID, a.Revision, Pose(1)))); throw new InvalidOperationException("observer"); };
        var b = Find(client, "B"); var result = client.Apply("event", new SessionTransformEdit(b.ID, b.Revision, Pose(8)));
        Check(result.Diagnostics.Single().Code == "observer-failed" && Find(client, "B").Transform == Pose(8), "Observer failure hid committed edit.");
        CallbackEntity.FailPre = true;
        var hash = File.ReadAllBytes(ScenePath(root));
        var preFailure = Reject<Exception>(() => client.Save(SessionDocumentKind.Scene, client.InspectScene().Revision));
        CallbackEntity.FailPre = false; Check(preFailure.ToString().Contains("pre-save fixture", StringComparison.Ordinal), "The pre-save failure was not exercised.");
        Check(hash.AsSpan().SequenceEqual(File.ReadAllBytes(ScenePath(root))) && client.InspectScene().IsDirty, "Failed pre-save changed file/buffer.");
        CallbackEntity.FailPost = true;
        result = client.Save(SessionDocumentKind.Scene, client.InspectScene().Revision); CallbackEntity.FailPost = false;
        Check(result.FileWritten && result.Diagnostics.Any(d => d.Code == "save-callback-failed-after-write"), "Post-save failure concealed written file.");
        Check(CallbackEntity.Activations == 0 && change.OperationID != Guid.Empty, "Authoring activated gameplay.");
    }
    private static void Processes(string root)
    {
        using (var owner = Open(root))
        {
            Reject<IOException>(() => { using var duplicate = Open(root); });
            Exit(Child("excluded", root), 0);
        }
        using var child = Child("crash", root);
        var line = child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        Check(Guid.TryParse(line, out var operation), "Child did not commit recovery.");
        Exit(Child("excluded", root), 0);
        child.Kill(true); Check(child.WaitForExit(30000), "Crash child did not retire.");
        Exit(Child("recover", root, operation.ToString()), 0); Exit(Child("disk", root), 0);
    }
    private static void SaveCrash(string root)
    {
        using var child = Child("save-crash", root);
        var line = child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        Check(line == "written", "Child did not reach the post-write boundary.");
        child.Kill(true); Check(child.WaitForExit(30000), "Interrupted save process did not retire.");
        Exit(Child("save-recover", root), 0);
    }
    private static void RecoveryValidation(string root)
    {
        using (var owner = Open(root)) { }
        var path = IOPath.Combine(root, ".electron2d/session/state.json"); var original = File.ReadAllBytes(path);
        var invalid = Encoding.UTF8.GetString(original).Replace("\"Version\":1", "\"Version\":99", StringComparison.Ordinal);
        Check(invalid != Encoding.UTF8.GetString(original), "Recovery version mutation did not apply.");
        var scene = File.ReadAllBytes(ScenePath(root)); var code = File.ReadAllBytes(CodePath(root)); File.WriteAllText(path, invalid);
        try { Reject<InvalidDataException>(() => { using var owner = Open(root); }); }
        finally { File.WriteAllBytes(path, original); }
        using var recovered = Open(root); using var client = recovered.Connect("after invalid recovery");
        Check(File.ReadAllBytes(ScenePath(root)).AsSpan().SequenceEqual(scene) && File.ReadAllBytes(CodePath(root)).AsSpan().SequenceEqual(code), "Invalid recovery modified project files.");
        Check(client.InspectScene().Nodes.Count == 4, "Rejected open retained the owner lease.");
    }
    private static void HistoryBounds(string root)
    {
        using var owner = Open(root); using var client = owner.Connect("history bounds"); Guid first = default;
        for (var i = 0; i < 33; i++)
        {
            var code = client.InspectCode(); var result = client.Apply("bounded", new SessionCodeEdit(code.ID, code.Revision, "// " + i));
            if (i == 0) first = result.OperationID;
        }
        Check(client.InspectHistory().Count == 32, "History step bound did not trim."); Reject<ArgumentException>(() => client.Undo(first));
        for (var i = 0; i < 12; i++)
        {
            var code = client.InspectCode(); client.Apply("large source", new SessionCodeEdit(code.ID, code.Revision, new string((char)('a' + i), 3 * 1024 * 1024)));
        }
        Check(client.InspectHistory().Count < 12 && client.InspectHistory().Count > 0, "Recovery byte bound did not retire oldest groups.");
        Check(new FileInfo(IOPath.Combine(root, ".electron2d/session/state.json")).Length <= 64 * 1024 * 1024, "Recovery exceeded its byte bound.");
        var last = client.InspectHistory()[^1]; client.Undo(last.ID); client.Redo(last.ID);
        Check(client.InspectCode().Text[0] == 'l', "History stopped working after byte trimming.");
    }
    private static void Boundaries(string root)
    {
        Reject<ArgumentException>(() => { using var session = ProjectSession.Open(root, "../scene.e2dscene", "Game.cs"); });
        if (!OperatingSystem.IsWindows())
        {
            File.CreateSymbolicLink(IOPath.Combine(root, "link.cs"), CodePath(root));
            Reject<IOException>(() => { using var session = ProjectSession.Open(root, "scene.e2dscene", "link.cs"); });
        }
        using var owner = Open(root); var client = owner.Connect("bounds");
        var code = client.InspectCode();
        File.WriteAllText(CodePath(root), code.Text, new UTF8Encoding(false));
        client.Refresh(SessionDocumentKind.Code, code.Revision, SessionExternalResolution.KeepLive);
        code = client.InspectCode(); Check(code.IsDirty, "Acknowledged BOM-only change lost the live encoding policy.");
        client.Save(SessionDocumentKind.Code, code.Revision);
        Check(!client.InspectCode().IsDirty && File.ReadAllBytes(CodePath(root)).AsSpan().StartsWith(Encoding.UTF8.Preamble), "Save failed to preserve live BOM.");
        var safeBytes = File.ReadAllBytes(CodePath(root));
        File.WriteAllBytes(CodePath(root), new byte[4 * 1024 * 1024 + 1]);
        Check(client.InspectCode().Text == code.Text && client.InspectCode().HasExternalChange, "Oversize external source hid the live buffer.");
        Reject<SessionConflictException>(() => client.Save(SessionDocumentKind.Code, client.InspectCode().Revision));
        Reject<InvalidDataException>(() => client.Refresh(SessionDocumentKind.Code, client.InspectCode().Revision));
        File.WriteAllBytes(CodePath(root), safeBytes);
        Reject<EncoderFallbackException>(() => client.Apply("unicode", new SessionCodeEdit(code.ID, code.Revision, "\ud800")));
        Reject<ArgumentException>(() => client.Apply("size", new SessionCodeEdit(code.ID, code.Revision, new string('x', 4 * 1024 * 1024))));
        Reject<ArgumentException>(() => client.Apply("empty"));
        Reject<InvalidOperationException>(() => Task.Run(() => client.InspectCode()).GetAwaiter().GetResult());
        client.Dispose(); Reject<ObjectDisposedException>(() => client.InspectCode());
    }
}

internal sealed class CallbackEntity : Entity
{
    internal static bool FailPre, FailPost, FailConstruct, BlockPost;
    public CallbackEntity() { if (FailConstruct) throw new InvalidOperationException("constructor fixture"); }
    internal static int Activations;
    private static Node CreateCallback() => new CallbackEntity();
    protected override Func<Node> CreateSceneInstanceFactory() => CreateCallback;
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what is Node.NotificationReady or Node.NotificationProcess or Node.NotificationEnterTree) Activations++;
        if (FailPre && what == Node.NotificationEditorPreSave) throw new InvalidOperationException("pre-save fixture");
        if (BlockPost && what == Node.NotificationEditorPostSave) { Console.WriteLine("written"); Console.Out.Flush(); Thread.Sleep(Timeout.Infinite); }
        if (FailPost && what == Node.NotificationEditorPostSave) throw new InvalidOperationException("post-save fixture");
    }
}
