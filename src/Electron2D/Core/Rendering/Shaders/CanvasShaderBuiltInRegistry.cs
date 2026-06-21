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

internal static class CanvasShaderBuiltInRegistry
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.Ordinal)
    {
        "AT_LIGHT_PASS",
        "CANVAS_MATRIX",
        "COLOR",
        "CUSTOM0",
        "CUSTOM1",
        "E",
        "FRAGCOORD",
        "INSTANCE_CUSTOM",
        "INSTANCE_ID",
        "LIGHT",
        "LIGHT_COLOR",
        "LIGHT_DIRECTION",
        "LIGHT_ENERGY",
        "LIGHT_IS_DIRECTIONAL",
        "LIGHT_POSITION",
        "LIGHT_VERTEX",
        "MODEL_MATRIX",
        "NORMAL",
        "NORMAL_MAP",
        "NORMAL_MAP_DEPTH",
        "NORMAL_TEXTURE",
        "PI",
        "POINT_COORD",
        "POINT_SIZE",
        "REGION_RECT",
        "SCREEN_MATRIX",
        "SCREEN_PIXEL_SIZE",
        "SCREEN_TEXTURE",
        "SCREEN_UV",
        "SHADOW_MODULATE",
        "SHADOW_VERTEX",
        "SPECULAR_SHININESS",
        "SPECULAR_SHININESS_TEXTURE",
        "TAU",
        "TEXTURE",
        "TEXTURE_PIXEL_SIZE",
        "TIME",
        "UV",
        "VERTEX",
        "VERTEX_ID",
        "screen_uv_to_sdf",
        "sdf_to_screen_uv",
        "texture_sdf",
        "texture_sdf_normal"
    };

    public static bool IsReserved(string? name)
    {
        return !string.IsNullOrEmpty(name) && ReservedNames.Contains(name);
    }
}
