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
using Electron2D;

ResourceObjectMetadataRegistry.Register(
    ResourceObjectTypeMetadata.Create(
        typeof(AotSmokeResource).FullName!,
        () => new AotSmokeResource(),
        [
            ResourceObjectPropertyMetadata.Create<AotSmokeResource, string>(
                "display_name",
                resource => resource.DisplayName,
                (resource, value) => resource.DisplayName = value),
            ResourceObjectPropertyMetadata.Create<AotSmokeResource, HorizontalAlignment>(
                "alignment",
                resource => resource.Alignment,
                (resource, value) => resource.Alignment = value),
            ResourceObjectPropertyMetadata.Create<AotSmokeResource, int?>(
                "optional_lives",
                resource => resource.OptionalLives,
                (resource, value) => resource.OptionalLives = value),
            ResourceObjectPropertyMetadata.CreateArray<AotSmokeResource, string>(
                "tags",
                resource => resource.Tags,
                (resource, value) => resource.Tags = value),
            ResourceObjectPropertyMetadata.CreateDictionary<AotSmokeResource, string, int>(
                "scores",
                resource => resource.Scores,
                (resource, value) => resource.Scores = value)
        ]));

var source = new AotSmokeResource
{
    DisplayName = "AOT",
    Alignment = HorizontalAlignment.Right,
    OptionalLives = 3,
    Tags = ["trimmed", "nativeaot"],
    Scores = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["coins"] = 9
    }
};

var document = ResourceObjectSerializer.Capture(source, "res://aot/smoke.e2res");
var text = SerializedResourceTextSerializer.Serialize(document);
var parsed = SerializedResourceTextSerializer.Deserialize(text);
var restored = (AotSmokeResource)ResourceObjectSerializer.Instantiate(parsed);

if (restored.DisplayName != "AOT" ||
    restored.Alignment != HorizontalAlignment.Right ||
    restored.OptionalLives != 3 ||
    restored.Tags.Length != 2 ||
    restored.Tags[1] != "nativeaot" ||
    restored.Scores["coins"] != 9)
{
    throw new InvalidOperationException("AOT metadata smoke round-trip failed.");
}

Console.WriteLine("AOT metadata smoke passed.");

internal sealed class AotSmokeResource : Resource
{
    public string DisplayName { get; set; } = string.Empty;

    public HorizontalAlignment Alignment { get; set; }

    public int? OptionalLives { get; set; }

    public string[] Tags { get; set; } = [];

    public Dictionary<string, int> Scores { get; set; } = new(StringComparer.Ordinal);
}
