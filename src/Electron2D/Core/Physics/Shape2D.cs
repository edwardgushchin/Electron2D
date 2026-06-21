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
/// Provides the Electron2D base resource for 2D physics shapes.
/// </summary>
///
/// <remarks>
/// `Shape2D` owns an opaque physics server <see cref="Rid" /> lazily created
/// by the concrete resource type. The public API does not expose backend-native
/// shape handles.
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
public abstract class Shape2D : Resource
{
    private Rid rid;

    /// <summary>
    /// Gets the physics server RID backing this shape.
    /// </summary>
    /// <returns>
    /// A valid shape RID created by the concrete shape resource.
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
        if (!rid.IsValid())
        {
            rid = CreatePhysicsRid();
        }

        return rid;
    }

    /// <summary>
    /// Creates the physics server RID for this shape resource.
    /// </summary>
    /// <returns>The created physics server RID.</returns>
    protected abstract Rid CreatePhysicsRid();

    /// <inheritdoc />
    protected override void OnFree()
    {
        if (rid.IsValid())
        {
            var ridToFree = rid;
            rid = default;
            PhysicsServer2D.FreeRid(ridToFree);
        }

        base.OnFree();
    }
}
