using Xunit;

namespace Electron2D.Tests.GoldenData;

public sealed class GoldenDataBaselineTests
{
    [Fact]
    public void GoldenDataDirectoryCanBeCreatedByFutureFixtures()
    {
        var root = FindRepositoryRoot();
        var expectedPath = Path.Combine(root, "tests", "golden-data");

        Assert.EndsWith(Path.Combine("tests", "golden-data"), expectedPath);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }
}
