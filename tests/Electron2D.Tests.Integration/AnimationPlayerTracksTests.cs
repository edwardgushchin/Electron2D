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

public sealed class AnimationPlayerTracksTests
{
    [Fact]
    public void AnimationStoresValueTracksAndInterpolatesDiscreteAndLinearValues()
    {
        var animation = new Electron2D.Animation { Length = 2d };

        var positionTrack = animation.AddTrack(Electron2D.Animation.TrackTypeEnum.Value);
        animation.TrackSetPath(positionTrack, "Target:Position");
        animation.TrackInsertKey(positionTrack, 0d, new Electron2D.Vector2(0f, 0f));
        animation.TrackInsertKey(positionTrack, 2d, new Electron2D.Vector2(10f, 4f));

        var stateTrack = animation.AddTrack(Electron2D.Animation.TrackTypeEnum.Value);
        animation.TrackSetPath(stateTrack, "Target:Name");
        animation.TrackSetInterpolationType(stateTrack, Electron2D.Animation.InterpolationTypeEnum.Nearest);
        animation.TrackInsertKey(stateTrack, 0d, "idle");
        animation.TrackInsertKey(stateTrack, 1d, "run");

        Assert.Equal(2, animation.GetTrackCount());
        Assert.Equal(new Electron2D.NodePath("Target:Position"), animation.TrackGetPath(positionTrack));
        Assert.Equal(2, animation.TrackGetKeyCount(positionTrack));
        Assert.Equal(new Electron2D.Vector2(5f, 2f), animation.ValueTrackInterpolate(positionTrack, 1d).AsVector2());
        Assert.Equal("idle", animation.ValueTrackInterpolate(stateTrack, 0.5d).AsString());
        Assert.Equal("run", animation.ValueTrackInterpolate(stateTrack, 1d).AsString());
    }

    [Fact]
    public void AnimationLibraryManagesAnimationsWithStableNames()
    {
        var library = new Electron2D.AnimationLibrary();
        var idle = new Electron2D.Animation { Length = 0.25d };
        var run = new Electron2D.Animation { Length = 0.5d };

        Assert.Equal(Electron2D.Error.Ok, library.AddAnimation("run", run));
        Assert.Equal(Electron2D.Error.Ok, library.AddAnimation("idle", idle));
        Assert.Equal(Electron2D.Error.AlreadyExists, library.AddAnimation("run", new Electron2D.Animation()));
        Assert.True(library.HasAnimation("idle"));
        Assert.Same(run, library.GetAnimation("run"));
        Assert.Equal([new Electron2D.StringName("idle"), new Electron2D.StringName("run")], library.GetAnimationList());

        library.RenameAnimation("run", "walk");
        Assert.False(library.HasAnimation("run"));
        Assert.Same(run, library.GetAnimation("walk"));

        library.RemoveAnimation("idle");
        Assert.Equal([new Electron2D.StringName("walk")], library.GetAnimationList());
    }

    [Fact]
    public void AnimationPlayerAppliesInterpolatedAndDiscreteValueTracksDuringSceneProcess()
    {
        var tree = new Electron2D.SceneTree();
        var target = new Electron2D.Node2D { Name = "Target" };
        var player = new Electron2D.AnimationPlayer { Name = "Player" };
        var library = new Electron2D.AnimationLibrary();
        var animation = new Electron2D.Animation { Length = 1d };

        try
        {
            var positionTrack = animation.AddTrack(Electron2D.Animation.TrackTypeEnum.Value);
            animation.TrackSetPath(positionTrack, "Target:Position");
            animation.TrackInsertKey(positionTrack, 0d, Electron2D.Vector2.Zero);
            animation.TrackInsertKey(positionTrack, 1d, new Electron2D.Vector2(10f, 20f));

            var rotationTrack = animation.AddTrack(Electron2D.Animation.TrackTypeEnum.Value);
            animation.TrackSetPath(rotationTrack, "Target:RotationDegrees");
            animation.TrackSetInterpolationType(rotationTrack, Electron2D.Animation.InterpolationTypeEnum.Nearest);
            animation.TrackInsertKey(rotationTrack, 0d, 0f);
            animation.TrackInsertKey(rotationTrack, 1d, 90f);

            Assert.Equal(Electron2D.Error.Ok, library.AddAnimation("move", animation));
            Assert.Equal(Electron2D.Error.Ok, player.AddAnimationLibrary("", library));

            tree.Root.AddChild(target);
            tree.Root.AddChild(player);

            player.Play("move");
            Assert.True(player.IsPlaying());
            Assert.Equal(Electron2D.Vector2.Zero, target.Position);

            tree.ProcessFrame(0.25d);
            Assert.Equal(new Electron2D.Vector2(2.5f, 5f), target.Position);
            Assert.Equal(0f, target.RotationDegrees);

            tree.ProcessFrame(0.75d);
            Assert.Equal(new Electron2D.Vector2(10f, 20f), target.Position);
            Assert.Equal(90f, target.RotationDegrees);
            Assert.False(player.IsPlaying());
        }
        finally
        {
            tree.Root.Free();
        }
    }

    [Fact]
    public void AnimationPlayerCallsMethodTracksOnceQueuesPlaybackAndEmitsCompletionSignal()
    {
        var tree = new Electron2D.SceneTree();
        var receiver = new AnimationMethodReceiver { Name = "Receiver" };
        var player = new Electron2D.AnimationPlayer { Name = "Player" };
        var library = new Electron2D.AnimationLibrary();
        var finished = new List<Electron2D.StringName>();

        try
        {
            var first = new Electron2D.Animation { Length = 0.5d };
            var firstMethodTrack = first.AddTrack(Electron2D.Animation.TrackTypeEnum.Method);
            first.TrackSetPath(firstMethodTrack, "Receiver");
            first.MethodTrackInsertKey(firstMethodTrack, 0.25d, "Record", [Electron2D.Variant.From("first")]);

            var second = new Electron2D.Animation { Length = 0.25d };
            var secondMethodTrack = second.AddTrack(Electron2D.Animation.TrackTypeEnum.Method);
            second.TrackSetPath(secondMethodTrack, "Receiver");
            second.MethodTrackInsertKey(secondMethodTrack, 0.1d, "Record", [Electron2D.Variant.From("second")]);

            Assert.Equal(Electron2D.Error.Ok, library.AddAnimation("first", first));
            Assert.Equal(Electron2D.Error.Ok, library.AddAnimation("second", second));
            Assert.Equal(Electron2D.Error.Ok, player.AddAnimationLibrary("", library));
            Assert.Equal(Electron2D.Error.Ok, player.Connect("animation_finished", Electron2D.Callable.From<Electron2D.StringName>(finished.Add)));

            tree.Root.AddChild(receiver);
            tree.Root.AddChild(player);

            player.Play("first");
            player.Queue("second");

            tree.ProcessFrame(0.25d);
            Assert.Equal(["first"], receiver.Calls);
            Assert.Empty(finished);

            tree.ProcessFrame(0.25d);
            Assert.Equal(["first"], receiver.Calls);
            Assert.Equal([new Electron2D.StringName("first")], finished);
            Assert.True(player.IsPlaying());
            Assert.Equal(new Electron2D.StringName("second"), player.CurrentAnimation);

            tree.ProcessFrame(0.1d);
            Assert.Equal(["first", "second"], receiver.Calls);

            tree.ProcessFrame(0.15d);
            Assert.False(player.IsPlaying());
            Assert.Equal([new Electron2D.StringName("first"), new Electron2D.StringName("second")], finished);
        }
        finally
        {
            tree.Root.Free();
        }
    }

    private sealed class AnimationMethodReceiver : Electron2D.Node
    {
        public List<string> Calls { get; } = new();

        public void Record(string value)
        {
            Calls.Add(value);
        }
    }
}
