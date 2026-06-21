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
/// Provides the Electron2D base resource for visual materials.
/// </summary>
///
/// <remarks>
/// <para>
/// `Material` is the common base for resources that describe how geometry is
/// colored or shaded. Electron2D 0.1.0 Preview uses it as the inheritance base
/// for <see cref="ShaderMaterial" />.
/// </para>
///
/// <para>
/// The current 2D renderer stores <see cref="NextPass" /> and
/// <see cref="RenderPriority" /> for API parity, but the draw pipeline does
/// not yet execute additional material passes.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Mutate material state on the main thread or
/// from a single import/tooling worker.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="ShaderMaterial" />
public abstract class Material : Resource
{
    private const int MinimumRenderPriority = -128;
    private const int MaximumRenderPriority = 127;

    private Material? nextPass;
    private int renderPriority;

    /// <summary>
    /// Gets or sets the material used for the next rendering pass.
    /// </summary>
    ///
    /// <remarks>
    /// Electron2D 0.1.0 Preview stores this value so resource data can round
    /// trip through tooling. Rendering extra passes is part of a later renderer
    /// task.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main thread or from
    /// a single import/tooling worker.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Material? NextPass
    {
        get
        {
            ThrowIfFreed();
            return nextPass;
        }
        set
        {
            ThrowIfFreed();
            nextPass = value;
        }
    }

    /// <summary>
    /// Gets or sets the material render priority.
    /// </summary>
    ///
    /// <remarks>
    /// The valid range matches Electron2D's material priority range:
    /// <c>-128</c> through <c>127</c>. The value is stored for future renderer
    /// ordering; current 2D queue ordering is still driven by canvas item
    /// state.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is outside the supported range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main thread or from
    /// a single import/tooling worker.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public int RenderPriority
    {
        get
        {
            ThrowIfFreed();
            return renderPriority;
        }
        set
        {
            ThrowIfFreed();
            if (value < MinimumRenderPriority || value > MaximumRenderPriority)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Material render priority must be in the range -128..127.");
            }

            renderPriority = value;
        }
    }
}
