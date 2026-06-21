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

public sealed class SpriteFramesAnimatedSprite2DTests
{
    [Fact]
    public void SpriteFramesManagesAnimationsFramesSpeedAndLoopModes()
    {
        var frames = new Electron2D.SpriteFrames();
        var idle = new Electron2D.RuntimeTexture2D(4, 3, hasAlpha: false);
        var step = new Electron2D.RuntimeTexture2D(8, 6, hasAlpha: true);

        Assert.True(frames.HasAnimation("default"));

        frames.SetAnimationSpeed("default", 12f);
        frames.SetAnimationLoopMode("default", Electron2D.SpriteFrames.LoopModeEnum.Pingpong);
        frames.AddFrame("default", idle, duration: 2f);
        frames.AddAnimation("walk");
        frames.AddFrame("walk", step, duration: 0.5f);
        frames.AddFrame("walk", idle, duration: 1.5f, atPosition: 0);

        Assert.Equal([new Electron2D.StringName("default"), new Electron2D.StringName("walk")], frames.GetAnimationNames());
        Assert.Equal(12f, frames.GetAnimationSpeed("default"));
        Assert.Equal(Electron2D.SpriteFrames.LoopModeEnum.Pingpong, frames.GetAnimationLoopMode("default"));
        Assert.Equal(2, frames.GetFrameCount("walk"));
        Assert.Same(idle, frames.GetFrameTexture("walk", 0));
        Assert.Equal(1.5f, frames.GetFrameDuration("walk", 0));

        frames.SetAnimationLoop("walk", true);
        Assert.True(frames.GetAnimationLoop("walk"));
        Assert.Equal(Electron2D.SpriteFrames.LoopModeEnum.Linear, frames.GetAnimationLoopMode("walk"));

        frames.DuplicateAnimation("walk", "walk-copy");
        frames.RenameAnimation("walk-copy", "run");
        Assert.True(frames.HasAnimation("run"));
        Assert.Equal(2, frames.GetFrameCount("run"));

        frames.RemoveFrame("run", 1);
        Assert.Equal(1, frames.GetFrameCount("run"));
        frames.Clear("run");
        Assert.Equal(0, frames.GetFrameCount("run"));
        frames.RemoveAnimation("run");
        Assert.False(frames.HasAnimation("run"));
    }

    [Fact]
    public void AnimatedSprite2DAutoplayAdvancesFramesAndLoops()
    {
        var frames = CreateWalkFrames(loopMode: Electron2D.SpriteFrames.LoopModeEnum.Linear);
        var tree = new Electron2D.SceneTree();
        var sprite = new Electron2D.AnimatedSprite2D
        {
            SpriteFrames = frames,
            Animation = "walk",
            Autoplay = "walk"
        };

        try
        {
            tree.Root.AddChild(sprite);

            Assert.True(sprite.IsPlaying());
            Assert.Equal(0, sprite.Frame);
            Assert.Equal(1f, sprite.GetPlayingSpeed());

            tree.ProcessFrame(0.5d);
            Assert.Equal(1, sprite.Frame);
            Assert.Equal(0f, sprite.FrameProgress);

            tree.ProcessFrame(1.0d);
            Assert.Equal(0, sprite.Frame);
            Assert.True(sprite.IsPlaying());
        }
        finally
        {
            tree.Root.Free();
        }
    }

    [Fact]
    public void AnimatedSprite2DStopsAtNonLoopingEndAndCanPlayBackwards()
    {
        var frames = CreateWalkFrames(loopMode: Electron2D.SpriteFrames.LoopModeEnum.None);
        var tree = new Electron2D.SceneTree();
        var sprite = new Electron2D.AnimatedSprite2D
        {
            SpriteFrames = frames
        };

        try
        {
            tree.Root.AddChild(sprite);

            sprite.Play("walk");
            tree.ProcessFrame(1.0d);
            Assert.Equal(2, sprite.Frame);
            Assert.False(sprite.IsPlaying());

            sprite.PlayBackwards("walk");
            Assert.Equal(2, sprite.Frame);
            Assert.True(sprite.GetPlayingSpeed() < 0f);

            tree.ProcessFrame(1.0d);
            Assert.Equal(0, sprite.Frame);
            Assert.False(sprite.IsPlaying());

            sprite.Play("walk");
            tree.ProcessFrame(0.25d);
            sprite.Pause();
            var pausedFrame = sprite.Frame;
            var pausedProgress = sprite.FrameProgress;

            tree.ProcessFrame(1.0d);
            Assert.Equal(pausedFrame, sprite.Frame);
            Assert.Equal(pausedProgress, sprite.FrameProgress);

            sprite.Stop();
            Assert.False(sprite.IsPlaying());
            Assert.Equal(0, sprite.Frame);
            Assert.Equal(0f, sprite.FrameProgress);
        }
        finally
        {
            tree.Root.Free();
        }
    }

    [Fact]
    public void AnimatedSprite2DSubmissionUsesCurrentFrameAndUpdatedSpriteFramesResource()
    {
        var first = new Electron2D.RuntimeTexture2D(4, 3, hasAlpha: false);
        var second = new Electron2D.RuntimeTexture2D(8, 6, hasAlpha: true);
        var replacement = new Electron2D.RuntimeTexture2D(10, 5, hasAlpha: true);
        var frames = new Electron2D.SpriteFrames();
        frames.AddFrame("default", first);
        frames.AddFrame("default", second);

        var root = new Electron2D.Node();
        var sprite = new Electron2D.AnimatedSprite2D
        {
            Name = "animated",
            SpriteFrames = frames,
            Centered = false,
            Offset = new Electron2D.Vector2(2f, 3f),
            FlipH = true
        };
        root.AddChild(sprite);

        var firstCommand = Assert.Single(new Electron2D.CanvasSubmissionContext().BuildPlan(root).Commands);
        Assert.Same(first, firstCommand.Texture);
        Assert.Equal(new Electron2D.Rect2(0f, 0f, 4f, 3f), firstCommand.SourceRect);
        Assert.Equal(new Electron2D.Rect2(2f, 3f, 4f, 3f), firstCommand.DestinationRect);
        Assert.True(firstCommand.FlipH);

        sprite.SetFrameAndProgress(1, 0.25f);
        frames.SetFrame("default", 1, replacement);

        var updatedCommand = Assert.Single(new Electron2D.CanvasSubmissionContext().BuildPlan(root).Commands);
        Assert.Same(replacement, updatedCommand.Texture);
        Assert.Equal(new Electron2D.Rect2(0f, 0f, 10f, 5f), updatedCommand.SourceRect);
        Assert.Equal(new Electron2D.Rect2(2f, 3f, 10f, 5f), updatedCommand.DestinationRect);
    }

    private static Electron2D.SpriteFrames CreateWalkFrames(Electron2D.SpriteFrames.LoopModeEnum loopMode)
    {
        var frames = new Electron2D.SpriteFrames();
        frames.AddAnimation("walk");
        frames.SetAnimationSpeed("walk", 2f);
        frames.SetAnimationLoopMode("walk", loopMode);
        frames.AddFrame("walk", new Electron2D.RuntimeTexture2D(4, 4, hasAlpha: false));
        frames.AddFrame("walk", new Electron2D.RuntimeTexture2D(4, 4, hasAlpha: false));
        frames.AddFrame("walk", new Electron2D.RuntimeTexture2D(4, 4, hasAlpha: false));
        return frames;
    }
}
