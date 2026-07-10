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

public sealed class TweenTests
{
    [Fact]
    public void SceneTreeCreateTweenInterpolatesPropertyWithEasingDuringProcess()
    {
        var tree = new Electron2D.SceneTree();
        var target = new Electron2D.Node2D { Name = "Target" };

        try
        {
            tree.Root.AddChild(target);

            var tween = tree.CreateTween();
            tween.SetTrans(Electron2D.Tween.TransitionType.Quad);
            tween.SetEase(Electron2D.Tween.EaseType.In);
            tween.TweenProperty(target, "Position", new Electron2D.Vector2(8f, 4f), 1d);

            tree.ProcessFrame(0.5d);

            Assert.True(tween.IsRunning());
            Assert.Equal(new Electron2D.Vector2(2f, 1f), target.Position);

            tree.ProcessFrame(0.5d);

            Assert.False(tween.IsValid());
            Assert.Equal(new Electron2D.Vector2(8f, 4f), target.Position);
        }
        finally
        {
            tree.Root.Free();
        }
    }

    [Fact]
    public void NodeCreateTweenBindsTweenAndStopsWhenBoundNodeLeavesTree()
    {
        var tree = new Electron2D.SceneTree();
        var owner = new Electron2D.Node2D { Name = "Owner" };
        var target = new Electron2D.Node2D { Name = "Target" };

        try
        {
            tree.Root.AddChild(owner);
            owner.AddChild(target);

            var tween = owner.CreateTween();
            tween.TweenProperty(target, "Position", new Electron2D.Vector2(10f, 0f), 1d);

            tree.ProcessFrame(0.25d);
            Assert.Equal(new Electron2D.Vector2(2.5f, 0f), target.Position);

            tree.Root.RemoveChild(owner);
            tree.ProcessFrame(0.75d);

            Assert.False(tween.IsRunning());
            Assert.True(tween.IsValid());
            Assert.Equal(new Electron2D.Vector2(2.5f, 0f), target.Position);
        }
        finally
        {
            owner.Free();
            tree.Root.Free();
        }
    }

    [Fact]
    public void PausePlayStopAndCustomStepUseDeterministicElapsedTime()
    {
        var tree = new Electron2D.SceneTree();
        var target = new Electron2D.Node2D { Name = "Target" };

        try
        {
            tree.Root.AddChild(target);

            var tween = tree.CreateTween();
            tween.TweenProperty(target, "Position:X", 10f, 1d);

            tree.ProcessFrame(0.25d);
            Assert.Equal(2.5f, target.Position.X);
            Assert.Equal(0.25d, tween.GetTotalElapsedTime(), 3);

            tween.Pause();
            tree.ProcessFrame(0.25d);
            Assert.Equal(2.5f, target.Position.X);
            Assert.Equal(0.25d, tween.GetTotalElapsedTime(), 3);

            Assert.True(tween.CustomStep(0.25d));
            Assert.Equal(5f, target.Position.X);
            Assert.Equal(0.5d, tween.GetTotalElapsedTime(), 3);

            tween.Play();
            tree.ProcessFrame(0.25d);
            Assert.Equal(7.5f, target.Position.X);

            tween.Stop();
            Assert.False(tween.IsRunning());
            Assert.Equal(0d, tween.GetTotalElapsedTime(), 3);

            tween.Play();
            tree.ProcessFrame(0.5d);
            Assert.Equal(5f, target.Position.X);
        }
        finally
        {
            tree.Root.Free();
        }
    }

    [Fact]
    public void TweenPropertyUpdatesNestedClrObjectPath()
    {
        var tree = new Electron2D.SceneTree();
        var target = new NestedTweenTarget { Name = "Target" };

        try
        {
            tree.Root.AddChild(target);

            var tween = tree.CreateTween();
            tween.TweenProperty(target, "State:Alpha", 6f, 1d);

            tree.ProcessFrame(0.5d);
            Assert.Equal(4f, target.State.Alpha);

            tree.ProcessFrame(0.5d);
            Assert.Equal(6f, target.State.Alpha);
        }
        finally
        {
            tree.Root.Free();
        }
    }

    [Fact]
    public void CompletionCallbacksSignalsAndStepSignalsRunAfterSequence()
    {
        var tree = new Electron2D.SceneTree();
        var target = new Electron2D.Node2D { Name = "Target" };
        var events = new List<string>();
        var steps = new List<long>();

        try
        {
            tree.Root.AddChild(target);

            var tween = tree.CreateTween();
            Assert.Equal(Electron2D.Error.Ok, tween.Connect("finished", Electron2D.Callable.From(() => events.Add("finished"))));
            Assert.Equal(Electron2D.Error.Ok, tween.Connect("step_finished", Electron2D.Callable.From<long>(steps.Add)));

            tween.TweenProperty(target, "Position", new Electron2D.Vector2(4f, 0f), 0.25d);
            tween.TweenInterval(0.25d);
            tween.TweenCallback(Electron2D.Callable.From(() => events.Add("callback")));

            tree.ProcessFrame(0.25d);
            Assert.Equal([0L], steps);
            Assert.Empty(events);

            tree.ProcessFrame(0.25d);
            Assert.Equal([0L, 1L], steps);
            Assert.Empty(events);

            tree.ProcessFrame(0d);

            Assert.Equal([0L, 1L, 2L], steps);
            Assert.Equal(["callback", "finished"], events);
            Assert.False(tween.IsValid());
        }
        finally
        {
            tree.Root.Free();
        }
    }

    [Fact]
    public void KillCancelsTweenWithoutCompletionCallbacks()
    {
        var tree = new Electron2D.SceneTree();
        var target = new Electron2D.Node2D { Name = "Target" };
        var events = new List<string>();

        try
        {
            tree.Root.AddChild(target);

            var tween = tree.CreateTween();
            tween.Connect("finished", Electron2D.Callable.From(() => events.Add("finished")));
            tween.TweenProperty(target, "Position", new Electron2D.Vector2(10f, 0f), 1d);
            tween.TweenCallback(Electron2D.Callable.From(() => events.Add("callback")));

            tree.ProcessFrame(0.25d);
            tween.Kill();
            tree.ProcessFrame(1d);

            Assert.False(tween.IsValid());
            Assert.Equal(new Electron2D.Vector2(2.5f, 0f), target.Position);
            Assert.Empty(events);
        }
        finally
        {
            tree.Root.Free();
        }
    }

    private sealed class NestedTweenTarget : Electron2D.Node2D
    {
        public NestedTweenState State { get; set; } = new();
    }

    private sealed class NestedTweenState
    {
        public float Alpha { get; set; } = 2f;
    }
}
