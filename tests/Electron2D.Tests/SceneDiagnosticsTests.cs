using Mathf = Electron2D.Mathf;
using Electron2D;
using ScenePath = Electron2D.Path;

internal static class SceneDiagnosticsTests
{
    internal static void Run()
    {
        using var root = new Node(); var edited = new Node { Name = "Edited" }; var outside = new Node { Name = "Outside" };
        var probe = new WarningNode(); edited.AddChild(probe); root.AddChild(edited); root.AddChild(outside);
        Check(root.GetConfigurationWarnings().Length == 0, "Default warning query is empty."); probe.UpdateConfigurationWarnings();
        using var tree = new SceneTree(root); var events = 0;
        Check(tree.EditedSceneRoot is null && !tree.DebugPathsHint, "Diagnostics default to inactive.");
        var inspected = new List<string>(); Action<SceneTree, Node> observer = (owner, node) => { Check(owner == tree && node == probe, "Warning event identity."); events++; inspected.AddRange(node.GetConfigurationWarnings()); };
        tree.NodeConfigurationWarningChanged += observer;
        probe.UpdateConfigurationWarnings(); Check(events == 0, "No event without edited scene.");
        tree.EditedSceneRoot = edited; Check(events == 0, "Selection emits no refresh.");
        outside.UpdateConfigurationWarnings(); Check(events == 0, "Other subtree is outside selection.");
        probe.Warning = "Missing target"; probe.UpdateConfigurationWarnings(); probe.UpdateConfigurationWarnings();
        Check(events == 2 && inspected.SequenceEqual(new[] { "Missing target", "Missing target" }), "Requests are synchronous, repeated and queried by the consumer.");
        tree.NodeConfigurationWarningChanged -= observer;
        tree.EditedSceneRoot = probe; var rootEvents = 0; tree.NodeConfigurationWarningChanged += (_, node) => { Check(node == probe, "Selected root warns too."); rootEvents++; };
        probe.UpdateConfigurationWarnings(); Check(rootEvents == 1, "Selected root included.");
        Action<SceneTree, Node> fail = (_, _) => throw new ApplicationException("warning listener"); tree.NodeConfigurationWarningChanged += fail;
        Reject<ApplicationException>(probe.UpdateConfigurationWarnings); tree.NodeConfigurationWarningChanged -= fail;
        // Updating warnings does not evaluate a user override unless a listener explicitly asks.
        probe.ThrowQuery = true; probe.UpdateConfigurationWarnings(); Reject<ApplicationException>(() => probe.GetConfigurationWarnings()); probe.ThrowQuery = false;
        Task.Run(() => { Reject<InvalidOperationException>(probe.UpdateConfigurationWarnings); Reject<InvalidOperationException>(() => probe.GetConfigurationWarnings()); Reject<InvalidOperationException>(() => tree.DebugPathsHint = true); Reject<InvalidOperationException>(() => tree.EditedSceneRoot = root); }).GetAwaiter().GetResult();
        using var detached = new Node(); Reject<ArgumentException>(() => tree.EditedSceneRoot = detached);
        using var foreignRoot = new Node(); using var otherTree = new SceneTree(foreignRoot); Reject<ArgumentException>(() => tree.EditedSceneRoot = foreignRoot);
        Action<SceneTree, Node> removed = (_, node) => { if (node == probe) Check(tree.EditedSceneRoot is null, "Selection clears before NodeRemoved."); }; tree.NodeRemoved += removed;
        edited.RemoveChild(probe); Check(tree.EditedSceneRoot is null, "Removed selection cleared."); var before = rootEvents; probe.UpdateConfigurationWarnings(); Check(rootEvents == before, "Detached refresh is silent.");
        tree.NodeRemoved -= removed; edited.AddChild(probe); Check(tree.EditedSceneRoot is null, "Reentry does not restore tooling selection.");
        tree.DebugPathsHint = true; tree.DebugPathsHint = false;
        using (var zRoot = new Entity())
        using (var zTree = new SceneTree(zRoot))
        {
            zTree.EditedSceneRoot = zRoot; var refreshes = 0;
            zTree.NodeConfigurationWarningChanged += (_, _) => { refreshes++; Check(zRoot.ZIndex == 3, "Z is committed before refresh."); };
            zRoot.ZIndex = 3; zRoot.ZIndex = 3; Check(refreshes == 2, "Repeated Z assignment refreshes warnings.");
            Reject<ArgumentOutOfRangeException>(() => zRoot.ZIndex = 10000); Check(refreshes == 2, "Invalid Z emits no event.");
        }
        Check(tree.GetPropertyList().Any(p => p.Name == nameof(SceneTree.DebugPathsHint)) && tree.GetPropertyList().Any(p => p.Name == nameof(SceneTree.EditedSceneRoot)), "Typed diagnostic descriptors.");
        using var path = new ScenePath(); var follower = new PathFollow();
        Check(follower.GetConfigurationWarnings().Length == 0, "Detached follower has no warning."); root.AddChild(follower);
        Check(follower.GetConfigurationWarnings().Length == 1, "Visible orphan follower warns."); follower.Hide(); Check(follower.GetConfigurationWarnings().Length == 0, "Hidden follower has no warning."); follower.Show();
        root.AddChild(path); follower.Reparent(path, false); Check(follower.GetConfigurationWarnings().Length == 0, "Direct parent with no curve is valid.");
        var canvas = new Entity { Visible = false }; root.AddChild(canvas); follower.Reparent(canvas, false); Check(follower.GetConfigurationWarnings().Length == 0, "Hidden ancestor suppresses warning."); canvas.Show(); Check(follower.GetConfigurationWarnings().Length == 1, "Visible invalid ancestor warns.");
        follower.Dispose(); Reject<ObjectDisposedException>(() => follower.GetConfigurationWarnings()); Reject<ObjectDisposedException>(follower.UpdateConfigurationWarnings); Reject<ObjectDisposedException>(() => tree.EditedSceneRoot = follower);
        var settings = ProjectSettings.Instance; var color = settings.Get(ProjectSettings.DebugPathsColor);
        try { Reject<System.Text.Json.JsonException>(() => settings.Set(ProjectSettings.DebugPathsColor, new Color(float.NaN, 0, 0))); Check(settings.Get(ProjectSettings.DebugPathsColor) == color, "Invalid color leaves state unchanged."); }
        finally { settings.Set(ProjectSettings.DebugPathsColor, color); }
        using var huge = new Curve2D(); huge.AddPoint(Vector2.Zero); huge.AddPoint(new(11_000_000, 0));
        var debugPath = new ScenePath { Name = "Debug", Curve = huge }; root.AddChild(debugPath); tree.DebugPathsHint = true;
        Reject<InvalidOperationException>(debugPath.PrepareCanvas); tree.DebugPathsHint = false; debugPath.PrepareCanvas();
        tree.Dispose(); Reject<ObjectDisposedException>(() => tree.DebugPathsHint = false); Reject<ObjectDisposedException>(() => _ = tree.EditedSceneRoot);
        SiblingDiagnostics();
        Console.WriteLine("Scene diagnostics checks passed.");
    }

    private static void SiblingDiagnostics()
    {
        using var root = new Node(); var timer = new Electron2D.Timer(); var animated = new AnimatedSprite();
        Check(timer.GetConfigurationWarnings().Length == 0 && animated.GetConfigurationWarnings().Length == 1, "Sibling defaults.");
        timer.WaitTime = .01; Check(timer.GetConfigurationWarnings().Length == 1, "Detached short timer warns.");
        timer.WaitTime = .05 - Mathf.Epsilon; Check(timer.GetConfigurationWarnings().Length == 0, "Timer threshold excludes canonical epsilon boundary.");
        timer.WaitTime = .05 - 2 * Mathf.Epsilon; Check(timer.GetConfigurationWarnings().Length == 1, "Timer threshold includes values below boundary.");
        root.AddChild(timer); root.AddChild(animated); using var tree = new SceneTree(root); tree.EditedSceneRoot = root;
        var calls = new List<string>(); tree.NodeConfigurationWarningChanged += (_, node) => calls.Add(node.Name);
        timer.WaitTime = .01; timer.WaitTime = .01; Check(calls.Count == 2, "Repeated timer duration refreshes.");
        timer.Start(.02); Check(calls.Count == 3 && !timer.IsStopped(), "Start(duration) uses the same refresh path.");
        Action<SceneTree, Node> detach = (_, node) => { if (node == timer) root.RemoveChild(timer); };
        tree.NodeConfigurationWarningChanged += detach; timer.Start(.03); Check(!timer.IsInsideTree && timer.TimeLeft != .03, "Reentrant removal cannot restart detached timer."); tree.NodeConfigurationWarningChanged -= detach; root.AddChild(timer);
        using var frames = new SpriteFrames(); animated.SpriteFramesChanged += () => calls.Add("frames"); calls.Clear(); animated.SpriteFrames = frames;
        Check(calls.SequenceEqual(new[] { animated.Name, "frames" }) && animated.GetConfigurationWarnings().Length == 0, "Library warning refresh precedes its change event.");
        animated.Hide(); animated.SpriteFrames = null; Check(animated.GetConfigurationWarnings().Length == 1, "Hidden missing-library warning is retained.");
        Action<SceneTree, Node> failure = (_, _) => throw new ApplicationException("sibling warning"); tree.NodeConfigurationWarningChanged += failure;
        Reject<ApplicationException>(() => timer.WaitTime = .04); Check(timer.WaitTime == .04, "Timer duration survives warning error.");
        Reject<ApplicationException>(() => animated.SpriteFrames = frames); Check(ReferenceEquals(animated.SpriteFrames, frames), "Sprite library survives warning error."); tree.NodeConfigurationWarningChanged -= failure;
        tree.NodeConfigurationWarningChanged += (_, node) => { if (node == timer) timer.Dispose(); };
        timer.Start(.05); Check(timer.IsDisposed, "Warning callback disposal cannot resurrect a timer countdown.");
    }

    private sealed class WarningNode : Node
    {
        public string Warning = string.Empty;
        public bool ThrowQuery;
        public override string[] GetConfigurationWarnings() { var original = base.GetConfigurationWarnings(); if (ThrowQuery) throw new ApplicationException("warning query"); return Warning.Length == 0 ? original : [.. original, Warning]; }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
