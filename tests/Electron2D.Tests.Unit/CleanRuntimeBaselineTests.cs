using System.Reflection;
using Xunit;

namespace Electron2D.Tests.Unit;

public sealed class CleanRuntimeBaselineTests
{
    [Fact]
    public void RuntimeAssemblyExportsOnlyCurrentGodotLikeBaselineTypes()
    {
        var assembly = Assembly.Load("Electron2D");
        var publicTypeNames = assembly
            .GetExportedTypes()
            .Select(type => type.FullName)
            .OrderBy(typeName => typeName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "Electron2D.Callable",
                "Electron2D.ConnectFlags",
                "Electron2D.Error",
                "Electron2D.InputEvent",
                "Electron2D.Node",
                "Electron2D.NodePath",
                "Electron2D.Object",
                "Electron2D.PackedScene",
                "Electron2D.RefCounted",
                "Electron2D.Resource",
                "Electron2D.SceneTree"
            },
            publicTypeNames);

        Assert.Null(assembly.GetType("Electron2D.IComponent"));
        Assert.Null(assembly.GetType("Electron2D.SpriteRenderer"));
        Assert.Null(assembly.GetType("Electron2D.SpriteAnimator"));
        Assert.Null(assembly.GetType("Electron2D.Rigidbody"));
        Assert.Null(assembly.GetType("Electron2D.Collider"));
    }
}
