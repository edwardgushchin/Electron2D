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

[Collection(PhysicsServer2DCollection.Name)]
public sealed class FixedPhysicsStepAndRigidBodyMotionTests
{
    private const double FixedDelta = 1d / 60d;

    [Fact]
    public void PhysicsFrameAccumulatesElapsedTimeAndRunsFixedSixtyHertzTicks()
    {
        var tree = new Electron2D.SceneTree();
        var recorder = new PhysicsDeltaRecorder();
        tree.Root.AddChild(recorder);

        tree.PhysicsFrame(1d / 120d);

        Assert.Empty(recorder.Deltas);

        tree.PhysicsFrame(1d / 120d);
        tree.PhysicsFrame(1d / 30d);

        Assert.Equal(3, recorder.Deltas.Count);
        Assert.All(recorder.Deltas, delta => Assert.Equal(FixedDelta, delta, precision: 12));
    }

    [Fact]
    public void RigidBody2DUsesSweptBoundsSoFastBodyStopsBeforeStaticBody()
    {
        var tree = new Electron2D.SceneTree();
        var wall = CreateStaticBody("Wall", new Electron2D.Vector2(50f, 0f), new Electron2D.Vector2(10f, 100f));
        var body = CreateRigidBody("Mover", Electron2D.Vector2.Zero, new Electron2D.Vector2(10f, 10f));
        body.LinearVelocity = new Electron2D.Vector2(6000f, 0f);

        tree.Root.AddChild(wall);
        tree.Root.AddChild(body);

        tree.PhysicsFrame(FixedDelta);

        AssertVectorEqual(new Electron2D.Vector2(40f, 0f), body.Position);
        AssertVectorEqual(new Electron2D.Vector2(0f, 0f), body.LinearVelocity);
    }

    [Fact]
    public void OneWayCollisionBlocksFromAboveAndAllowsMotionFromBelow()
    {
        var fallingTree = new Electron2D.SceneTree();
        var platform = CreateStaticBody(
            "Platform",
            new Electron2D.Vector2(0f, 50f),
            new Electron2D.Vector2(120f, 10f),
            oneWay: true);
        var falling = CreateRigidBody("Falling", Electron2D.Vector2.Zero, new Electron2D.Vector2(10f, 10f));
        falling.LinearVelocity = new Electron2D.Vector2(0f, 6000f);
        fallingTree.Root.AddChild(platform);
        fallingTree.Root.AddChild(falling);

        fallingTree.PhysicsFrame(FixedDelta);

        AssertVectorEqual(new Electron2D.Vector2(0f, 40f), falling.Position);
        AssertVectorEqual(new Electron2D.Vector2(0f, 0f), falling.LinearVelocity);

        var risingTree = new Electron2D.SceneTree();
        var oneWayPlatform = CreateStaticBody(
            "Platform",
            new Electron2D.Vector2(0f, 50f),
            new Electron2D.Vector2(120f, 10f),
            oneWay: true);
        var rising = CreateRigidBody("Rising", new Electron2D.Vector2(0f, 70f), new Electron2D.Vector2(10f, 10f));
        rising.LinearVelocity = new Electron2D.Vector2(0f, -6000f);
        risingTree.Root.AddChild(oneWayPlatform);
        risingTree.Root.AddChild(rising);

        risingTree.PhysicsFrame(FixedDelta);

        AssertVectorEqual(new Electron2D.Vector2(0f, -30f), rising.Position);
        AssertVectorEqual(new Electron2D.Vector2(0f, -6000f), rising.LinearVelocity);
    }

    [Fact]
    public void QueueFreeDuringFirstFixedTickSkipsQueuedBodyInRemainingTicksAndKeepsSiblingRunning()
    {
        var tree = new Electron2D.SceneTree();
        var deleting = new QueueFreeOnFirstPhysicsBody();
        var sibling = new PhysicsStepCounter();
        tree.Root.AddChild(deleting);
        tree.Root.AddChild(sibling);

        tree.PhysicsFrame(1d / 30d);

        Assert.Equal(1, deleting.PhysicsCalls);
        Assert.Equal(2, sibling.PhysicsCalls);
        Assert.False(Electron2D.ElectronObject.IsInstanceValid(deleting));
        Assert.True(Electron2D.ElectronObject.IsInstanceValid(sibling));
    }

    [Fact]
    public void RigidBody2DHonorsFreezeSleepingLockRotationAndFreeMotionWithoutShapes()
    {
        var tree = new Electron2D.SceneTree();
        var frozen = new Electron2D.RigidBody2D
        {
            Name = "Frozen",
            Freeze = true,
            LinearVelocity = new Electron2D.Vector2(60f, 0f),
            AngularVelocity = 60f
        };
        var sleeping = new Electron2D.RigidBody2D
        {
            Name = "Sleeping",
            Position = new Electron2D.Vector2(0f, 10f),
            Sleeping = true,
            LinearVelocity = new Electron2D.Vector2(0f, 60f),
            AngularVelocity = 60f
        };
        var moving = new Electron2D.RigidBody2D
        {
            Name = "Moving",
            Position = new Electron2D.Vector2(0f, 20f),
            LinearVelocity = new Electron2D.Vector2(60f, 0f),
            AngularVelocity = 60f
        };
        var rotationLocked = new Electron2D.RigidBody2D
        {
            Name = "RotationLocked",
            Position = new Electron2D.Vector2(0f, 30f),
            LockRotation = true,
            AngularVelocity = 60f
        };
        tree.Root.AddChild(frozen);
        tree.Root.AddChild(sleeping);
        tree.Root.AddChild(moving);
        tree.Root.AddChild(rotationLocked);

        tree.PhysicsFrame(FixedDelta);

        AssertVectorEqual(Electron2D.Vector2.Zero, frozen.Position);
        Assert.Equal(0f, frozen.Rotation);
        AssertVectorEqual(new Electron2D.Vector2(0f, 10f), sleeping.Position);
        Assert.Equal(0f, sleeping.Rotation);
        AssertVectorEqual(new Electron2D.Vector2(1f, 20f), moving.Position);
        Assert.Equal(1f, moving.Rotation);
        Assert.Equal(0f, rotationLocked.Rotation);
    }

    private static Electron2D.StaticBody2D CreateStaticBody(
        string name,
        Electron2D.Vector2 position,
        Electron2D.Vector2 size,
        bool oneWay = false)
    {
        var body = new Electron2D.StaticBody2D
        {
            Name = name,
            Position = position
        };
        body.AddChild(CreateShape(size, oneWay));
        return body;
    }

    private static Electron2D.RigidBody2D CreateRigidBody(
        string name,
        Electron2D.Vector2 position,
        Electron2D.Vector2 size)
    {
        var body = new Electron2D.RigidBody2D
        {
            Name = name,
            Position = position
        };
        body.AddChild(CreateShape(size));
        return body;
    }

    private static Electron2D.CollisionShape2D CreateShape(Electron2D.Vector2 size, bool oneWay = false)
    {
        return new Electron2D.CollisionShape2D
        {
            Shape = new Electron2D.RectangleShape2D { Size = size },
            OneWayCollision = oneWay
        };
    }

    private static void AssertVectorEqual(Electron2D.Vector2 expected, Electron2D.Vector2 actual)
    {
        Assert.True(actual.IsEqualApprox(expected), $"Expected {expected}, actual {actual}.");
    }

    private sealed class PhysicsDeltaRecorder : Electron2D.Node
    {
        public List<double> Deltas { get; } = new();

        public override void _PhysicsProcess(double delta)
        {
            Deltas.Add(delta);
        }
    }

    private sealed class QueueFreeOnFirstPhysicsBody : Electron2D.RigidBody2D
    {
        public int PhysicsCalls { get; private set; }

        public override void _PhysicsProcess(double delta)
        {
            PhysicsCalls++;
            QueueFree();
        }
    }

    private sealed class PhysicsStepCounter : Electron2D.RigidBody2D
    {
        public int PhysicsCalls { get; private set; }

        public override void _PhysicsProcess(double delta)
        {
            PhysicsCalls++;
        }
    }
}
