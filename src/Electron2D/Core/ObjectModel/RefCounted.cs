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
/// Represents the ref counted type.
/// </summary>
///
/// <remarks>
/// This type is part of the Electron2D 0.1-preview public API.
/// </remarks>
///
/// <threadsafety>
/// Instances of this type are not synchronized. Access them from the thread that owns the object unless the member documentation states otherwise.
/// </threadsafety>
///
/// <since>
/// This API is available since Electron2D 0.1-preview.
/// </since>
///
public class RefCounted : Object
{

    /// <summary>
    /// Initializes a new instance of the RefCounted type.
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
    /// <seealso cref="RefCounted" />
    ///
    public RefCounted()
    {
    }

    private int _referenceCount = 1;

    /// <summary>
    /// Executes the reference operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="RefCounted" />
    ///
    public bool Reference()
    {
        if (!IsInstanceValid(this) || _referenceCount <= 0)
        {
            return false;
        }

        checked
        {
            _referenceCount++;
        }

        return true;
    }

    /// <summary>
    /// Executes the unreference operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="RefCounted" />
    ///
    public bool Unreference()
    {
        if (_referenceCount <= 0)
        {
            return true;
        }

        _referenceCount--;
        if (_referenceCount == 0)
        {
            Free();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the reference count value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current reference count value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="RefCounted" />
    ///
    public int GetReferenceCount()
    {
        return _referenceCount;
    }

    protected override void OnFree()
    {
        _referenceCount = 0;
        base.OnFree();
    }
}
