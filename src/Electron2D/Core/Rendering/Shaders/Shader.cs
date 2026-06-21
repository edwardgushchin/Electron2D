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
/// Provides the Godot-like resource for a canvas shader source.
/// </summary>
///
/// <remarks>
/// <para>
/// `Shader` stores source code that is imported by Electron2D tooling into
/// compiled canvas shader stages. Electron2D 0.1.0 Preview supports only the
/// canvas item mode.
/// </para>
///
/// <para>
/// `ShaderMaterial`, uniforms and sampler assignment are implemented by later
/// tasks. This type intentionally does not expose SDL_shadercross or SDL_GPU
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
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
public sealed class Shader : Resource
{
    private string code = string.Empty;

    /// <summary>
    /// Identifies the shader mode.
    /// </summary>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    public enum Mode
    {
        /// <summary>
        /// Canvas item shader mode used for 2D drawing.
        /// </summary>
        CanvasItem = 1
    }

    /// <summary>
    /// Gets or sets the shader source code.
    /// </summary>
    ///
    /// <remarks>
    /// The source is stored as written by the user. Import tooling parses the
    /// `shader_type canvas_item;`, `vertex_entry` and `fragment_entry` header
    /// directives before sending HLSL to SDL_shadercross.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main thread or in a
    /// single import worker.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
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
    /// <returns><see cref="Mode.CanvasItem" />, the only supported shader mode in Electron2D 0.1.0 Preview.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when the shader is not being
    /// mutated concurrently.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Mode GetMode()
    {
        ThrowIfFreed();
        return Mode.CanvasItem;
    }
}
