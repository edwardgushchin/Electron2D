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
public sealed class PhysicsMaterialStateTests
{
    [Fact]
    public void CollisionLayerAndMaskHelpersUseGodotLikeLayerNumbers()
    {
        var body = new Electron2D.StaticBody2D
        {
            CollisionLayer = 0,
            CollisionMask = 0
        };

        body.SetCollisionLayerValue(1, true);
        body.SetCollisionLayerValue(3, true);
        body.SetCollisionMaskValue(2, true);
        body.SetCollisionMaskValue(32, true);

        Assert.True(body.GetCollisionLayerValue(1));
        Assert.False(body.GetCollisionLayerValue(2));
        Assert.True(body.GetCollisionLayerValue(3));
        Assert.Equal(0b101u, body.CollisionLayer);
        Assert.True(body.GetCollisionMaskValue(2));
        Assert.True(body.GetCollisionMaskValue(32));
        Assert.Equal((1u << 1) | (1u << 31), body.CollisionMask);

        Assert.Contains("1..32", Assert.Throws<ArgumentOutOfRangeException>(
            () => body.GetCollisionLayerValue(0)).Message);
        Assert.Contains("1..32", Assert.Throws<ArgumentOutOfRangeException>(
            () => body.SetCollisionMaskValue(33, true)).Message);
    }

    [Fact]
    public void PhysicsMaterialValidatesAndRoundTripsThroughAotSafeSerialization()
    {
        var material = new Electron2D.PhysicsMaterial
        {
            Friction = 0.25f,
            Bounce = 0.75f,
            Rough = true,
            Absorbent = true
        };

        Assert.Contains(nameof(Electron2D.PhysicsMaterial.Friction), Assert.Throws<ArgumentOutOfRangeException>(
            () => { material.Friction = -0.01f; }).Message);
        Assert.Contains(nameof(Electron2D.PhysicsMaterial.Bounce), Assert.Throws<ArgumentOutOfRangeException>(
            () => { material.Bounce = float.NaN; }).Message);

        var document = Electron2D.ResourceObjectSerializer.Capture(material, "res://materials/bouncy.e2res");
        var serialized = Electron2D.SerializedResourceTextSerializer.Serialize(document);
        var parsed = Electron2D.SerializedResourceTextSerializer.Deserialize(serialized);
        var restored = Assert.IsType<Electron2D.PhysicsMaterial>(Electron2D.ResourceObjectSerializer.Instantiate(parsed));

        Assert.Equal(0.25f, restored.Friction);
        Assert.Equal(0.75f, restored.Bounce);
        Assert.True(restored.Rough);
        Assert.True(restored.Absorbent);
    }

    [Fact]
    public void PhysicsFrameSynchronizesCollisionFilterMaterialGravityAndSleepingState()
    {
        var backend = new RecordingPhysicsServer2DBackend();
        Electron2D.PhysicsServer2D.SetBackend(backend);
        var tree = new Electron2D.SceneTree();
        var material = new Electron2D.PhysicsMaterial
        {
            Friction = 0.4f,
            Bounce = 0.2f,
            Rough = true,
            Absorbent = true
        };
        var body = new Electron2D.RigidBody2D
        {
            CollisionLayer = 0b0011u,
            CollisionMask = 0b0101u,
            PhysicsMaterialOverride = material,
            GravityScale = -2f,
            Sleeping = true,
            CanSleep = false
        };

        try
        {
            tree.Root.AddChild(body);
            tree.PhysicsFrame(1d / 60d);

            var rid = body.GetRid();
            Assert.Equal(new Electron2D.PhysicsCollisionFilter(0b0011u, 0b0101u), backend.CollisionFilters[rid]);

            var state = backend.BodyStates[rid];
            Assert.Equal(new Electron2D.PhysicsMaterialState(0.4f, 0.2f, Rough: true, Absorbent: true), state.MaterialOverride);
            Assert.NotNull(state.RigidBody);
            Assert.Equal(-2f, state.RigidBody.Value.GravityScale);
            Assert.True(state.RigidBody.Value.Sleeping);
            Assert.False(state.RigidBody.Value.CanSleep);
        }
        finally
        {
            ResetBackend();
        }
    }

    private static void ResetBackend()
    {
        Electron2D.PhysicsServer2D.SetBackend(new Electron2D.ManagedPhysicsServer2DBackend());
    }

    private sealed class RecordingPhysicsServer2DBackend : Electron2D.IPhysicsServer2DBackend
    {
        private readonly HashSet<Electron2D.Rid> liveRids = [];
        private long nextRid = 700L;

        public string Name => "physics-material-recording";

        public Dictionary<Electron2D.Rid, Electron2D.PhysicsCollisionFilter> CollisionFilters { get; } = [];

        public Dictionary<Electron2D.Rid, Electron2D.PhysicsBody2DState> BodyStates { get; } = [];

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
            return NextRid();
        }

        public Electron2D.Rid BodyCreate(Electron2D.PhysicsBodyKind bodyKind)
        {
            _ = bodyKind;
            return NextRid();
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
            _ = rid;
            _ = transform;
        }

        public void CollisionObjectSetCollisionFilter(Electron2D.Rid rid, Electron2D.PhysicsCollisionFilter filter)
        {
            CollisionFilters[rid] = filter;
        }

        public void BodySetState(Electron2D.Rid rid, Electron2D.PhysicsBody2DState state)
        {
            BodyStates[rid] = state;
        }

        public void FreeRid(Electron2D.Rid rid)
        {
            liveRids.Remove(rid);
            CollisionFilters.Remove(rid);
            BodyStates.Remove(rid);
        }

        public int GetProcessInfo(Electron2D.PhysicsServer2D.ProcessInfo processInfo)
        {
            return processInfo == Electron2D.PhysicsServer2D.ProcessInfo.ActiveObjects ? liveRids.Count : 0;
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
