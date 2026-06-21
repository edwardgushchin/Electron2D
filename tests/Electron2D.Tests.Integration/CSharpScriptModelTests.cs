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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class CSharpScriptModelTests
{
    [Fact]
    public void ScriptClassInheritedFromNodeReceivesLifecycleAndEngineServices()
    {
        var tree = new Electron2D.SceneTree();
        var script = new ScriptNode { Name = "Script" };

        tree.Root.AddChild(script);
        tree.ProcessFrame(0.125d);
        tree.PhysicsFrame(0.25d);

        Assert.Equal(
            ["_EnterTree", "_Ready", "_Process", "_PhysicsProcess"],
            script.Events);
        Assert.Same(tree, script.TreeSeenFromReady);
        Assert.True(script.TextFeatureWasAvailable);
    }

    private sealed class ScriptNode : Electron2D.Node
    {
        private readonly List<string> _events = new();

        public IReadOnlyList<string> Events => _events;

        public Electron2D.SceneTree? TreeSeenFromReady { get; private set; }

        public bool TextFeatureWasAvailable { get; private set; }

        public override void _EnterTree()
        {
            _events.Add(nameof(_EnterTree));
        }

        public override void _Ready()
        {
            _events.Add(nameof(_Ready));
            TreeSeenFromReady = GetTree();
            TextFeatureWasAvailable = Electron2D.RenderingServer.HasFeature(
                Electron2D.RenderingServer.RenderingFeature.Text);
        }

        public override void _Process(double delta)
        {
            _events.Add(nameof(_Process));
        }

        public override void _PhysicsProcess(double delta)
        {
            _events.Add(nameof(_PhysicsProcess));
        }
    }
}
