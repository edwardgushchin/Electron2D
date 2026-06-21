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

internal static class ScriptObjectSerializer
{
    public static IReadOnlyDictionary<string, SerializedPropertyValue> CaptureExportedProperties(Node script)
    {
        ArgumentNullException.ThrowIfNull(script);

        var metadata = ScriptObjectMetadataRegistry.GetByScriptType(script.GetType());
        var properties = new Dictionary<string, SerializedPropertyValue>(StringComparer.Ordinal);
        foreach (var property in metadata.ExportedProperties)
        {
            properties.Add(property.Name, property.Capture(script));
        }

        return properties;
    }

    public static void RestoreExportedProperties(
        Node script,
        IReadOnlyDictionary<string, SerializedPropertyValue> properties)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(properties);

        var metadata = ScriptObjectMetadataRegistry.GetByScriptType(script.GetType());
        foreach (var property in metadata.ExportedProperties)
        {
            if (!properties.TryGetValue(property.Name, out var value))
            {
                continue;
            }

            property.Restore(script, value);
        }
    }
}
