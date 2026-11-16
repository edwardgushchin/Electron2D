using Electron2D;

if (args.Length != 0 && args[0] == "deletion") { DeletionChecks.Run(); return; }

if (args.Length != 0 && args[0] == "window") { WindowChecks.Run(args[1]); return; }

if (args.Length != 0 && args[0] == "changes") { TreeChangeChecks.Run(); return; }

using (var failedRoot = new Node())
{
    failedRoot.AddChild(new Node()); var rollbackCounts = new List<int>(); SceneTree? escaped = null;
    failedRoot.TreeEntered += n => { escaped = n.Tree; escaped!.NodeRemoved += (t, _) => rollbackCounts.Add(t.NodeCount); throw new InvalidOperationException("controlled constructor failure"); };
    try { _ = new SceneTree(failedRoot); throw new Exception("missing activation failure"); } catch (AggregateException) { }
    Check(escaped!.IsDisposed && !failedRoot.IsDisposed && failedRoot.Tree is null && failedRoot.GetChild(0).Tree is null, "constructor rollback releases membership without taking caller ownership");
    try { _ = escaped.NodeCount; throw new Exception("missing failed-tree guard"); } catch (ObjectDisposedException) { }

}

using (var failedInitialChange = new Node())
{
    failedInitialChange.Ready += n => n.Tree!.TreeChanged += _ => throw new InvalidOperationException("controlled initial change failure");
    try { _ = new SceneTree(failedInitialChange); throw new Exception("missing initial change failure"); } catch (AggregateException) { }
    Check(!failedInitialChange.IsDisposed && failedInitialChange.Tree is null, "initial change failure rolls activation back");
}

var root = new Node { Name = "Root" };
var branch = new Node { Name = "Branch" };
var leaf = new Node { Name = "Leaf" };
root.AddChild(branch); branch.AddChild(leaf);
var observations = new List<string>();
var initialChanges = 0;
root.TreeEntered += n => n.Tree!.TreeChanged += _ => initialChanges++;
foreach (var node in new[] { root, branch, leaf })
{
    node.TreeEntered += n => observations.Add($"enter:{n.Name}:{n.Tree!.NodeCount}");
    node.TreeExiting += n => observations.Add($"exit:{n.Name}:{n.Tree!.NodeCount}");
}
using var tree = new SceneTree(root);
Check(string.Join(",", observations) == "enter:Root:1,enter:Branch:2,enter:Leaf:3", "parent-first entry counts: " + string.Join(",", observations));
Check(tree.Root == root && tree.NodeCount == 3 && initialChanges == 1, "stable root, complete entry and initial tree change");
observations.Clear();
tree.NodeRemoved += (t, n) => observations.Add($"removed:{n.Name}:{t.NodeCount}");
root.RemoveChild(branch);
Check(string.Join(",", observations) == "exit:Leaf:3,removed:Leaf:2,exit:Branch:2,removed:Branch:1", "child-first exit counts: " + string.Join(",", observations));
Check(tree.NodeCount == 1 && tree.Root == root, "detached descendants excluded");
root.AddChild(branch);
Check(tree.NodeCount == 3, "reattachment counted exactly once");
branch.TreeExiting += _ => throw new InvalidOperationException("controlled callback failure");
try { root.RemoveChild(branch); throw new Exception("missing callback failure"); }
catch (AggregateException) { }
Check(tree.NodeCount == 1 && branch.Tree is null && leaf.Tree is null, "failed callback still releases all membership");
branch.Dispose();
using var destination = new Node { Name = "Destination" };
root.AddChild(destination);
var moving = new Node { Name = "Moving" };
root.AddChild(moving);
moving.Reparent(destination);
Check(tree.NodeCount == 3 && moving.Parent == destination, "reparent preserves final membership");
moving.TreeEntered += _ => throw new InvalidOperationException("controlled entry failure");
destination.RemoveChild(moving);
try { root.AddChild(moving); throw new Exception("missing entry failure"); }
catch (AggregateException) { }
Check(tree.NodeCount == 3 && moving.Tree == tree, "entry callback failure preserves committed membership");
moving.Dispose();
Check(tree.NodeCount == 2, "direct disposal releases membership");
root.Notify(Node.NotificationEnterTree);
root.Notify(Node.NotificationExitTree);
Check(tree.NodeCount == 2, "manual lifecycle notifications do not change membership");
for (var i = 0; i < 10000; i++) _ = tree.NodeCount;
var before = GC.GetAllocatedBytesForCurrentThread();
var total = 0;
for (var i = 0; i < 10000; i++) total += tree.NodeCount;
var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
Check(total == 20000 && allocated == 0, $"prepared count reads allocate zero bytes: {allocated}");
tree.FinalizeLoop();
Check(tree.NodeCount == 0 && tree.Root == root && root.IsDisposed, "finalization preserves root identity and releases count");
tree.Dispose();
Check(tree.Root == root, "root identity survives disposal");
try { _ = tree.NodeCount; throw new Exception("missing disposed guard"); }
catch (ObjectDisposedException) { }
Console.WriteLine("SceneTree public membership checks passed.");

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
