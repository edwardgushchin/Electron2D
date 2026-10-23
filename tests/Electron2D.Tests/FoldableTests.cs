using Electron2D;
using System.Diagnostics;

internal static class FoldableTests
{
    private sealed class CallbackControl : Control
    {
        internal Action? Measure;
        protected override Vector2 OnGetMinimumSize() { var callback = Measure; Measure = null; callback?.Invoke(); return new(20, 12); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Flush(SceneTree tree) { tree.ProcessFrame(.01); tree.ProcessFrame(.01); tree.ProcessFrame(.01); }
    internal static void RunManaged()
    {
        using (var panel = new FoldableContainer("Callback"))
        using (var removable = new CallbackControl { Name = "Removable" })
        {
            panel.AddTitleBarControl(removable); removable.Measure = () => panel.RemoveTitleBarControl(removable);
            Check(panel.GetMinimumSize().IsFinite() && removable.Parent is null, "Title measurement tolerates callback removal.");
        }
        using var group = new FoldableGroup(); var root = new SubViewport { Size = new(400, 240) };
        var a = new FoldableContainer("First") { Name = "A", Size = new(180, 120), FoldableGroup = group }; var b = new FoldableContainer("Second") { Name = "B", Position = new(200, 0), Size = new(180, 120), FoldableGroup = group };
        var body = new Control { Name = "Body", CustomMinimumSize = new(80, 30) }; var title = new Button("+") { Name = "Extra" }; a.AddChild(body); a.AddTitleBarControl(title); root.AddChild(a); root.AddChild(b);
        using var tree = new SceneTree(root); Flush(tree);
        Check(!a.Folded && b.Folded && body.Visible && group.ResourceLocalToScene && !group.AllowFoldingAll, "Defaults and assignment select a single expanded member.");
        Check(a.FocusMode == FocusMode.All && a.MouseFilter == MouseFilter.Stop && a.TitlePosition == BarPosition.Top && a.TitleTextOverrunBehavior == TextOverrunBehavior.NoTrimming, "Public defaults.");
        var expandedHeight = a.GetMinimumSize().Y; var titleEvents = 0; a.FoldingChanged += _ => titleEvents++;
        a.Fold(); Check(!a.Folded && titleEvents == 1, "Refused explicit fold still emits actual state.");
        group.AllowFoldingAll = true; a.Folded = true; Flush(tree); Check(!body.Visible && title.Visible && a.GetMinimumSize().Y < expandedHeight && titleEvents == 1, "Silent property, deferred body hiding and retained title controls.");
        a.Expand(); Flush(tree); Check(body.Visible && titleEvents == 2, "Expand shows content and emits once.");
        bool observedBeforeCommit = false; Action<FoldableContainer> observe = node => { observedBeforeCommit = node.Folded && a.Folded; }; group.Expanded += observe;
        b.Expand(); Flush(tree); Check(observedBeforeCommit && a.Folded && !b.Folded && ReferenceEquals(group.GetExpandedContainer(), b), "Group event follows peer folding and precedes source expansion."); group.Expanded -= observe;
        Action<FoldableContainer> fail = _ => throw new InvalidOperationException("group callback"); group.Expanded += fail;
        Reject<AggregateException>(a.Expand); Check(!a.Folded && b.Folded, "Throwing group observers cannot prevent required commitment."); group.Expanded -= fail;
        Action<FoldableContainer> self = node => node.Expand(); group.Expanded += self; b.Expand(); Check(!b.Folded && a.Folded, "Reentrant same-target expansion terminates."); group.Expanded -= self;
        Action<FoldableContainer> replace = node => { if (ReferenceEquals(node, a)) b.Expand(); }; group.Expanded += replace; a.Expand(); Check(a.Folded && !b.Folded, "Later reentrant group selection wins."); group.Expanded -= replace;
        group.AllowFoldingAll = true; b.Fold(); group.AllowFoldingAll = false; Check(group.GetExpandedContainer() is not null, "Disallowing all-folded expands a member.");
        a.FoldableGroup = null; a.Expand(); Flush(tree); a.TitlePosition = BarPosition.Bottom; a.LayoutDirection = LayoutDirection.RTL; a.TitleAlignment = HorizontalAlignment.Right; a.TitleTextDirection = TextDirection.Inherited; a.TitleTextOverrunBehavior = TextOverrunBehavior.TrimEllipsis; Flush(tree); a.Position = Vector2.Zero;
        Check(title.Position.Y > body.Position.Y && title.Position.X < a.Size.X * .5f, "Bottom title and RTL title controls.");
        a.TooltipText = "header hint"; Check(a.GetTooltip(new(2, 2)) == "" && a.GetTooltip(new(2, a.Size.Y - 2)) == "header hint", "Tooltip limited to title area.");
        using var click = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = new(2, a.Size.Y - 2) }; Check(a.HitTest(click.Position), "Native-compatible title hit: " + a.Position + " / " + a.Size + " / " + click.Position); root.PushInput(click, true); Check(a.Folded, "Title mouse press folds immediately; a=" + a.Size + ", hovered=" + root.GUIGetHoveredControl()?.Name);
        using var accept = new InputEventAction { Action = "ui_accept", Pressed = true }; a.GrabFocus(); root.PushInput(accept, true); Check(!a.Folded, "Exact accept action expands.");
        Reject<ArgumentOutOfRangeException>(() => a.TitlePosition = BarPosition.Max); Reject<ArgumentOutOfRangeException>(() => a.TitleAlignment = HorizontalAlignment.Fill); Reject<ArgumentNullException>(() => a.Title = null!);
        Reject<ArgumentException>(() => a.AddTitleBarControl(title));
        Task.Run(() => { Reject<InvalidOperationException>(() => _ = a.Title); Reject<InvalidOperationException>(() => group.AllowFoldingAll = true); }).GetAwaiter().GetResult();
        var guard = root.BeginSceneCapture(); try { Reject<InvalidOperationException>(() => a.Fold()); Reject<InvalidOperationException>(() => a.AddTitleBarControl(new Control())); } finally { Node.EndSceneCapture(guard); }
        a.FoldableGroup = group; Flush(tree);
        a.Owner = root; b.Owner = root; body.Owner = root; title.Owner = root;
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Electron2D-foldable-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var scene = new PackedScene(); scene.Pack(root); var path = System.IO.Path.Combine(folder, "scene.e2dscene"); ResourceSaver.Save(scene, path); RunArchive(path);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(FoldableTests).Assembly.Location); start.ArgumentList.Add("--foldable-archive"); start.ArgumentList.Add(path); start.Environment.Remove("ELECTRON2D_TEST_FOLDABLE");
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh foldable process deadline."); }
            Check(child.ExitCode == 0, "Fresh process: " + output.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(folder, true); }
        a.RemoveTitleBarControl(title); Check(title.Parent is null && !title.IsDisposed, "Removing title control preserves caller ownership."); title.Dispose(); b.Dispose(); Check(group.GetContainers().Length == 1, "Disposed members unregister.");
        Console.WriteLine("Foldable managed: group ordering/failure/reentry, pointer/action, RTL/bottom layout, visibility/title controls, storage/fresh local groups and guards passed.");
    }
    internal static void RunArchive(string path)
    {
        using var saved = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var one = saved.Instantiate(); using var two = saved.Instantiate(); using var tree = new SceneTree(one);
        var a = (FoldableContainer)one.GetNode("A"); var b = (FoldableContainer)one.GetNode("B"); var extra = (Button)a.GetNode("Extra");
        Check(a.Title == "First" && a.TitlePosition == BarPosition.Bottom && ReferenceEquals(a.FoldableGroup, b.FoldableGroup) && !ReferenceEquals(a.FoldableGroup, ((FoldableContainer)two.GetNode("A")).FoldableGroup), "Exact state and scene-local group aliases.");
        Flush(tree); Check(extra.Visible && extra.Position.Y > 20, "Title control membership restored through typed relative node reference."); a.RemoveTitleBarControl(extra); Check(extra.Parent == null, "Fresh title control remains removable."); extra.Dispose();
    }
}
