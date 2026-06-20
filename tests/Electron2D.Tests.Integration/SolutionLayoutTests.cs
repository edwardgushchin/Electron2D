using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class SolutionLayoutTests
{
    [Fact]
    public void RepositoryContainsRuntimeProjectAndReleaseSpecification()
    {
        var root = FindRepositoryRoot();

        Assert.True(File.Exists(Path.Combine(root, "src", "Electron2D", "Electron2D.csproj")));
        Assert.True(File.Exists(Path.Combine(root, "docs", "specifications", "releases", "0.1.0-preview.md")));
        Assert.True(File.Exists(Path.Combine(root, "TASKS.md")));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }
}
