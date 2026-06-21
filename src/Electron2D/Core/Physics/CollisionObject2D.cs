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
/// Provides the Godot-like base node for 2D objects that own a physics server RID.
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
    public uint CollisionLayer { get; set; } = 1u;

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
    public uint CollisionMask { get; set; } = 1u;

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
        _ = delta;
        if (rid.IsValid())
        {
            PhysicsServer2D.CollisionObjectSetTransform(rid, GlobalTransform);
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
}
