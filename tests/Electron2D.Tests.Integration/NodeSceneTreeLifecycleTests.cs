/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
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
    public void SubtreeEnterAndReadyUseElectron2DOrder()
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
        tree.PhysicsFrame(1d / 60d);
        tree.DispatchInput(inputEvent);

        Assert.Equal(
            new[]
            {
                "Parent:_Process:0.25",
                "Child:_Process:0.25",
                "Parent:_PhysicsProcess:0.02",
                "Child:_PhysicsProcess:0.02",
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
        Assert.Equal(Electron2D.RuntimeUserCodeFailureKind.LifecycleCallback, diagnostic.Kind);
        Assert.IsType<InvalidOperationException>(diagnostic.Exception);
        Assert.Equal("boom", diagnostic.Message);
        Assert.Contains(nameof(ThrowingProcessNode._Process), diagnostic.StackTrace);
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
