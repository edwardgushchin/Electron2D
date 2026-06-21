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
namespace Electron2D;

internal sealed class ManagedPhysicsServer2DBackend : IPhysicsServer2DBackend
{
    private readonly Dictionary<Rid, PhysicsResource> resources = new();
    private long nextRid;
    private bool active = true;

    public string Name => "managed";

    public void SetActive(bool active)
    {
        this.active = active;
    }

    public Rid SpaceCreate()
    {
        return Allocate(PhysicsResourceKind.Space);
    }

    public void SpaceSetActive(Rid space, bool active)
    {
        Ensure(space, PhysicsResourceKind.Space).Active = active;
    }

    public bool SpaceIsActive(Rid space)
    {
        return Ensure(space, PhysicsResourceKind.Space).Active;
    }

    public void SpaceSetParam(Rid space, PhysicsServer2D.SpaceParameter param, float value)
    {
        Ensure(space, PhysicsResourceKind.Space).SpaceParameters[param] = value;
    }

    public float SpaceGetParam(Rid space, PhysicsServer2D.SpaceParameter param)
    {
        var resource = Ensure(space, PhysicsResourceKind.Space);
        return resource.SpaceParameters.TryGetValue(param, out var value) ? value : 0f;
    }

    public Rid AreaCreate()
    {
        return Allocate(PhysicsResourceKind.Area);
    }

    public Rid BodyCreate(PhysicsBodyKind bodyKind)
    {
        return Allocate(PhysicsResourceKind.Body, bodyKind: bodyKind);
    }

    public Rid JointCreate()
    {
        return Allocate(PhysicsResourceKind.Joint);
    }

    public Rid ShapeCreate(PhysicsServer2D.ShapeType type)
    {
        if (type == PhysicsServer2D.ShapeType.Custom)
        {
            throw new ArgumentException("Custom physics shapes are reserved for engine internals.", nameof(type));
        }

        return Allocate(PhysicsResourceKind.Shape, type);
    }

    public PhysicsServer2D.ShapeType ShapeGetType(Rid shape)
    {
        return Ensure(shape, PhysicsResourceKind.Shape).ShapeType;
    }

    public void CollisionObjectSetTransform(Rid rid, Transform2D transform)
    {
        var resource = EnsureCollisionObject(rid);
        resource.Transform = transform;
    }

    public void CollisionObjectSetCollisionFilter(Rid rid, PhysicsCollisionFilter filter)
    {
        var resource = EnsureCollisionObject(rid);
        resource.CollisionFilter = filter;
    }

    public void BodySetState(Rid rid, PhysicsBody2DState state)
    {
        var resource = Ensure(rid, PhysicsResourceKind.Body);
        resource.BodyState = state;
    }

    public void FreeRid(Rid rid)
    {
        if (!resources.Remove(rid))
        {
            throw new ArgumentException($"Physics RID '{rid}' was not created by this PhysicsServer2D backend.", nameof(rid));
        }
    }

    public int GetProcessInfo(PhysicsServer2D.ProcessInfo processInfo)
    {
        _ = active;
        return processInfo == PhysicsServer2D.ProcessInfo.ActiveObjects
            ? resources.Values.Count(resource => resource.Kind is PhysicsResourceKind.Area or PhysicsResourceKind.Body)
            : 0;
    }

    private Rid Allocate(
        PhysicsResourceKind kind,
        PhysicsServer2D.ShapeType shapeType = PhysicsServer2D.ShapeType.Custom,
        PhysicsBodyKind bodyKind = PhysicsBodyKind.Generic)
    {
        nextRid++;
        var rid = new Rid(nextRid);
        resources.Add(rid, new PhysicsResource(kind, shapeType, bodyKind));
        return rid;
    }

    private PhysicsResource Ensure(Rid rid, PhysicsResourceKind expectedKind)
    {
        if (!resources.TryGetValue(rid, out var resource))
        {
            throw new ArgumentException($"Physics RID '{rid}' is not alive in this PhysicsServer2D backend.", nameof(rid));
        }

        if (resource.Kind != expectedKind)
        {
            throw new ArgumentException($"Physics RID '{rid}' is a {resource.Kind}, not a {expectedKind}.", nameof(rid));
        }

        return resource;
    }

    private PhysicsResource EnsureCollisionObject(Rid rid)
    {
        if (!resources.TryGetValue(rid, out var resource))
        {
            throw new ArgumentException($"Physics RID '{rid}' is not alive in this PhysicsServer2D backend.", nameof(rid));
        }

        if (resource.Kind is not (PhysicsResourceKind.Area or PhysicsResourceKind.Body))
        {
            throw new ArgumentException($"Physics RID '{rid}' is a {resource.Kind}, not a collision object.", nameof(rid));
        }

        return resource;
    }

    private enum PhysicsResourceKind
    {
        Space,
        Area,
        Body,
        Joint,
        Shape
    }

    private sealed class PhysicsResource
    {
        public PhysicsResource(PhysicsResourceKind kind, PhysicsServer2D.ShapeType shapeType, PhysicsBodyKind bodyKind)
        {
            Kind = kind;
            ShapeType = shapeType;
            BodyKind = bodyKind;
        }

        public PhysicsResourceKind Kind { get; }

        public PhysicsServer2D.ShapeType ShapeType { get; }

        public PhysicsBodyKind BodyKind { get; }

        public Transform2D Transform { get; set; } = Transform2D.Identity;

        public PhysicsCollisionFilter CollisionFilter { get; set; } = new(1u, 1u);

        public PhysicsBody2DState BodyState { get; set; }

        public bool Active { get; set; } = true;

        public Dictionary<PhysicsServer2D.SpaceParameter, float> SpaceParameters { get; } = new();
    }
}
