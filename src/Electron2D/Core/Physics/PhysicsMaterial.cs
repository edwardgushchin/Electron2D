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
/// Provides a Godot-like physics material resource for 2D body collision behavior.
/// </summary>
///
/// <remarks>
/// `Bounce` is the Godot-like public name for restitution. The 0.1.0 Preview
/// stores material state and serializes it for the future solver; it does not
/// calculate contacts yet.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate resources on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
public sealed class PhysicsMaterial : Resource
{
    private float friction = 1f;
    private float bounce;
    private bool rough;
    private bool absorbent;

    /// <summary>
    /// Gets or sets the friction coefficient used by this material.
    /// </summary>
    ///
    /// <value>
    /// A finite value greater than or equal to <c>0</c>.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public float Friction
    {
        get
        {
            ThrowIfFreed();
            return friction;
        }
        set
        {
            ThrowIfFreed();
            PhysicsMaterialValidation.RequireNonNegativeFinite(value, nameof(Friction));
            friction = value;
        }
    }

    /// <summary>
    /// Gets or sets the bounce coefficient used by this material.
    /// </summary>
    ///
    /// <remarks>
    /// This is the Godot-like name for restitution.
    /// </remarks>
    ///
    /// <value>
    /// A finite value greater than or equal to <c>0</c>.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public float Bounce
    {
        get
        {
            ThrowIfFreed();
            return bounce;
        }
        set
        {
            ThrowIfFreed();
            PhysicsMaterialValidation.RequireNonNegativeFinite(value, nameof(Bounce));
            bounce = value;
        }
    }

    /// <summary>
    /// Gets or sets whether this material should prefer its friction in future contact solving.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool Rough
    {
        get
        {
            ThrowIfFreed();
            return rough;
        }
        set
        {
            ThrowIfFreed();
            rough = value;
        }
    }

    /// <summary>
    /// Gets or sets whether this material should absorb bounce in future contact solving.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool Absorbent
    {
        get
        {
            ThrowIfFreed();
            return absorbent;
        }
        set
        {
            ThrowIfFreed();
            absorbent = value;
        }
    }
}
