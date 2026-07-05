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
/// This type is available since Electron2D 0.1-preview.
/// </since>
public class CanvasLayer : Node
{

    /// <summary>
    /// Initializes a new instance of the CanvasLayer type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="CanvasLayer" />
    ///
    public CanvasLayer()
    {
    }

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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <value>
    /// The current layer value.
    /// </value>
    ///
    /// <seealso cref="CanvasLayer" />
    ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current offset value.
    /// </value>
    ///
    /// <seealso cref="CanvasLayer" />
    ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="RotationDegrees" />
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current rotation value.
    /// </value>
    ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rotation" />
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current rotation degrees value.
    /// </value>
    ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current scale value.
    /// </value>
    ///
    /// <seealso cref="CanvasLayer" />
    ///
    public Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// Gets or sets the layer transform.
    /// </summary>
    ///
    /// <remarks>
    /// The setter decomposes offset, rotation and scale for transforms without
    /// skew. Skew is outside the Electron2D 0.1-preview subset.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <value>
    /// The current transform value.
    /// </value>
    ///
    /// <seealso cref="CanvasLayer" />
    ///
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current visible value.
    /// </value>
    ///
    /// <seealso cref="CanvasLayer" />
    ///
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets the transform applied to descendant canvas items during submission.
    /// </summary>
    ///
    /// <returns>The layer transform for Electron2D 0.1-preview.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="CanvasLayer" />
    ///
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
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Show" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
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
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Hide" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void Show()
    {
        ThrowIfFreed();
        Visible = true;
    }
}
