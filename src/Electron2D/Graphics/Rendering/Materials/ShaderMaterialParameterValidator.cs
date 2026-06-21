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

internal static class ShaderMaterialParameterValidator
{
    public static void ThrowIfUnsupported(StringName parameterName, Variant value)
    {
        var name = parameterName.ToString();
        if (CanvasShaderBuiltInRegistry.IsReserved(name))
        {
            throw new ArgumentException(
                $"Shader parameter '{name}' is an engine-provided canvas shader built-in and cannot be set as a material parameter.",
                nameof(parameterName));
        }

        switch (value.VariantType)
        {
            case Variant.Type.Nil:
            case Variant.Type.Bool:
            case Variant.Type.Int:
            case Variant.Type.Float:
            case Variant.Type.Vector2:
            case Variant.Type.Transform2D:
            case Variant.Type.Color:
                return;
            case Variant.Type.Object:
                ValidateObjectParameter(name, value);
                return;
            default:
                throw new ArgumentException(
                    $"Variant type '{value.VariantType}' is not supported as a ShaderMaterial parameter in Electron2D 0.1.0 Preview.",
                    nameof(value));
        }
    }

    private static void ValidateObjectParameter(string parameterName, Variant value)
    {
        if (value.AsObject() is Texture2D texture && Object.IsInstanceValid(texture))
        {
            return;
        }

        throw new ArgumentException(
            $"Shader parameter '{parameterName}' can store Object values only when they are valid Texture2D sampler resources.",
            nameof(value));
    }
}
