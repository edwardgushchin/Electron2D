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
/// Provides the Electron2D resource for a canvas shader source.
/// </summary>
///
/// <remarks>
/// <para>
/// `Shader` stores source code that is imported by Electron2D tooling into
/// compiled canvas shader stages. Electron2D 0.1-preview supports only the
/// canvas item mode.
/// </para>
///
/// <para>
/// `ShaderMaterial`, uniforms and sampler assignment are implemented by later
/// tasks. This type intentionally does not expose the shader compiler backend or the native GPU backend
/// handles.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Mutate shader code on the main thread or in
/// a single import worker.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
public sealed class Shader : Resource
{

    /// <summary>
    /// Initializes a new instance of the Shader type.
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
    /// <seealso cref="Shader" />
    ///
    public Shader()
    {
    }

    private string code = string.Empty;

    /// <summary>
    /// Identifies the shader mode.
    /// </summary>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This type is part of the Electron2D 0.1-preview public API.
    /// </remarks>
    ///
    public enum Mode
    {
        /// <summary>
        /// Canvas item shader mode used for 2D drawing.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept Mode.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="Mode" />
        ///
        CanvasItem = 1
    }

    /// <summary>
    /// Gets or sets the shader source code.
    /// </summary>
    ///
    /// <remarks>
    /// The source is stored as written by the user. Import tooling parses the
    /// `shader_type canvas_item;`, `vertex_entry` and `fragment_entry` header
    /// directives before sending HLSL to the shader compiler backend.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main thread or in a
    /// single import worker.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <value>
    /// The current code value.
    /// </value>
    ///
    /// <seealso cref="Shader" />
    ///
    public string Code
    {
        get
        {
            ThrowIfFreed();
            return code;
        }
        set
        {
            ThrowIfFreed();
            code = value ?? string.Empty;
        }
    }

    /// <summary>
    /// Gets the shader mode.
    /// </summary>
    ///
    /// <returns><see cref="Mode.CanvasItem" />, the only supported shader mode in Electron2D 0.1-preview.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when the shader is not being
    /// mutated concurrently.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Shader" />
    ///
    public Mode GetMode()
    {
        ThrowIfFreed();
        return Mode.CanvasItem;
    }
}
