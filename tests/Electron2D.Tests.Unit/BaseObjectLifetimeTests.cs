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

public sealed class BaseObjectLifetimeTests
{
    [Fact]
    public void ObjectInstanceIdsAreStableUniqueAndNonZero()
    {
        var first = new Electron2D.ElectronObject();
        var second = new Electron2D.ElectronObject();
        var firstId = first.GetInstanceId();

        first.Free();

        Assert.NotEqual(0UL, firstId);
        Assert.NotEqual(firstId, second.GetInstanceId());
        Assert.Equal(firstId, first.GetInstanceId());
    }

    [Fact]
    public void ObjectFreeIsIdempotentAndInvalidatesInstance()
    {
        var value = new Electron2D.ElectronObject();

        value.Free();
        value.Free();

        Assert.False(Electron2D.ElectronObject.IsInstanceValid(value));
        Assert.False(Electron2D.ElectronObject.IsInstanceValid(null));
    }

    [Fact]
    public void RefCountedFreesWhenReferenceCountReachesZero()
    {
        var value = new Electron2D.RefCounted();

        Assert.Equal(1, value.GetReferenceCount());
        Assert.True(value.Reference());
        Assert.Equal(2, value.GetReferenceCount());
        Assert.False(value.Unreference());
        Assert.Equal(1, value.GetReferenceCount());
        Assert.True(value.Unreference());

        Assert.Equal(0, value.GetReferenceCount());
        Assert.False(Electron2D.ElectronObject.IsInstanceValid(value));
        Assert.False(value.Reference());
    }

    [Fact]
    public void ResourceCarriesElectron2DIdentityFields()
    {
        var resource = new Electron2D.Resource
        {
            ResourceName = "Player",
            ResourceLocalToScene = true,
            ResourceSceneUniqueId = "player-scene-id"
        };

        resource.TakeOverPath("res://player.tres");

        Assert.Equal("Player", resource.ResourceName);
        Assert.Equal("res://player.tres", resource.ResourcePath);
        Assert.True(resource.ResourceLocalToScene);
        Assert.Equal("player-scene-id", resource.ResourceSceneUniqueId);
    }
}
