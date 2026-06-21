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
/// Provides a Godot-like node that attaches a <see cref="Shape2D" /> to a collision object.
/// </summary>
///
/// <remarks>
/// This node stores a shape resource reference and validates Godot-like shape
/// placement rules when it enters a <see cref="SceneTree" />.
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
public class CollisionShape2D : Node2D, ISceneTreeLifecycleHandler
{
    private Shape2D? shape;

    /// <summary>
    /// Gets or sets the shape resource attached by this node.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Shape2D? Shape
    {
        get
        {
            ThrowIfFreed();
            return shape;
        }
        set
        {
            ThrowIfFreed();
            ValidateConcaveOwner(value);
            shape = value;
        }
    }

    /// <summary>
    /// Gets or sets whether the shape is disabled.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets whether the shape should act as a one-way collision shape.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool OneWayCollision { get; set; }

    /// <summary>
    /// Gets or sets the one-way collision margin.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public float OneWayCollisionMargin { get; set; } = 1f;

    void ISceneTreeLifecycleHandler.OnEnterTree()
    {
        ValidateConcaveOwner(shape);
    }

    void ISceneTreeLifecycleHandler.OnPhysicsProcess(double delta)
    {
        _ = delta;
    }

    void ISceneTreeLifecycleHandler.OnExitTree()
    {
    }

    private void ValidateConcaveOwner(Shape2D? candidate)
    {
        if (candidate is not ConcavePolygonShape2D)
        {
            return;
        }

        var owner = FindCollisionObjectOwner();
        if (owner is null or StaticBody2D)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{nameof(ConcavePolygonShape2D)} can only be used under {nameof(StaticBody2D)}. " +
            $"Current collision owner is '{owner.GetType().Name}'.");
    }

    private CollisionObject2D? FindCollisionObjectOwner()
    {
        var current = GetParent();
        while (current is not null)
        {
            if (current is CollisionObject2D collisionObject)
            {
                return collisionObject;
            }

            current = current.GetParent();
        }

        return null;
    }
}
