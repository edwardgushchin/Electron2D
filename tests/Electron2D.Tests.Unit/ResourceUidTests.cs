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

public sealed class ResourceUidTests
{
    [Fact]
    public void CreateIdForPathIsStableForTheSamePath()
    {
        var first = Electron2D.ResourceUid.CreateIdForPath("res://textures/player.png");
        var second = Electron2D.ResourceUid.CreateIdForPath("res://textures/player.png");
        var other = Electron2D.ResourceUid.CreateIdForPath("res://textures/enemy.png");

        Assert.NotEqual(Electron2D.ResourceUid.InvalidId, first);
        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
        Assert.Equal(first, Electron2D.ResourceUid.TextToId(Electron2D.ResourceUid.IdToText(first)));
    }

    [Fact]
    public void RegisteredUidSurvivesRenameMoveThroughSetId()
    {
        const long id = 987654321L;
        RemoveIfExists(id);

        Electron2D.ResourceUid.AddId(id, "res://characters/player.e2res");

        Assert.True(Electron2D.ResourceUid.HasId(id));
        Assert.Equal("res://characters/player.e2res", Electron2D.ResourceUid.GetIdPath(id));
        Assert.Equal("uid://gc0uy9", Electron2D.ResourceUid.IdToText(id));
        Assert.Equal(id, Electron2D.ResourceUid.TextToId("uid://gc0uy9"));
        Assert.Equal("uid://gc0uy9", Electron2D.ResourceUid.PathToUid("res://characters/player.e2res"));

        Electron2D.ResourceUid.SetId(id, "res://actors/player.e2res");

        Assert.Equal("res://actors/player.e2res", Electron2D.ResourceUid.GetIdPath(id));
        Assert.Equal("uid://gc0uy9", Electron2D.ResourceUid.PathToUid("res://actors/player.e2res"));
        Assert.Equal("res://actors/player.e2res", Electron2D.ResourceUid.UidToPath("uid://gc0uy9"));
        Assert.Equal("res://actors/player.e2res", Electron2D.ResourceUid.EnsurePath("uid://gc0uy9"));
        Assert.Equal("res://textures/free.png", Electron2D.ResourceUid.EnsurePath("res://textures/free.png"));
        Assert.Equal("res://textures/free.png", Electron2D.ResourceUid.PathToUid("res://textures/free.png"));

        RemoveIfExists(id);
    }

    [Fact]
    public void InvalidUidTextUsesGodotLikeInvalidMarker()
    {
        Assert.Equal("uid://<invalid>", Electron2D.ResourceUid.IdToText(Electron2D.ResourceUid.InvalidId));
        Assert.Equal(Electron2D.ResourceUid.InvalidId, Electron2D.ResourceUid.TextToId("uid://<invalid>"));
        Assert.Equal(Electron2D.ResourceUid.InvalidId, Electron2D.ResourceUid.TextToId("uid://not a uid"));
        Assert.Equal(string.Empty, Electron2D.ResourceUid.UidToPath("uid://missing"));
        Assert.Equal(string.Empty, Electron2D.ResourceUid.EnsurePath("uid://missing"));
    }

    private static void RemoveIfExists(long id)
    {
        if (Electron2D.ResourceUid.HasId(id))
        {
            Electron2D.ResourceUid.RemoveId(id);
        }
    }
}
