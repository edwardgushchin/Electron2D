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

internal sealed class Electron2DExportPresetDocument
{
    public Electron2DExportPreset[] Presets { get; set; } = [];

    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Presets);
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var preset in Presets)
        {
            ArgumentNullException.ThrowIfNull(preset);
            preset.Validate();
            if (!names.Add(preset.Name))
            {
                throw new Electron2DExportPresetFormatException(
                    "E2D-EXPORT-PRESET-0001",
                    $"Export preset name '{preset.Name}' is duplicated.",
                    preset.Name);
            }
        }

        Presets = Presets
            .OrderBy(preset => preset.Name, StringComparer.Ordinal)
            .ToArray();
    }
}
