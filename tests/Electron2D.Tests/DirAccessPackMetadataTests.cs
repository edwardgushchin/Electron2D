using Electron2D;
using System.Diagnostics;
using Path = System.IO.Path;

internal static class DirAccessPackMetadataTests
{
    internal static void Run()
    {
        const string node = "dir-pack-metadata-node";
        const string child = node + "/child";
        const string resourceNode = "res://" + node;
        const string marker = node + "/marker";
        const string physicalDirectory = "physical-target";
        var oldProject = ProjectSettings.ProjectRoot;
        var oldUser = ProjectSettings.UserDataRoot;
        var fixture = Directory.CreateTempSubdirectory("electron2d-dir-pack-metadata-");
        try
        {
            ProjectSettings.ConfigurePaths(fixture.FullName, fixture.CreateSubdirectory("user").FullName);
            var target = fixture.CreateSubdirectory(physicalDirectory);
            var childTarget = fixture.CreateSubdirectory("physical-child-target");
            File.WriteAllText(Path.Combine(target.FullName, "marker"), "physical");
            CreateDirectoryLink(fixture.FullName, node, target.Name);
            CreateDirectoryLink(fixture.FullName, Path.Combine(target.Name, "child"), Path.Combine("..", childTarget.Name));

            var filePack = Path.Combine(fixture.FullName, "file.pck");
            var directoryPack = Path.Combine(fixture.FullName, "directory.pck");
            WritePack(filePack, child, node);
            WritePack(directoryPack, node, child);

            using var resource = DirAccess.Open("res://");
            using var physical = DirAccess.Open(fixture.FullName);
            var physicalTarget = physical.ReadLink(node);
            Check(physicalTarget.Contains(physicalDirectory, StringComparison.Ordinal) && physical.IsLink(node) &&
                  resource.ReadLink(node) == physicalTarget && resource.IsLink(node) && resource.DirExists(node) &&
                  DirAccess.DirExistsAbsolute(resourceNode) && resource.FileExists(marker) && resource.IsLink(child),
                "The ordinary path exposes the physical directory, its link target and its child.");

            Mount(filePack);
            Check(resource.FileExists(node) && !resource.DirExists(node) && !DirAccess.DirExistsAbsolute(resourceNode) &&
                  !resource.IsLink(node), "The mounted file masks the reparse metadata.");
            Check(!resource.FileExists(marker) && !resource.DirExists(child), "The mounted file ancestor masks physical metadata.");
            ExpectMissingLink(resource, node);
            ExpectMissingLink(resource, child);
            if (OperatingSystem.IsWindows())
                Check(!resource.IsLink(child), "Windows link detection respects the mounted file ancestor.");
            else
            {
                try
                {
                    resource.IsLink(child);
                    throw new InvalidOperationException("Unix link detection must report the masked descendant as missing.");
                }
                catch (FileNotFoundException) { }
            }

            Mount(directoryPack);
            Check(resource.DirExists(node) && DirAccess.DirExistsAbsolute(resourceNode) && !resource.FileExists(node) &&
                  !resource.IsLink(node) && resource.FileExists(child) && !resource.IsLink(child),
                "The mounted directory masks the exact physical link.");
            Check(resource.FileExists(marker), "The mounted directory still merges unmasked physical descendants.");
            ExpectMissingLink(resource, node);
            ExpectMissingLink(resource, child);

            var mounts = Task.Run(() =>
            {
                for (var i = 0; i < 4096; i++)
                {
                    Mount(filePack);
                    Mount(directoryPack);
                }
            });
            int queries = 0, exposedLinkFlags = 0, exposedTargets = 0, missingLinkFlags = 0;
            try
            {
                do
                {
                    try
                    {
                        if (resource.IsLink(node)) exposedLinkFlags++;
                    }
                    catch (FileNotFoundException) { missingLinkFlags++; }
                    try
                    {
                        _ = resource.ReadLink(node);
                        exposedTargets++;
                    }
                    catch (FileNotFoundException) { }
                    queries++;
                }
                while (!mounts.IsCompleted);
            }
            finally
            {
                mounts.GetAwaiter().GetResult();
            }

            Check(physical.IsLink(node) && physical.ReadLink(node) == physicalTarget && physical.FileExists(marker),
                "The absolute physical path still reads the original link and child after replacements.");
            var result = $"DirAccess mount race: {queries} IsLink/ReadLink pairs, {exposedLinkFlags} physical link flags, " +
                         $"{exposedTargets} physical targets, {missingLinkFlags} unexpected missing-entry exceptions.";
            Console.WriteLine(result);
            Check(exposedLinkFlags == 0 && exposedTargets == 0 && missingLinkFlags == 0, result);
        }
        finally
        {
            ProjectSettings.ConfigurePaths(oldProject, oldUser);
            // Remove directory links directly before recursively deleting their physical parents.
            var childLink = Path.Combine(fixture.FullName, physicalDirectory, "child");
            if (Directory.Exists(childLink))
                Directory.Delete(childLink);
            var directoryLink = Path.Combine(fixture.FullName, node);
            if (Directory.Exists(directoryLink))
                Directory.Delete(directoryLink);
            fixture.Delete(recursive: true);
        }
    }

    private static void Mount(string pack) => Check(ProjectSettings.LoadResourcePack(pack), "Mount the PCK fixture.");

    internal static void CreateDirectoryLink(string root, string link, string relativeTarget)
    {
        var path = Path.Combine(root, link);
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(path, relativeTarget);
            return;
        }

        // A junction exercises reparse metadata without requiring Windows symbolic-link privilege.
        var parent = Path.GetDirectoryName(path)!;
        var target = Path.GetFullPath(relativeTarget, parent);
        var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"),
            $"/d /c mklink /J {Path.GetFileName(path)} \"{target}\"")
        {
            WorkingDirectory = parent,
            UseShellExecute = false
        };
        using var junction = Process.Start(start)!;
        junction.WaitForExit();
        Check(junction.ExitCode == 0, "Create the physical Windows junction.");
    }

    private static void WritePack(string pack, string remove, string add)
    {
        using var writer = new PCKPacker();
        writer.PCKStart(pack);
        writer.AddFileRemoval(remove);
        writer.AddFileFromBuffer(add, [1]);
    }

    private static void ExpectMissingLink(DirAccess directory, string path)
    {
        try { directory.ReadLink(path); }
        catch (FileNotFoundException) { return; }
        throw new InvalidOperationException($"A mounted entry exposed the physical link target at '{path}'.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
