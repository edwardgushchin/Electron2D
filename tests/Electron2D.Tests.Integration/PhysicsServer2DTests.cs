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
using System.Reflection;
using Xunit;

namespace Electron2D.Tests.Integration;

[Collection(PhysicsServer2DCollection.Name)]
public sealed class PhysicsServer2DTests
{
    [Fact]
    public void PhysicsServer2DCreatesAndFreesServerRids()
    {
        ResetBackend();

        var space = Electron2D.PhysicsServer2D.SpaceCreate();
        var area = Electron2D.PhysicsServer2D.AreaCreate();
        var body = Electron2D.PhysicsServer2D.BodyCreate();
        var joint = Electron2D.PhysicsServer2D.JointCreate();
        var circle = Electron2D.PhysicsServer2D.CircleShapeCreate();

        Assert.All(new[] { space, area, body, joint, circle }, rid => Assert.True(rid.IsValid()));
        Assert.Equal(5, new HashSet<Electron2D.Rid> { space, area, body, joint, circle }.Count);

        Electron2D.PhysicsServer2D.FreeRid(circle);

        Assert.Throws<ArgumentException>(() => Electron2D.PhysicsServer2D.ShapeGetType(circle));
    }

    [Fact]
    public void SpaceStateAndParametersRoundTripThroughServerBoundary()
    {
        ResetBackend();

        var space = Electron2D.PhysicsServer2D.SpaceCreate();

        Electron2D.PhysicsServer2D.SpaceSetActive(space, active: false);
        Electron2D.PhysicsServer2D.SpaceSetParam(
            space,
            Electron2D.PhysicsServer2D.SpaceParameter.SolverIterations,
            12.5f);

        Assert.False(Electron2D.PhysicsServer2D.SpaceIsActive(space));
        Assert.Equal(
            12.5f,
            Electron2D.PhysicsServer2D.SpaceGetParam(
                space,
                Electron2D.PhysicsServer2D.SpaceParameter.SolverIterations));
    }

    [Fact]
    public void ShapeCreationPreservesElectron2DShapeType()
    {
        ResetBackend();

        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.WorldBoundary,
            Electron2D.PhysicsServer2D.ShapeGetType(Electron2D.PhysicsServer2D.WorldBoundaryShapeCreate()));
        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.SeparationRay,
            Electron2D.PhysicsServer2D.ShapeGetType(Electron2D.PhysicsServer2D.SeparationRayShapeCreate()));
        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.Segment,
            Electron2D.PhysicsServer2D.ShapeGetType(Electron2D.PhysicsServer2D.SegmentShapeCreate()));
        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.Circle,
            Electron2D.PhysicsServer2D.ShapeGetType(Electron2D.PhysicsServer2D.CircleShapeCreate()));
        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.Rectangle,
            Electron2D.PhysicsServer2D.ShapeGetType(Electron2D.PhysicsServer2D.RectangleShapeCreate()));
        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.Capsule,
            Electron2D.PhysicsServer2D.ShapeGetType(Electron2D.PhysicsServer2D.CapsuleShapeCreate()));
        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.ConvexPolygon,
            Electron2D.PhysicsServer2D.ShapeGetType(Electron2D.PhysicsServer2D.ConvexPolygonShapeCreate()));
        Assert.Equal(
            Electron2D.PhysicsServer2D.ShapeType.ConcavePolygon,
            Electron2D.PhysicsServer2D.ShapeGetType(Electron2D.PhysicsServer2D.ConcavePolygonShapeCreate()));
    }

    [Fact]
    public void PhysicsServer2DSwitchesInternalBackend()
    {
        var backend = new RecordingPhysicsServer2DBackend();
        Electron2D.PhysicsServer2D.SetBackend(backend);

        try
        {
            Electron2D.PhysicsServer2D.SetActive(false);

            var space = Electron2D.PhysicsServer2D.SpaceCreate();
            Electron2D.PhysicsServer2D.SpaceSetActive(space, active: true);
            Electron2D.PhysicsServer2D.SpaceSetParam(
                space,
                Electron2D.PhysicsServer2D.SpaceParameter.ContactDefaultBias,
                0.42f);
            var shape = Electron2D.PhysicsServer2D.CircleShapeCreate();

            Assert.Equal("recording", Electron2D.PhysicsServer2D.CurrentBackendName);
            Assert.False(backend.Active);
            Assert.Equal(9001L, space.GetId());
            Assert.True(Electron2D.PhysicsServer2D.SpaceIsActive(space));
            Assert.Equal(
                0.42f,
                Electron2D.PhysicsServer2D.SpaceGetParam(
                    space,
                    Electron2D.PhysicsServer2D.SpaceParameter.ContactDefaultBias));
            Assert.Equal(Electron2D.PhysicsServer2D.ShapeType.Circle, Electron2D.PhysicsServer2D.ShapeGetType(shape));
            Assert.Equal(7, Electron2D.PhysicsServer2D.GetProcessInfo(Electron2D.PhysicsServer2D.ProcessInfo.ActiveObjects));
        }
        finally
        {
            ResetBackend();
        }
    }

    [Fact]
    public void PublicPhysicsApiDoesNotExposeBox2DOrBackendTypes()
    {
        var assembly = Assembly.Load("Electron2D");
        var exportedTypes = assembly.GetExportedTypes();

        Assert.Contains(exportedTypes, type => type.FullName == "Electron2D.PhysicsServer2D");
        Assert.Contains(exportedTypes, type => type.FullName == "Electron2D.PhysicsServer2D+SpaceParameter");
        Assert.Contains(exportedTypes, type => type.FullName == "Electron2D.PhysicsServer2D+ShapeType");
        Assert.DoesNotContain(exportedTypes, type => type.FullName == "Electron2D.IPhysicsServer2DBackend");
        Assert.DoesNotContain(exportedTypes, type => type.FullName == "Electron2D.ManagedPhysicsServer2DBackend");

        foreach (var type in exportedTypes)
        {
            Assert.DoesNotContain("Box2D", type.FullName, StringComparison.OrdinalIgnoreCase);

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            {
                AssertPublicTypeDoesNotExposeBox2D(method.ReturnType);
                foreach (var parameter in method.GetParameters())
                {
                    AssertPublicTypeDoesNotExposeBox2D(parameter.ParameterType);
                }
            }
        }
    }

    private static void ResetBackend()
    {
        Electron2D.PhysicsServer2D.SetBackend(new Electron2D.ManagedPhysicsServer2DBackend());
    }

    private static void AssertPublicTypeDoesNotExposeBox2D(Type type)
    {
        Assert.DoesNotContain("Box2D", type.FullName ?? type.Name, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RecordingPhysicsServer2DBackend : Electron2D.IPhysicsServer2DBackend
    {
        private readonly Dictionary<Electron2D.Rid, bool> spaces = new();
        private readonly Dictionary<(Electron2D.Rid Space, Electron2D.PhysicsServer2D.SpaceParameter Param), float> parameters = new();
        private readonly Dictionary<Electron2D.Rid, Electron2D.PhysicsServer2D.ShapeType> shapes = new();
        private long nextRid = 9000L;

        public string Name => "recording";

        public bool Active { get; private set; } = true;

        public void SetActive(bool active)
        {
            Active = active;
        }

        public Electron2D.Rid SpaceCreate()
        {
            var rid = NextRid();
            spaces.Add(rid, true);
            return rid;
        }

        public void SpaceSetActive(Electron2D.Rid space, bool active)
        {
            spaces[space] = active;
        }

        public bool SpaceIsActive(Electron2D.Rid space)
        {
            return spaces[space];
        }

        public void SpaceSetParam(Electron2D.Rid space, Electron2D.PhysicsServer2D.SpaceParameter param, float value)
        {
            parameters[(space, param)] = value;
        }

        public float SpaceGetParam(Electron2D.Rid space, Electron2D.PhysicsServer2D.SpaceParameter param)
        {
            return parameters[(space, param)];
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
            var rid = NextRid();
            shapes.Add(rid, type);
            return rid;
        }

        public Electron2D.PhysicsServer2D.ShapeType ShapeGetType(Electron2D.Rid shape)
        {
            return shapes[shape];
        }

        public void CollisionObjectSetTransform(Electron2D.Rid rid, Electron2D.Transform2D transform)
        {
            _ = rid;
            _ = transform;
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
            spaces.Remove(rid);
            shapes.Remove(rid);
        }

        public int GetProcessInfo(Electron2D.PhysicsServer2D.ProcessInfo processInfo)
        {
            return processInfo == Electron2D.PhysicsServer2D.ProcessInfo.ActiveObjects ? 7 : 0;
        }

        private Electron2D.Rid NextRid()
        {
            nextRid++;
            return new Electron2D.Rid(nextRid);
        }
    }
}
