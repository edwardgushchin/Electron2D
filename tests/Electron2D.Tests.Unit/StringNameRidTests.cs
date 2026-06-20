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

public sealed class StringNameRidTests
{
    [Fact]
    public void StringNameSupportsInternedNameEqualityAndHashing()
    {
        var ready = new Electron2D.StringName("ready");
        Electron2D.StringName implicitReady = "ready";
        var differentCase = new Electron2D.StringName("Ready");

        Assert.Equal(ready, implicitReady);
        Assert.True(ready == implicitReady);
        Assert.False(ready != implicitReady);
        Assert.True(ready == "ready");
        Assert.True("ready" == ready);
        Assert.True(ready != "Ready");
        Assert.NotEqual(ready, differentCase);
        Assert.Equal(ready.GetHashCode(), implicitReady.GetHashCode());
        Assert.Equal("ready", ready.ToString());

        var values = new Dictionary<Electron2D.StringName, int>
        {
            [ready] = 10
        };

        Assert.Equal(10, values[implicitReady]);
    }

    [Fact]
    public void StringNameDefaultNullAndEmptyValuesAreEmpty()
    {
        var defaultName = default(Electron2D.StringName);
        var nullName = new Electron2D.StringName(null);
        var emptyName = new Electron2D.StringName(string.Empty);

        Assert.True(defaultName.IsEmpty());
        Assert.True(nullName.IsEmpty());
        Assert.True(emptyName.IsEmpty());
        Assert.Equal(defaultName, nullName);
        Assert.Equal(nullName, emptyName);
        Assert.Equal(string.Empty, defaultName.ToString());
        Assert.True(defaultName == string.Empty);
    }

    [Fact]
    public void RidDefaultValueIsInvalidAndComparable()
    {
        var invalid = default(Electron2D.Rid);

        Assert.False(invalid.IsValid());
        Assert.Equal(0L, invalid.GetId());
        Assert.Equal("Rid(0)", invalid.ToString());
        Assert.Equal(default, invalid);
        Assert.True(invalid == default);
        Assert.False(invalid != default);
        Assert.Equal(0, invalid.CompareTo(default));
        Assert.Equal(invalid.GetHashCode(), default(Electron2D.Rid).GetHashCode());
    }
}
