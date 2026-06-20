using Xunit;

namespace Electron2D.Tests.Unit;

public sealed class BaseObjectLifetimeTests
{
    [Fact]
    public void ObjectInstanceIdsAreStableUniqueAndNonZero()
    {
        var first = new Electron2D.Object();
        var second = new Electron2D.Object();
        var firstId = first.GetInstanceId();

        first.Free();

        Assert.NotEqual(0UL, firstId);
        Assert.NotEqual(firstId, second.GetInstanceId());
        Assert.Equal(firstId, first.GetInstanceId());
    }

    [Fact]
    public void ObjectFreeIsIdempotentAndInvalidatesInstance()
    {
        var value = new Electron2D.Object();

        value.Free();
        value.Free();

        Assert.False(Electron2D.Object.IsInstanceValid(value));
        Assert.False(Electron2D.Object.IsInstanceValid(null));
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
        Assert.False(Electron2D.Object.IsInstanceValid(value));
        Assert.False(value.Reference());
    }

    [Fact]
    public void ResourceCarriesGodotLikeIdentityFields()
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
