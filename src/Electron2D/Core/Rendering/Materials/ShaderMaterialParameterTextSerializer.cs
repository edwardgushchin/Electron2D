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
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D;

internal static class ShaderMaterialParameterTextSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(ShaderMaterialParametersSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return WriteSnapshot(snapshot).ToJsonString(IndentedOptions).ReplaceLineEndings("\n");
    }

    private static JsonObject WriteSnapshot(ShaderMaterialParametersSnapshot snapshot)
    {
        return new JsonObject
        {
            ["format"] = ShaderMaterialParametersSnapshot.FormatName,
            ["version"] = ShaderMaterialParametersSnapshot.CurrentVersion,
            ["shader"] = snapshot.ShaderPath,
            ["parameters"] = WriteParameters(snapshot.Parameters)
        };
    }

    private static JsonObject WriteParameters(IEnumerable<ShaderMaterialParameterSnapshot> parameters)
    {
        var result = new JsonObject();
        foreach (var parameter in parameters.OrderBy(parameter => parameter.Name, StringComparer.Ordinal))
        {
            result[parameter.Name] = WriteParameter(parameter);
        }

        return result;
    }

    private static JsonObject WriteParameter(ShaderMaterialParameterSnapshot parameter)
    {
        return parameter.Kind switch
        {
            ShaderMaterialParameterKind.Variant => new JsonObject
            {
                ["kind"] = parameter.Kind.ToString(),
                ["value"] = JsonNode.Parse(VariantTextSerializer.Serialize(parameter.VariantValue))
            },
            ShaderMaterialParameterKind.Texture2D => new JsonObject
            {
                ["kind"] = parameter.Kind.ToString(),
                ["type"] = parameter.TextureType,
                ["resource_path"] = parameter.TextureResourcePath,
                ["resource_scene_unique_id"] = parameter.TextureResourceSceneUniqueId,
                ["width"] = parameter.TextureWidth,
                ["height"] = parameter.TextureHeight,
                ["has_alpha"] = parameter.TextureHasAlpha,
                ["has_mipmaps"] = parameter.TextureHasMipmaps,
                ["mipmap_count"] = parameter.TextureMipmapCount
            },
            _ => throw new InvalidOperationException($"Shader material parameter kind '{parameter.Kind}' is not supported.")
        };
    }
}
