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

/// <summary>
/// Provides the Electron2D base node for 2D objects that own a physics server RID.
/// </summary>
///
/// <remarks>
/// `CollisionObject2D` creates its RID when it enters a <see cref="SceneTree" />
/// and frees it when it exits the tree. The RID is an Electron2D
/// <see cref="Rid" />, not a Box2D.NET handle.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate nodes on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
public abstract class CollisionObject2D : Node2D, ISceneTreeLifecycleHandler
{
    private Rid rid;
    private uint collisionLayer = 1u;
    private uint collisionMask = 1u;

    /// <summary>
    /// Gets or sets the collision layer bits for this object.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public uint CollisionLayer
    {
        get
        {
            ThrowIfFreed();
            return collisionLayer;
        }
        set
        {
            ThrowIfFreed();
            collisionLayer = value;
        }
    }

    /// <summary>
    /// Gets or sets the collision mask bits used by this object.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public uint CollisionMask
    {
        get
        {
            ThrowIfFreed();
            return collisionMask;
        }
        set
        {
            ThrowIfFreed();
            collisionMask = value;
        }
    }

    /// <summary>
    /// Gets the physics server RID owned by this collision object.
    /// </summary>
    /// <returns>
    /// The current physics server RID, or the default empty <see cref="Rid" />
    /// when the node is not inside a <see cref="SceneTree" />.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Rid GetRid()
    {
        ThrowIfFreed();
        return rid;
    }

    /// <summary>
    /// Enables or disables an Electron2D collision layer number.
    /// </summary>
    /// <param name="layerNumber">The layer number in the inclusive range <c>1..32</c>.</param>
    /// <param name="value">Whether the layer bit should be enabled.</param>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public void SetCollisionLayerValue(int layerNumber, bool value)
    {
        CollisionLayer = SetCollisionBit(CollisionLayer, layerNumber, value);
    }

    /// <summary>
    /// Checks whether an Electron2D collision layer number is enabled.
    /// </summary>
    /// <param name="layerNumber">The layer number in the inclusive range <c>1..32</c>.</param>
    /// <returns><c>true</c> when the layer bit is enabled; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool GetCollisionLayerValue(int layerNumber)
    {
        ThrowIfFreed();
        return (CollisionLayer & LayerNumberToBit(layerNumber)) != 0u;
    }

    /// <summary>
    /// Enables or disables an Electron2D collision mask number.
    /// </summary>
    /// <param name="layerNumber">The mask layer number in the inclusive range <c>1..32</c>.</param>
    /// <param name="value">Whether the mask bit should be enabled.</param>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public void SetCollisionMaskValue(int layerNumber, bool value)
    {
        CollisionMask = SetCollisionBit(CollisionMask, layerNumber, value);
    }

    /// <summary>
    /// Checks whether an Electron2D collision mask number is enabled.
    /// </summary>
    /// <param name="layerNumber">The mask layer number in the inclusive range <c>1..32</c>.</param>
    /// <returns><c>true</c> when the mask bit is enabled; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool GetCollisionMaskValue(int layerNumber)
    {
        ThrowIfFreed();
        return (CollisionMask & LayerNumberToBit(layerNumber)) != 0u;
    }

    internal Rid CurrentRid => rid;

    void ISceneTreeLifecycleHandler.OnEnterTree()
    {
        if (!rid.IsValid())
        {
            rid = CreatePhysicsRid();
        }
    }

    void ISceneTreeLifecycleHandler.OnPhysicsProcess(double delta)
    {
        if (rid.IsValid())
        {
            PhysicsStep(delta);
            PhysicsServer2D.CollisionObjectSetTransform(rid, GlobalTransform);
            PhysicsServer2D.CollisionObjectSetCollisionFilter(rid, new PhysicsCollisionFilter(CollisionLayer, CollisionMask));
            SynchronizePhysicsState(rid);
        }
    }

    void ISceneTreeLifecycleHandler.OnExitTree()
    {
        FreePhysicsRid();
    }

    protected override void OnFree()
    {
        FreePhysicsRid();
        base.OnFree();
    }

    /// <summary>
    /// Creates the physics server RID for this collision object.
    /// </summary>
    /// <returns>The created physics server RID.</returns>
    protected abstract Rid CreatePhysicsRid();

    /// <summary>
    /// Synchronizes subclass-specific physics state into the physics server.
    /// </summary>
    /// <param name="rid">The physics server RID owned by this object.</param>
    protected virtual void SynchronizePhysicsState(Rid rid)
    {
        _ = rid;
    }

    internal virtual void PhysicsStep(double delta)
    {
        _ = delta;
    }

    private void FreePhysicsRid()
    {
        if (!rid.IsValid())
        {
            return;
        }

        var ridToFree = rid;
        rid = default;
        PhysicsServer2D.FreeRid(ridToFree);
    }

    private static uint SetCollisionBit(uint bits, int layerNumber, bool value)
    {
        var bit = LayerNumberToBit(layerNumber);
        return value ? bits | bit : bits & ~bit;
    }

    private static uint LayerNumberToBit(int layerNumber)
    {
        if (layerNumber is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(
                nameof(layerNumber),
                layerNumber,
                "Collision layer number must be in range 1..32.");
        }

        return 1u << (layerNumber - 1);
    }
}
