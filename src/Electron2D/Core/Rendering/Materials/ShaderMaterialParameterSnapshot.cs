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

internal sealed class ShaderMaterialParameterSnapshot
{
    private ShaderMaterialParameterSnapshot(
        string name,
        ShaderMaterialParameterKind kind,
        Variant variantValue,
        string textureType,
        string textureResourcePath,
        string textureResourceSceneUniqueId,
        int textureWidth,
        int textureHeight,
        bool textureHasAlpha,
        bool textureHasMipmaps,
        int textureMipmapCount)
    {
        Name = name;
        Kind = kind;
        VariantValue = variantValue;
        TextureType = textureType;
        TextureResourcePath = textureResourcePath;
        TextureResourceSceneUniqueId = textureResourceSceneUniqueId;
        TextureWidth = textureWidth;
        TextureHeight = textureHeight;
        TextureHasAlpha = textureHasAlpha;
        TextureHasMipmaps = textureHasMipmaps;
        TextureMipmapCount = textureMipmapCount;
    }

    public string Name { get; }

    public ShaderMaterialParameterKind Kind { get; }

    public Variant VariantValue { get; }

    public string TextureType { get; }

    public string TextureResourcePath { get; }

    public string TextureResourceSceneUniqueId { get; }

    public int TextureWidth { get; }

    public int TextureHeight { get; }

    public bool TextureHasAlpha { get; }

    public bool TextureHasMipmaps { get; }

    public int TextureMipmapCount { get; }

    public static ShaderMaterialParameterSnapshot FromVariant(string name, Variant value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ShaderMaterialParameterValidator.ThrowIfUnsupported(new StringName(name), value);

        return new ShaderMaterialParameterSnapshot(
            name,
            ShaderMaterialParameterKind.Variant,
            value,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            0,
            false,
            false,
            0);
    }

    public static ShaderMaterialParameterSnapshot FromTexture(string name, Texture2D texture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(texture);

        if (!Object.IsInstanceValid(texture))
        {
            throw new ArgumentException("Texture sampler parameter must reference a valid Texture2D.", nameof(texture));
        }

        return new ShaderMaterialParameterSnapshot(
            name,
            ShaderMaterialParameterKind.Texture2D,
            texture,
            texture.GetType().FullName ?? texture.GetType().Name,
            texture.ResourcePath,
            texture.ResourceSceneUniqueId,
            texture.GetWidth(),
            texture.GetHeight(),
            texture.HasAlpha(),
            texture.HasMipmaps(),
            texture.GetMipmapCount());
    }
}
