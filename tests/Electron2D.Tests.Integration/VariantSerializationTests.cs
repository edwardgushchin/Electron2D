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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class VariantSerializationTests
{
    [Fact]
    public void VariantTextSerializerWritesStableCanonicalFormatForScalarValues()
    {
        AssertRoundTrips(default(Electron2D.Variant), "{\"type\":\"Nil\",\"value\":null}");
        AssertRoundTrips(true, "{\"type\":\"Bool\",\"value\":true}");
        AssertRoundTrips(42, "{\"type\":\"Int\",\"value\":42}");
        AssertRoundTrips(2.5d, "{\"type\":\"Float\",\"value\":2.5}");
        AssertRoundTrips("player", "{\"type\":\"String\",\"value\":\"player\"}");
    }

    [Fact]
    public void VariantTextSerializerWritesStableCanonicalFormatForMathAndIdentityValues()
    {
        AssertRoundTrips(new Electron2D.Vector2(1.5f, -2f), "{\"type\":\"Vector2\",\"value\":{\"x\":1.5,\"y\":-2}}");
        AssertRoundTrips(new Electron2D.Vector2I(3, -4), "{\"type\":\"Vector2I\",\"value\":{\"x\":3,\"y\":-4}}");
        AssertRoundTrips(new Electron2D.Rect2(1f, 2f, 3f, 4f), "{\"type\":\"Rect2\",\"value\":{\"position\":{\"x\":1,\"y\":2},\"size\":{\"x\":3,\"y\":4}}}");
        AssertRoundTrips(new Electron2D.Rect2I(5, 6, 7, 8), "{\"type\":\"Rect2I\",\"value\":{\"position\":{\"x\":5,\"y\":6},\"size\":{\"x\":7,\"y\":8}}}");
        AssertRoundTrips(new Electron2D.Transform2D(1f, 0f, 0f, 1f, 9f, 10f), "{\"type\":\"Transform2D\",\"value\":{\"x\":{\"x\":1,\"y\":0},\"y\":{\"x\":0,\"y\":1},\"origin\":{\"x\":9,\"y\":10}}}");
        AssertRoundTrips(new Electron2D.Color(0.25f, 0.5f, 0.75f, 1f), "{\"type\":\"Color\",\"value\":{\"r\":0.25,\"g\":0.5,\"b\":0.75,\"a\":1}}");
        AssertRoundTrips(new Electron2D.StringName("ready"), "{\"type\":\"StringName\",\"value\":\"ready\"}");
        AssertRoundTrips(new Electron2D.NodePath("Root/Player:position"), "{\"type\":\"NodePath\",\"value\":\"Root/Player:position\"}");
    }

    [Fact]
    public void VariantTextSerializerWritesStableCanonicalFormatForNestedCollections()
    {
        var array = new Electron2D.Collections.Array();
        array.Add(1);
        array.Add("two");

        var dictionary = new Electron2D.Collections.Dictionary();
        dictionary.Add("b", 2);
        dictionary.Add("a", array);

        const string expectedArray = "{\"type\":\"Array\",\"value\":[{\"type\":\"Int\",\"value\":1},{\"type\":\"String\",\"value\":\"two\"}]}";
        const string expectedDictionary = "{\"type\":\"Dictionary\",\"value\":[{\"key\":{\"type\":\"String\",\"value\":\"a\"},\"value\":{\"type\":\"Array\",\"value\":[{\"type\":\"Int\",\"value\":1},{\"type\":\"String\",\"value\":\"two\"}]}},{\"key\":{\"type\":\"String\",\"value\":\"b\"},\"value\":{\"type\":\"Int\",\"value\":2}}]}";

        AssertRoundTrips(array, expectedArray);
        AssertRoundTrips(dictionary, expectedDictionary);

        var parsed = Electron2D.VariantTextSerializer.Deserialize(expectedDictionary).AsDictionary();

        Assert.True(parsed.TryGetValue("a", out var parsedArray));
        Assert.Equal("two", parsedArray.AsArray()[1].AsString());
        Assert.True(parsed.TryGetValue("b", out var parsedInteger));
        Assert.Equal(2L, parsedInteger.AsInt64());
    }

    [Fact]
    public void VariantTextSerializerRejectsRuntimeOnlyValuesWithClearErrors()
    {
        var node = new Electron2D.Node();
        var callable = Electron2D.Callable.From(() => { });

        AssertSerializationError((Electron2D.Variant)node, "Object");
        AssertSerializationError((Electron2D.Variant)callable, "Callable");
        AssertSerializationError(default(Electron2D.Rid), "Rid");
    }

    [Fact]
    public void VariantTextSerializerRejectsMalformedOrUnsupportedTextWithClearErrors()
    {
        var malformed = Assert.Throws<FormatException>(() => Electron2D.VariantTextSerializer.Deserialize("[]"));
        var missingType = Assert.Throws<FormatException>(() => Electron2D.VariantTextSerializer.Deserialize("{\"value\":1}"));
        var unsupported = Assert.Throws<FormatException>(() => Electron2D.VariantTextSerializer.Deserialize("{\"type\":\"Vector3\",\"value\":{\"x\":1,\"y\":2,\"z\":3}}"));
        var runtimeOnly = Assert.Throws<FormatException>(() => Electron2D.VariantTextSerializer.Deserialize("{\"type\":\"Object\",\"value\":1}"));

        Assert.Contains("object", malformed.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("type", missingType.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Vector3", unsupported.Message, StringComparison.Ordinal);
        Assert.Contains("Object", runtimeOnly.Message, StringComparison.Ordinal);
    }

    private static void AssertRoundTrips(Electron2D.Variant variant, string expected)
    {
        var serialized = Electron2D.VariantTextSerializer.Serialize(variant);
        var deserialized = Electron2D.VariantTextSerializer.Deserialize(serialized);

        Assert.Equal(expected, serialized);
        Assert.Equal(expected, Electron2D.VariantTextSerializer.Serialize(deserialized));
    }

    private static void AssertSerializationError(Electron2D.Variant variant, string expectedType)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Electron2D.VariantTextSerializer.Serialize(variant));

        Assert.Contains(expectedType, exception.Message, StringComparison.Ordinal);
        Assert.Contains("not serializable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
