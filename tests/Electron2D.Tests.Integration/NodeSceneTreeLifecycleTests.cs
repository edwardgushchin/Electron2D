using System.Globalization;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class NodeSceneTreeLifecycleTests
{
    [Fact]
    public void AddChildToRootEntersTreeAndBecomesReady()
    {
        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var child = new RecordingNode("Child", events);

        tree.Root.AddChild(child);

        Assert.True(child.IsInsideTree());
        Assert.Same(tree, child.GetTree());
        Assert.Same(tree.Root, child.GetParent());
        Assert.Equal(new[] { "Child:_EnterTree", "Child:_Ready" }, events);
    }

    [Fact]
    public void SubtreeEnterAndReadyUseGodotLikeOrder()
    {
        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var parent = new RecordingNode("Parent", events);
        var child = new RecordingNode("Child", events);
        parent.AddChild(child);

        tree.Root.AddChild(parent);

        Assert.Equal(
            new[]
            {
                "Parent:_EnterTree",
                "Child:_EnterTree",
                "Child:_Ready",
                "Parent:_Ready"
            },
            events);
    }

    [Fact]
    public void ProcessPhysicsAndInputUseTreeOrder()
    {
        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var inputEvent = new Electron2D.InputEvent();
        var parent = new RecordingNode("Parent", events, inputEvent);
        var child = new RecordingNode("Child", events, inputEvent);
        parent.AddChild(child);
        tree.Root.AddChild(parent);
        events.Clear();

        tree.ProcessFrame(0.25d);
        tree.PhysicsFrame(0.5d);
        tree.DispatchInput(inputEvent);

        Assert.Equal(
            new[]
            {
                "Parent:_Process:0.25",
                "Child:_Process:0.25",
                "Parent:_PhysicsProcess:0.50",
                "Child:_PhysicsProcess:0.50",
                "Parent:_Input:True",
                "Child:_Input:True"
            },
            events);
    }

    [Fact]
    public void RemoveChildExitsTreeAndClearsTreeState()
    {
        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var parent = new RecordingNode("Parent", events);
        var child = new RecordingNode("Child", events);
        parent.AddChild(child);
        tree.Root.AddChild(parent);
        events.Clear();

        tree.Root.RemoveChild(parent);

        Assert.False(parent.IsInsideTree());
        Assert.False(child.IsInsideTree());
        Assert.Null(parent.GetTree());
        Assert.Null(child.GetTree());
        Assert.Null(parent.GetParent());
        Assert.Same(parent, child.GetParent());
        Assert.Equal(new[] { "Child:_ExitTree", "Parent:_ExitTree" }, events);
    }

    [Fact]
    public void UserCodeExceptionIsCapturedAndTraversalContinues()
    {
        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var throwing = new ThrowingProcessNode("Throwing", events);
        var sibling = new RecordingNode("Sibling", events);
        tree.Root.AddChild(throwing);
        tree.Root.AddChild(sibling);
        events.Clear();

        tree.ProcessFrame(1.0d);

        Assert.Equal(new[] { "Throwing:_Process:throw", "Sibling:_Process:1.00" }, events);
        var diagnostic = Assert.Single(tree.Diagnostics);
        Assert.Same(throwing, diagnostic.Node);
        Assert.Equal("_Process", diagnostic.Callback);
        Assert.IsType<InvalidOperationException>(diagnostic.Exception);
    }

    private sealed class RecordingNode : Electron2D.Node
    {
        private readonly List<string> _events;
        private readonly Electron2D.InputEvent? _expectedInputEvent;

        public RecordingNode(
            string name,
            List<string> events,
            Electron2D.InputEvent? expectedInputEvent = null)
        {
            Name = name;
            _events = events;
            _expectedInputEvent = expectedInputEvent;
        }

        public override void _EnterTree()
        {
            _events.Add($"{Name}:_EnterTree");
        }

        public override void _Ready()
        {
            _events.Add($"{Name}:_Ready");
        }

        public override void _Process(double delta)
        {
            _events.Add($"{Name}:_Process:{delta.ToString("0.00", CultureInfo.InvariantCulture)}");
        }

        public override void _PhysicsProcess(double delta)
        {
            _events.Add($"{Name}:_PhysicsProcess:{delta.ToString("0.00", CultureInfo.InvariantCulture)}");
        }

        public override void _Input(Electron2D.InputEvent inputEvent)
        {
            _events.Add($"{Name}:_Input:{ReferenceEquals(inputEvent, _expectedInputEvent)}");
        }

        public override void _ExitTree()
        {
            _events.Add($"{Name}:_ExitTree");
        }
    }

    private sealed class ThrowingProcessNode : Electron2D.Node
    {
        private readonly List<string> _events;

        public ThrowingProcessNode(string name, List<string> events)
        {
            Name = name;
            _events = events;
        }

        public override void _Process(double delta)
        {
            _events.Add($"{Name}:_Process:throw");
            throw new InvalidOperationException("boom");
        }
    }
}
