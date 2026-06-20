using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class DeferredCallTests
{
    [Fact]
    public void DeferredCallsRunAfterProcessTraversalInQueueOrderAndDrainNestedCalls()
    {
        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        tree.Root.AddChild(new DeferringProcessNode(events));
        tree.Root.AddChild(new ProcessRecordingNode("Sibling", events));

        tree.ProcessFrame(0.25d);

        Assert.Equal(
            new[]
            {
                "Deferrer:_Process",
                "Sibling:_Process",
                "Deferrer:deferred:object",
                "callable:deferred",
                "nested:deferred"
            },
            events);
    }

    [Fact]
    public void QueueFreeDuringReadyFlushesAfterLifecyclePass()
    {
        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var parent = new Electron2D.Node { Name = "Parent" };
        var deleting = new QueueFreeOnReadyNode("Deleting", events);
        var sibling = new ReadyRecordingNode("Sibling", events);
        parent.AddChild(deleting);
        parent.AddChild(sibling);

        tree.Root.AddChild(parent);

        Assert.Equal(new[] { "Deleting:_Ready", "Sibling:_Ready", "Deleting:_ExitTree" }, events);
        Assert.False(Electron2D.Object.IsInstanceValid(deleting));
        Assert.True(Electron2D.Object.IsInstanceValid(sibling));
        Assert.Equal(1, parent.GetChildCount());
        Assert.Same(sibling, parent.GetChild(0));
    }

    [Fact]
    public void QueueFreeDuringPhysicsFlushesAfterTraversalAndKeepsSiblingsRunning()
    {
        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var deleting = new QueueFreeOnPhysicsNode("Deleting", events);
        var descendant = new PhysicsRecordingNode("Descendant", events);
        var sibling = new PhysicsRecordingNode("Sibling", events);
        deleting.AddChild(descendant);
        tree.Root.AddChild(deleting);
        tree.Root.AddChild(sibling);
        events.Clear();

        tree.PhysicsFrame(0.25d);

        Assert.Equal(
            new[]
            {
                "Deleting:_PhysicsProcess:queued:True",
                "Descendant:_PhysicsProcess",
                "Sibling:_PhysicsProcess",
                "Descendant:_ExitTree",
                "Deleting:_ExitTree"
            },
            events);
        Assert.False(Electron2D.Object.IsInstanceValid(deleting));
        Assert.False(Electron2D.Object.IsInstanceValid(descendant));
        Assert.True(Electron2D.Object.IsInstanceValid(sibling));
    }

    [Fact]
    public void TreeCanBeModifiedDuringProcessTraversalWithoutCorruptingNextFrame()
    {
        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var victim = new ProcessRecordingNode("Victim", events);
        var replacement = new ProcessRecordingNode("Replacement", events);
        var mutating = new MutatingProcessNode(events, victim, replacement);
        var sibling = new ProcessRecordingNode("Sibling", events);
        tree.Root.AddChild(mutating);
        tree.Root.AddChild(victim);
        tree.Root.AddChild(sibling);
        events.Clear();

        tree.ProcessFrame(0.25d);

        Assert.Equal(new[] { "Mutating:_Process", "Victim:_ExitTree", "Sibling:_Process" }, events);
        Assert.Equal(3, tree.Root.GetChildCount());
        Assert.Same(replacement, tree.Root.GetChild(2));

        events.Clear();
        tree.ProcessFrame(0.25d);

        Assert.Equal(new[] { "Mutating:_Process", "Sibling:_Process", "Replacement:_Process" }, events);
    }

    private sealed class DeferringProcessNode : Electron2D.Node
    {
        private readonly List<string> _events;

        public DeferringProcessNode(List<string> events)
        {
            Name = "Deferrer";
            _events = events;
        }

        public override void _Process(double delta)
        {
            _events.Add("Deferrer:_Process");
            CallDeferred(nameof(RecordDeferred), "object");
            Electron2D.Callable.From(() =>
            {
                _events.Add("callable:deferred");
                Electron2D.Callable.From(() => _events.Add("nested:deferred")).CallDeferred();
            }).CallDeferred();
        }

        public void RecordDeferred(string value)
        {
            _events.Add($"Deferrer:deferred:{value}");
        }
    }

    private sealed class QueueFreeOnReadyNode : Electron2D.Node
    {
        private readonly List<string> _events;

        public QueueFreeOnReadyNode(string name, List<string> events)
        {
            Name = name;
            _events = events;
        }

        public override void _Ready()
        {
            _events.Add($"{Name}:_Ready");
            QueueFree();
        }

        public override void _ExitTree()
        {
            _events.Add($"{Name}:_ExitTree");
        }
    }

    private sealed class ReadyRecordingNode : Electron2D.Node
    {
        private readonly List<string> _events;

        public ReadyRecordingNode(string name, List<string> events)
        {
            Name = name;
            _events = events;
        }

        public override void _Ready()
        {
            _events.Add($"{Name}:_Ready");
        }
    }

    private sealed class QueueFreeOnPhysicsNode : Electron2D.Node
    {
        private readonly List<string> _events;

        public QueueFreeOnPhysicsNode(string name, List<string> events)
        {
            Name = name;
            _events = events;
        }

        public override void _PhysicsProcess(double delta)
        {
            QueueFree();
            _events.Add($"{Name}:_PhysicsProcess:queued:{IsQueuedForDeletion()}");
        }

        public override void _ExitTree()
        {
            _events.Add($"{Name}:_ExitTree");
        }
    }

    private sealed class PhysicsRecordingNode : Electron2D.Node
    {
        private readonly List<string> _events;

        public PhysicsRecordingNode(string name, List<string> events)
        {
            Name = name;
            _events = events;
        }

        public override void _PhysicsProcess(double delta)
        {
            _events.Add($"{Name}:_PhysicsProcess");
        }

        public override void _ExitTree()
        {
            _events.Add($"{Name}:_ExitTree");
        }
    }

    private sealed class MutatingProcessNode : Electron2D.Node
    {
        private readonly List<string> _events;
        private readonly Electron2D.Node _replacement;
        private readonly Electron2D.Node _victim;

        public MutatingProcessNode(List<string> events, Electron2D.Node victim, Electron2D.Node replacement)
        {
            Name = "Mutating";
            _events = events;
            _victim = victim;
            _replacement = replacement;
        }

        public override void _Process(double delta)
        {
            _events.Add("Mutating:_Process");
            var parent = GetParent();
            if (parent is not null && _victim.GetParent() is not null)
            {
                parent.RemoveChild(_victim);
                parent.AddChild(_replacement);
            }
        }
    }

    private sealed class ProcessRecordingNode : Electron2D.Node
    {
        private readonly List<string> _events;

        public ProcessRecordingNode(string name, List<string> events)
        {
            Name = name;
            _events = events;
        }

        public override void _Process(double delta)
        {
            _events.Add($"{Name}:_Process");
        }

        public override void _ExitTree()
        {
            _events.Add($"{Name}:_ExitTree");
        }
    }
}
