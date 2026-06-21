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

internal sealed class ShaderMaterialParametersSnapshot
{
    public const string FormatName = "Electron2D.ShaderMaterialParameters";
    public const int CurrentVersion = 1;

    public ShaderMaterialParametersSnapshot(string shaderPath, IReadOnlyList<ShaderMaterialParameterSnapshot> parameters)
    {
        ShaderPath = shaderPath ?? string.Empty;
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    public string ShaderPath { get; }

    public IReadOnlyList<ShaderMaterialParameterSnapshot> Parameters { get; }

    public static ShaderMaterialParametersSnapshot FromMaterial(ShaderMaterial material)
    {
        ArgumentNullException.ThrowIfNull(material);

        var shaderPath = material.Shader?.ResourcePath ?? string.Empty;
        var parameters = material.ShaderParameters
            .Select(pair => ToSnapshot(pair.Key.ToString(), pair.Value))
            .OrderBy(parameter => parameter.Name, StringComparer.Ordinal)
            .ToArray();

        return new ShaderMaterialParametersSnapshot(shaderPath, parameters);
    }

    private static ShaderMaterialParameterSnapshot ToSnapshot(string name, Variant value)
    {
        if (value.VariantType == Variant.Type.Object && value.AsObject() is Texture2D texture)
        {
            return ShaderMaterialParameterSnapshot.FromTexture(name, texture);
        }

        return ShaderMaterialParameterSnapshot.FromVariant(name, value);
    }
}
