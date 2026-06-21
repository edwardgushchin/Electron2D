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

namespace Electron2D.Tests.Unit;

public sealed class VariantTests
{
    [Fact]
    public void VariantDefaultAndNullValuesAreNil()
    {
        var defaultVariant = default(Electron2D.Variant);
        var nullVariant = Electron2D.Variant.CreateFrom(null);

        Assert.Equal(Electron2D.Variant.Type.Nil, defaultVariant.VariantType);
        Assert.True(defaultVariant.IsNil());
        Assert.Null(defaultVariant.Obj);
        Assert.Equal(defaultVariant, nullVariant);
        Assert.Equal("<null>", defaultVariant.ToString());
    }

    [Fact]
    public void VariantStoresPrimitiveValuesAndEnumsInElectron2DTypes()
    {
        Electron2D.Variant boolean = true;
        Electron2D.Variant integer = 42;
        Electron2D.Variant unsignedInteger = 42u;
        Electron2D.Variant floating = 2.5f;
        Electron2D.Variant text = "player";
        var enumValue = Electron2D.Variant.From(VariantFixtureState.Ready);

        Assert.Equal(Electron2D.Variant.Type.Bool, boolean.VariantType);
        Assert.True(boolean.AsBool());
        Assert.Equal(Electron2D.Variant.Type.Int, integer.VariantType);
        Assert.Equal(42L, integer.AsInt64());
        Assert.Equal(42, integer.AsInt32());
        Assert.Equal(42L, unsignedInteger.AsInt64());
        Assert.Equal(Electron2D.Variant.Type.Float, floating.VariantType);
        Assert.Equal(2.5d, floating.AsDouble());
        Assert.Equal(Electron2D.Variant.Type.String, text.VariantType);
        Assert.Equal("player", text.AsString());
        Assert.Equal(Electron2D.Variant.Type.Int, enumValue.VariantType);
        Assert.Equal(VariantFixtureState.Ready, enumValue.As<VariantFixtureState>());
    }

    [Fact]
    public void VariantStoresTwoDimensionalMathAndIdentityValues()
    {
        var vector = new Electron2D.Vector2(1f, 2f);
        var vectorI = new Electron2D.Vector2I(3, 4);
        var rect = new Electron2D.Rect2(1f, 2f, 3f, 4f);
        var rectI = new Electron2D.Rect2I(5, 6, 7, 8);
        var transform = new Electron2D.Transform2D(1f, 0f, 0f, 1f, 9f, 10f);
        var color = new Electron2D.Color(0.25f, 0.5f, 0.75f, 1f);
        var stringName = new Electron2D.StringName("ready");
        var nodePath = new Electron2D.NodePath("Root/Player:position");
        var rid = default(Electron2D.Rid);
        var callable = Electron2D.Callable.From(() => { });

        Assert.Equal(vector, ((Electron2D.Variant)vector).AsVector2());
        Assert.Equal(vectorI, ((Electron2D.Variant)vectorI).AsVector2I());
        Assert.Equal(rect, ((Electron2D.Variant)rect).AsRect2());
        Assert.Equal(rectI, ((Electron2D.Variant)rectI).AsRect2I());
        Assert.Equal(transform, ((Electron2D.Variant)transform).AsTransform2D());
        Assert.Equal(color, ((Electron2D.Variant)color).AsColor());
        Assert.Equal(stringName, ((Electron2D.Variant)stringName).AsStringName());
        Assert.Equal(nodePath, ((Electron2D.Variant)nodePath).AsNodePath());
        Assert.Equal(rid, ((Electron2D.Variant)rid).AsRid());
        Assert.Equal(callable, ((Electron2D.Variant)callable).AsCallable());
    }

    [Fact]
    public void VariantStoresObjectDerivedValuesAsObject()
    {
        var node = new Electron2D.Node();
        var resource = new Electron2D.Resource();

        Electron2D.Variant nodeVariant = node;
        Electron2D.Variant resourceVariant = resource;

        Assert.Equal(Electron2D.Variant.Type.Object, nodeVariant.VariantType);
        Assert.Same(node, nodeVariant.AsObject());
        Assert.Same(node, nodeVariant.As<Electron2D.Node>());
        Assert.Equal(Electron2D.Variant.Type.Object, resourceVariant.VariantType);
        Assert.Same(resource, resourceVariant.AsObject());
        Assert.Same(resource, resourceVariant.As<Electron2D.Resource>());
    }

    [Fact]
    public void VariantStoresElectron2DCollectionsByReference()
    {
        var array = new Electron2D.Collections.Array();
        array.Add(1);
        array.Add("two");

        var dictionary = new Electron2D.Collections.Dictionary();
        dictionary.Add("name", "player");
        dictionary[42] = true;

        Electron2D.Variant arrayVariant = array;
        Electron2D.Variant dictionaryVariant = dictionary;

        Assert.Equal(Electron2D.Variant.Type.Array, arrayVariant.VariantType);
        Assert.Same(array, arrayVariant.AsArray());
        Assert.Equal(2, arrayVariant.AsArray().Count);
        Assert.Equal(1L, array[0].AsInt64());
        Assert.Equal("two", array[1].AsString());
        Assert.Equal(Electron2D.Variant.Type.Dictionary, dictionaryVariant.VariantType);
        Assert.Same(dictionary, dictionaryVariant.AsDictionary());
        Assert.True(dictionary.TryGetValue("name", out var playerName));
        Assert.Equal("player", playerName.AsString());
        Assert.True(dictionary[42].AsBool());
    }

    [Fact]
    public void VariantRejectsUnsupportedValuesAndWrongCastsWithClearErrors()
    {
        var unsupported = Assert.Throws<ArgumentException>(() => Electron2D.Variant.CreateFrom(new object()));
        var overflowing = Assert.Throws<ArgumentOutOfRangeException>(() => Electron2D.Variant.CreateFrom(ulong.MaxValue));
        Electron2D.Variant text = "42";

        Assert.Contains("System.Object", unsupported.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(UInt64), overflowing.Message, StringComparison.Ordinal);
        var wrongCast = Assert.Throws<InvalidCastException>(() => text.AsInt64());
        Assert.Contains("String", wrongCast.Message, StringComparison.Ordinal);
        Assert.Contains("Int", wrongCast.Message, StringComparison.Ordinal);
    }

    private enum VariantFixtureState
    {
        Idle = 0,
        Ready = 2
    }
}
