using System.Reflection;
using Xunit;

namespace Electron2D.Tests.RuntimeSmoke;

public sealed class RuntimeBaselineTests
{
    [Fact]
    public void RuntimeAssemblyCanBeLoaded()
    {
        var assembly = Assembly.Load("Electron2D");

        Assert.Equal("Electron2D", assembly.GetName().Name);
    }

    [Fact]
    [Trait("Category", "Baseline")]
    public void SceneTreeBaselineFailsUntilNodeExists()
    {
        var assembly = Assembly.Load("Electron2D");

        Assert.NotNull(assembly.GetType("Electron2D.Node"));
    }
}
