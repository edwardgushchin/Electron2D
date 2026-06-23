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
public sealed class CharacterBody2DKinematicSolverTests
{
    private const double FixedDelta = 1d / 60d;
    private const float SafeMargin = 0.08f;

    [Fact]
    public void MoveAndCollideStopsBeforeStaticBodyAndReportsCollision()
    {
        var tree = new Electron2D.SceneTree();
        var wall = CreateStaticBody("Wall", new Electron2D.Vector2(50f, 0f), new Electron2D.Vector2(10f, 100f));
        var character = CreateCharacter("Character", Electron2D.Vector2.Zero, new Electron2D.Vector2(10f, 10f));
        tree.Root.AddChild(wall);
        tree.Root.AddChild(character);

        var collision = character.MoveAndCollide(new Electron2D.Vector2(100f, 0f));

        Assert.NotNull(collision);
        AssertVectorEqual(new Electron2D.Vector2(39.92f, 0f), character.Position);
        Assert.Same(wall, collision.GetCollider());
        Assert.Equal(wall.GetRid(), collision.GetColliderRid());
        Assert.Equal(0, collision.GetColliderShapeIndex());
        AssertVectorEqual(new Electron2D.Vector2(-1f, 0f), collision.GetNormal());
        AssertVectorEqual(new Electron2D.Vector2(39.92f, 0f), collision.GetTravel());
        AssertVectorEqual(new Electron2D.Vector2(60.08f, 0f), collision.GetRemainder());
    }

    [Fact]
    public void MoveAndCollideTestOnlyReturnsWouldBeCollisionWithoutMoving()
    {
        var tree = new Electron2D.SceneTree();
        var wall = CreateStaticBody("Wall", new Electron2D.Vector2(50f, 0f), new Electron2D.Vector2(10f, 100f));
        var character = CreateCharacter("Character", Electron2D.Vector2.Zero, new Electron2D.Vector2(10f, 10f));
        tree.Root.AddChild(wall);
        tree.Root.AddChild(character);

        var collision = character.MoveAndCollide(new Electron2D.Vector2(100f, 0f), testOnly: true);

        Assert.NotNull(collision);
        AssertVectorEqual(Electron2D.Vector2.Zero, character.Position);
        Assert.Same(wall, collision.GetCollider());
        AssertVectorEqual(new Electron2D.Vector2(39.92f, 0f), collision.GetTravel());
    }

    [Fact]
    public void MoveAndSlideDetectsFloorWallAndCeiling()
    {
        var floorTree = new Electron2D.SceneTree();
        var floor = CreateStaticBody("Floor", new Electron2D.Vector2(0f, 50f), new Electron2D.Vector2(120f, 10f));
        var falling = CreateCharacter("Falling", Electron2D.Vector2.Zero, new Electron2D.Vector2(10f, 10f));
        falling.Velocity = new Electron2D.Vector2(0f, 6000f);
        floorTree.Root.AddChild(floor);
        floorTree.Root.AddChild(falling);

        Assert.True(falling.MoveAndSlide());

        Assert.True(falling.IsOnFloor());
        Assert.True(falling.IsOnFloorOnly());
        Assert.False(falling.IsOnWall());
        AssertVectorEqual(new Electron2D.Vector2(0f, -1f), falling.GetFloorNormal());
        AssertVectorEqual(new Electron2D.Vector2(0f, 0f), falling.Velocity);
        AssertVectorEqual(new Electron2D.Vector2(0f, 39.92f), falling.Position);
        Assert.Equal(1, falling.GetSlideCollisionCount());
        Assert.Same(falling.GetSlideCollision(0), falling.GetLastSlideCollision());
        AssertVectorEqual(new Electron2D.Vector2(0f, 39.92f), falling.GetLastMotion());
        AssertVectorEqual(new Electron2D.Vector2(0f, 39.92f), falling.GetPositionDelta());
        AssertVectorEqual(new Electron2D.Vector2(0f, 2395.2f), falling.GetRealVelocity());

        var wallTree = new Electron2D.SceneTree();
        var wall = CreateStaticBody("Wall", new Electron2D.Vector2(50f, 0f), new Electron2D.Vector2(10f, 100f));
        var walking = CreateCharacter("Walking", Electron2D.Vector2.Zero, new Electron2D.Vector2(10f, 10f));
        walking.Velocity = new Electron2D.Vector2(6000f, 0f);
        wallTree.Root.AddChild(wall);
        wallTree.Root.AddChild(walking);

        Assert.True(walking.MoveAndSlide());

        Assert.True(walking.IsOnWall());
        Assert.True(walking.IsOnWallOnly());
        AssertVectorEqual(new Electron2D.Vector2(-1f, 0f), walking.GetWallNormal());
        AssertVectorEqual(new Electron2D.Vector2(0f, 0f), walking.Velocity);

        var ceilingTree = new Electron2D.SceneTree();
        var ceiling = CreateStaticBody("Ceiling", new Electron2D.Vector2(0f, 50f), new Electron2D.Vector2(120f, 10f));
        var rising = CreateCharacter("Rising", new Electron2D.Vector2(0f, 70f), new Electron2D.Vector2(10f, 10f));
        rising.Velocity = new Electron2D.Vector2(0f, -6000f);
        ceilingTree.Root.AddChild(ceiling);
        ceilingTree.Root.AddChild(rising);

        Assert.True(rising.MoveAndSlide());

        Assert.True(rising.IsOnCeiling());
        Assert.True(rising.IsOnCeilingOnly());
        AssertVectorEqual(new Electron2D.Vector2(0f, 1f), rising.GetLastSlideCollision()!.GetNormal());
    }

    [Fact]
    public void FloorSnapAndPlatformVelocityKeepCharacterAttachedToMovingFloor()
    {
        var tree = new Electron2D.SceneTree();
        var platform = CreateStaticBody("Platform", new Electron2D.Vector2(0f, 50f), new Electron2D.Vector2(120f, 10f));
        platform.ConstantLinearVelocity = new Electron2D.Vector2(30f, 0f);
        var character = CreateCharacter("Character", new Electron2D.Vector2(0f, 38f), new Electron2D.Vector2(10f, 10f));
        character.FloorSnapLength = 4f;
        character.Velocity = new Electron2D.Vector2(60f, 0f);
        tree.Root.AddChild(platform);
        tree.Root.AddChild(character);

        Assert.True(character.MoveAndSlide());

        Assert.True(character.IsOnFloor());
        AssertVectorEqual(new Electron2D.Vector2(1f, 39.92f), character.Position);
        AssertVectorEqual(platform.ConstantLinearVelocity, character.GetPlatformVelocity());
    }

    [Fact]
    public void MoveAndSlideKeepsFloorContactAcrossRepeatedFrames()
    {
        var tree = new Electron2D.SceneTree();
        var platform = CreateStaticBody("Platform", new Electron2D.Vector2(0f, 50f), new Electron2D.Vector2(120f, 10f));
        var character = CreateCharacter("Character", new Electron2D.Vector2(0f, 39.92f), new Electron2D.Vector2(10f, 10f));
        character.FloorSnapLength = 4f;
        tree.Root.AddChild(platform);
        tree.Root.AddChild(character);

        for (var frame = 0; frame < 3; frame++)
        {
            character.Velocity = new Electron2D.Vector2(60f, 40f);

            Assert.True(character.MoveAndSlide());
            Assert.True(character.IsOnFloor());
            AssertVectorEqual(new Electron2D.Vector2(frame + 1f, 39.92f), character.Position);
        }
    }

    [Fact]
    public void MoveAndSlideClassifiesSegmentSlopeAsFloor()
    {
        var tree = new Electron2D.SceneTree();
        var slope = new Electron2D.StaticBody2D
        {
            Name = "Slope",
            Position = new Electron2D.Vector2(0f, 50f)
        };
        slope.AddChild(new Electron2D.CollisionShape2D
        {
            Shape = new Electron2D.SegmentShape2D
            {
                A = new Electron2D.Vector2(-50f, 0f),
                B = new Electron2D.Vector2(50f, 50f)
            }
        });
        var character = CreateCharacter("Character", Electron2D.Vector2.Zero, new Electron2D.Vector2(10f, 10f));
        character.Velocity = new Electron2D.Vector2(0f, 6000f);
        tree.Root.AddChild(slope);
        tree.Root.AddChild(character);

        Assert.True(character.MoveAndSlide());

        Assert.True(character.IsOnFloor());
        Assert.InRange(character.GetFloorAngle(), 0.46f, 0.47f);
        Assert.True(character.GetFloorNormal().Y < -0.8f);
    }

    private static Electron2D.StaticBody2D CreateStaticBody(
        string name,
        Electron2D.Vector2 position,
        Electron2D.Vector2 size)
    {
        var body = new Electron2D.StaticBody2D
        {
            Name = name,
            Position = position
        };
        body.AddChild(CreateShape(size));
        return body;
    }

    private static Electron2D.CharacterBody2D CreateCharacter(
        string name,
        Electron2D.Vector2 position,
        Electron2D.Vector2 size)
    {
        var body = new Electron2D.CharacterBody2D
        {
            Name = name,
            Position = position
        };
        body.AddChild(CreateShape(size));
        return body;
    }

    private static Electron2D.CollisionShape2D CreateShape(Electron2D.Vector2 size)
    {
        return new Electron2D.CollisionShape2D
        {
            Shape = new Electron2D.RectangleShape2D { Size = size }
        };
    }

    private static void AssertVectorEqual(Electron2D.Vector2 expected, Electron2D.Vector2 actual)
    {
        Assert.True(actual.IsEqualApprox(expected), $"Expected {expected}, actual {actual}.");
    }
}
