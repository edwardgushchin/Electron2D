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
/// Provides the Electron2D material resource backed by a custom <see cref="Shader" />.
/// </summary>
///
/// <remarks>
/// <para>
/// `ShaderMaterial` stores a shader resource and a case-sensitive map of shader
/// parameter values. Electron2D 0.1-preview supports scalar 2D uniforms and
/// <see cref="Texture2D" /> sampler values.
/// </para>
///
/// <para>
/// Unsupported values are rejected when set. This prevents the renderer from
/// silently replacing invalid parameters later in the frame.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Mutate shader material state on the main
/// thread or from a single import/tooling worker.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Material" />
/// <seealso cref="Shader" />
/// <seealso cref="Texture2D" />
public sealed class ShaderMaterial : Material
{

    /// <summary>
    /// Initializes a new instance of the ShaderMaterial type.
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
    /// <seealso cref="ShaderMaterial" />
    ///
    public ShaderMaterial()
    {
    }

    private readonly Dictionary<StringName, Variant> parameters = new();
    private Shader? shader;

    /// <summary>
    /// Gets or sets the shader program used by this material.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main thread or from
    /// a single import/tooling worker.
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
    /// The current shader value.
    /// </value>
    ///
    /// <seealso cref="ShaderMaterial" />
    ///
    public Shader? Shader
    {
        get
        {
            ThrowIfFreed();
            return shader;
        }
        set
        {
            ThrowIfFreed();
            shader = value;
        }
    }

    internal IReadOnlyDictionary<StringName, Variant> ShaderParameters => parameters;

    /// <summary>
    /// Returns the current value set for a shader uniform parameter.
    /// </summary>
    ///
    /// <param name="param">The exact case-sensitive parameter name.</param>
    ///
    /// <returns>
    /// The stored parameter value, or a nil <see cref="Variant" /> when no
    /// value is set for <paramref name="param" />.
    /// </returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="param" /> is empty.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main thread or from a
    /// single import/tooling worker.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetShaderParameter" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public Variant GetShaderParameter(StringName param)
    {
        ThrowIfFreed();
        var parameterName = ValidateParameterName(param);
        return parameters.TryGetValue(parameterName, out var value) ? value : default;
    }

    /// <summary>
    /// Changes the value set for a shader uniform parameter.
    /// </summary>
    ///
    /// <param name="param">The exact case-sensitive parameter name.</param>
    /// <param name="value">
    /// The new value. A nil <see cref="Variant" /> removes the stored
    /// parameter value.
    /// </param>
    ///
    /// <remarks>
    /// In Electron2D 0.1-preview the supported value subset is
    /// <see cref="bool" />, integer values, floating-point values,
    /// <see cref="Vector2" />, <see cref="Color" />,
    /// <see cref="Transform2D" /> and <see cref="Texture2D" /> references.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="param" /> is empty, uses a reserved canvas
    /// shader built-in name, or <paramref name="value" /> is not a supported
    /// shader parameter value.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main thread or from a
    /// single import/tooling worker.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetShaderParameter" />
    public void SetShaderParameter(StringName param, Variant value)
    {
        ThrowIfFreed();
        var parameterName = ValidateParameterName(param);
        ShaderMaterialParameterValidator.ThrowIfUnsupported(parameterName, value);

        if (value.IsNil())
        {
            parameters.Remove(parameterName);
            return;
        }

        parameters[parameterName] = value;
    }

    private static StringName ValidateParameterName(StringName param)
    {
        if (param.IsEmpty())
        {
            throw new ArgumentException("Shader parameter name must not be empty.", nameof(param));
        }

        return param;
    }
}
