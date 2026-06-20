using System.Reflection;
using Xunit;

namespace Electron2D.Tests.Unit;

public sealed class CleanRuntimeBaselineTests
{
    [Fact]
    public void RuntimeAssemblyStartsWithoutPublicLegacyTypes()
    {
        var assembly = Assembly.Load("Electron2D");
        var publicTypeNames = assembly
            .GetExportedTypes()
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(publicTypeNames);
        Assert.Null(assembly.GetType("Electron2D.IComponent"));
        Assert.Null(assembly.GetType("Electron2D.SpriteRenderer"));
        Assert.Null(assembly.GetType("Electron2D.SpriteAnimator"));
        Assert.Null(assembly.GetType("Electron2D.Rigidbody"));
        Assert.Null(assembly.GetType("Electron2D.Collider"));
    }
}
