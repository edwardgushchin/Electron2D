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
/// Provides an Electron2D node that renders 2D descendants on an independent canvas layer.
/// </summary>
///
/// <remarks>
/// Descendant <see cref="CanvasItem" /> nodes are submitted with this layer's
/// numeric draw layer. Lower layers are drawn before higher layers, regardless
/// of descendant `ZIndex` values.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate layers on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
public class CanvasLayer : Node
{
    /// <summary>
    /// Gets or sets the numeric draw layer.
    /// </summary>
    ///
    /// <remarks>
    /// The default value is <c>1</c>. Canvas items outside any
    /// `CanvasLayer` are submitted to layer <c>0</c>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public int Layer { get; set; } = 1;

    /// <summary>
    /// Gets or sets the layer offset.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 Offset { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the layer rotation in radians.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RotationDegrees" />
    public float Rotation { get; set; }

    /// <summary>
    /// Gets or sets the layer rotation in degrees.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Rotation" />
    public float RotationDegrees
    {
        get
        {
            ThrowIfFreed();
            return Mathf.RadToDeg(Rotation);
        }
        set
        {
            ThrowIfFreed();
            Rotation = Mathf.DegToRad(value);
        }
    }

    /// <summary>
    /// Gets or sets the layer scale.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// Gets or sets the layer transform.
    /// </summary>
    ///
    /// <remarks>
    /// The setter decomposes offset, rotation and scale for transforms without
    /// skew. Skew is outside the Electron2D 0.1.0 Preview subset.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Transform2D Transform
    {
        get
        {
            ThrowIfFreed();
            return Node2D.CreateTransform(Rotation, Scale, Offset);
        }
        set
        {
            ThrowIfFreed();
            Offset = value.Origin;
            Rotation = Node2D.DecomposeRotation(value);
            Scale = Node2D.DecomposeScale(value);
        }
    }

    /// <summary>
    /// Gets or sets whether this canvas layer is visible.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets the transform applied to descendant canvas items during submission.
    /// </summary>
    ///
    /// <returns>The layer transform for Electron2D 0.1.0 Preview.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Transform2D GetFinalTransform()
    {
        ThrowIfFreed();
        return Transform;
    }

    /// <summary>
    /// Hides this canvas layer and its submitted descendants.
    /// </summary>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Show" />
    public void Hide()
    {
        ThrowIfFreed();
        Visible = false;
    }

    /// <summary>
    /// Shows this canvas layer.
    /// </summary>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Hide" />
    public void Show()
    {
        ThrowIfFreed();
        Visible = true;
    }
}
