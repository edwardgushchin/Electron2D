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
public sealed class PhysicsNodeLifecycleTests
{
    [Fact]
    public void CollisionObjectsAllocateAndFreeServerRidsWithSceneTreeLifecycle()
    {
        var backend = new RecordingPhysicsServer2DBackend();
        Electron2D.PhysicsServer2D.SetBackend(backend);
        var tree = new Electron2D.SceneTree();
        var body = new Electron2D.StaticBody2D();

        try
        {
            Assert.Equal(default, body.GetRid());

            tree.Root.AddChild(body);
            var firstRid = body.GetRid();

            Assert.NotEqual(default, firstRid);
            Assert.True(backend.LiveRids.Contains(firstRid));
            Assert.Equal(1, Electron2D.PhysicsServer2D.GetProcessInfo(Electron2D.PhysicsServer2D.ProcessInfo.ActiveObjects));

            tree.Root.RemoveChild(body);

            Assert.Equal(default, body.GetRid());
            Assert.DoesNotContain(firstRid, backend.LiveRids);
            Assert.Equal(0, Electron2D.PhysicsServer2D.GetProcessInfo(Electron2D.PhysicsServer2D.ProcessInfo.ActiveObjects));

            tree.Root.AddChild(body);

            Assert.NotEqual(default, body.GetRid());
            Assert.NotEqual(firstRid, body.GetRid());
        }
        finally
        {
            ResetBackend();
        }
    }

    [Fact]
    public void StaticRigidCharacterAndAreaNodesUseExpectedPhysicsResourceKinds()
    {
        var backend = new RecordingPhysicsServer2DBackend();
        Electron2D.PhysicsServer2D.SetBackend(backend);
        var tree = new Electron2D.SceneTree();
        var staticBody = new Electron2D.StaticBody2D();
        var rigidBody = new Electron2D.RigidBody2D();
        var characterBody = new Electron2D.CharacterBody2D();
        var area = new Electron2D.Area2D();

        try
        {
            tree.Root.AddChild(staticBody);
            tree.Root.AddChild(rigidBody);
            tree.Root.AddChild(characterBody);
            tree.Root.AddChild(area);

            Assert.Equal(Electron2D.PhysicsBodyKind.Static, backend.BodyKinds[staticBody.GetRid()]);
            Assert.Equal(Electron2D.PhysicsBodyKind.Rigid, backend.BodyKinds[rigidBody.GetRid()]);
            Assert.Equal(Electron2D.PhysicsBodyKind.Character, backend.BodyKinds[characterBody.GetRid()]);
            Assert.Contains(area.GetRid(), backend.AreaRids);
        }
        finally
        {
            ResetBackend();
        }
    }

    [Fact]
    public void PhysicsFrameSynchronizesCollisionObjectGlobalTransform()
    {
        var backend = new RecordingPhysicsServer2DBackend();
        Electron2D.PhysicsServer2D.SetBackend(backend);
        var tree = new Electron2D.SceneTree();
        var parent = new Electron2D.Node2D
        {
            Position = new Electron2D.Vector2(10f, 20f)
        };
        var body = new Electron2D.RigidBody2D
        {
            Position = new Electron2D.Vector2(3f, 4f),
            Rotation = 0.5f,
            Scale = new Electron2D.Vector2(2f, 3f)
        };
        parent.AddChild(body);

        try
        {
            tree.Root.AddChild(parent);
            tree.PhysicsFrame(1d / 60d);

            Assert.Equal(body.GlobalTransform, backend.Transforms[body.GetRid()]);
        }
        finally
        {
            ResetBackend();
        }
    }

    [Fact]
    public void QueueFreeDuringPhysicsFrameFreesRidAfterTraversal()
    {
        var backend = new RecordingPhysicsServer2DBackend();
        Electron2D.PhysicsServer2D.SetBackend(backend);
        var tree = new Electron2D.SceneTree();
        var body = new QueueFreeOnPhysicsBody();

        try
        {
            tree.Root.AddChild(body);
            var rid = body.GetRid();

            tree.PhysicsFrame(1d / 60d);

            Assert.False(Electron2D.Object.IsInstanceValid(body));
            Assert.Equal(0, Electron2D.PhysicsServer2D.GetProcessInfo(Electron2D.PhysicsServer2D.ProcessInfo.ActiveObjects));
            Assert.DoesNotContain(rid, backend.LiveRids);
        }
        finally
        {
            ResetBackend();
        }
    }

    [Fact]
    public void CollisionShape2DStoresShapeAndFlags()
    {
        var shape = new TestShape2D();
        var collisionShape = new Electron2D.CollisionShape2D
        {
            Shape = shape,
            Disabled = true,
            OneWayCollision = true,
            OneWayCollisionMargin = 3.5f
        };

        Assert.Same(shape, collisionShape.Shape);
        Assert.True(collisionShape.Disabled);
        Assert.True(collisionShape.OneWayCollision);
        Assert.Equal(3.5f, collisionShape.OneWayCollisionMargin);
    }

    [Fact]
    public void RayCast2DStoresQueryPropertiesAndReturnsEmptyResultUntilQueryBackendExists()
    {
        var rayCast = new Electron2D.RayCast2D
        {
            Enabled = true,
            TargetPosition = new Electron2D.Vector2(0f, 64f),
            ExcludeParent = false,
            HitFromInside = true,
            CollideWithAreas = true,
            CollideWithBodies = false,
            CollisionMask = 3
        };

        rayCast.ForceRaycastUpdate();

        Assert.True(rayCast.Enabled);
        Assert.Equal(new Electron2D.Vector2(0f, 64f), rayCast.TargetPosition);
        Assert.False(rayCast.ExcludeParent);
        Assert.True(rayCast.HitFromInside);
        Assert.True(rayCast.CollideWithAreas);
        Assert.False(rayCast.CollideWithBodies);
        Assert.Equal(3u, rayCast.CollisionMask);
        Assert.False(rayCast.IsColliding());
        Assert.Null(rayCast.GetCollider());
        Assert.Equal(default, rayCast.GetColliderRid());
        Assert.Equal(0, rayCast.GetColliderShape());
        Assert.Equal(Electron2D.Vector2.Zero, rayCast.GetCollisionPoint());
        Assert.Equal(Electron2D.Vector2.Zero, rayCast.GetCollisionNormal());
    }

    private sealed class QueueFreeOnPhysicsBody : Electron2D.StaticBody2D
    {
        public override void _PhysicsProcess(double delta)
        {
            QueueFree();
        }
    }

    private sealed class TestShape2D : Electron2D.Shape2D
    {
        protected override Electron2D.Rid CreatePhysicsRid()
        {
            return Electron2D.PhysicsServer2D.RectangleShapeCreate();
        }
    }

    private static void ResetBackend()
    {
        Electron2D.PhysicsServer2D.SetBackend(new Electron2D.ManagedPhysicsServer2DBackend());
    }

    private sealed class RecordingPhysicsServer2DBackend : Electron2D.IPhysicsServer2DBackend
    {
        private readonly HashSet<Electron2D.Rid> liveRids = new();
        private long nextRid = 100L;

        public string Name => "physics-node-recording";

        public IReadOnlySet<Electron2D.Rid> LiveRids => liveRids;

        public Dictionary<Electron2D.Rid, Electron2D.PhysicsBodyKind> BodyKinds { get; } = new();

        public HashSet<Electron2D.Rid> AreaRids { get; } = new();

        public Dictionary<Electron2D.Rid, Electron2D.Transform2D> Transforms { get; } = new();

        public void SetActive(bool active)
        {
            _ = active;
        }

        public Electron2D.Rid SpaceCreate()
        {
            return NextRid();
        }

        public void SpaceSetActive(Electron2D.Rid space, bool active)
        {
            _ = space;
            _ = active;
        }

        public bool SpaceIsActive(Electron2D.Rid space)
        {
            _ = space;
            return true;
        }

        public void SpaceSetParam(Electron2D.Rid space, Electron2D.PhysicsServer2D.SpaceParameter param, float value)
        {
            _ = space;
            _ = param;
            _ = value;
        }

        public float SpaceGetParam(Electron2D.Rid space, Electron2D.PhysicsServer2D.SpaceParameter param)
        {
            _ = space;
            _ = param;
            return 0f;
        }

        public Electron2D.Rid AreaCreate()
        {
            var rid = NextRid();
            AreaRids.Add(rid);
            return rid;
        }

        public Electron2D.Rid BodyCreate(Electron2D.PhysicsBodyKind bodyKind)
        {
            var rid = NextRid();
            BodyKinds.Add(rid, bodyKind);
            return rid;
        }

        public Electron2D.Rid JointCreate()
        {
            return NextRid();
        }

        public Electron2D.Rid ShapeCreate(Electron2D.PhysicsServer2D.ShapeType type)
        {
            _ = type;
            return NextRid();
        }

        public Electron2D.PhysicsServer2D.ShapeType ShapeGetType(Electron2D.Rid shape)
        {
            _ = shape;
            return Electron2D.PhysicsServer2D.ShapeType.Custom;
        }

        public void CollisionObjectSetTransform(Electron2D.Rid rid, Electron2D.Transform2D transform)
        {
            Transforms[rid] = transform;
        }

        public void CollisionObjectSetCollisionFilter(Electron2D.Rid rid, Electron2D.PhysicsCollisionFilter filter)
        {
            _ = rid;
            _ = filter;
        }

        public void BodySetState(Electron2D.Rid rid, Electron2D.PhysicsBody2DState state)
        {
            _ = rid;
            _ = state;
        }

        public void FreeRid(Electron2D.Rid rid)
        {
            liveRids.Remove(rid);
            BodyKinds.Remove(rid);
            AreaRids.Remove(rid);
            Transforms.Remove(rid);
        }

        public int GetProcessInfo(Electron2D.PhysicsServer2D.ProcessInfo processInfo)
        {
            return processInfo == Electron2D.PhysicsServer2D.ProcessInfo.ActiveObjects
                ? liveRids.Count
                : 0;
        }

        private Electron2D.Rid NextRid()
        {
            nextRid++;
            var rid = new Electron2D.Rid(nextRid);
            liveRids.Add(rid);
            return rid;
        }
    }
}
