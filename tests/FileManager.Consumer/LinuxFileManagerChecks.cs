using Electron2D;
using IOPath = System.IO.Path;

internal static class LinuxFileManagerChecks
{
    internal static void Run(string scenario, string directory, string file)
    {
        switch (scenario)
        {
            case "open-directory":
                OS.ShellShowInFileManager(directory + IOPath.DirectorySeparatorChar);
                break;
            case "select-directory":
                OS.ShellShowInFileManager(directory + IOPath.DirectorySeparatorChar, openFolder: false);
                break;
            case "select-file":
                OS.ShellShowInFileManager(file);
                break;
            case "select-relative-file":
                OS.ShellShowInFileManager(IOPath.GetRelativePath(Environment.CurrentDirectory, file), openFolder: false);
                break;
            case "rejected":
                Reject<IOException>(() => OS.ShellShowInFileManager(file), "Probe rejected selection");
                break;
            case "invalid":
                Reject<ArgumentNullException>(() => OS.ShellShowInFileManager(null!));
                Reject<ArgumentException>(() => OS.ShellShowInFileManager(""));
                Reject<ArgumentException>(() => OS.ShellShowInFileManager("bad\0path"));
                Reject<IOException>(() => OS.ShellShowInFileManager(file + ".absent"));
                break;
            default:
                throw new ArgumentException("Unknown Linux scenario.", nameof(scenario));
        }
    }

    private static void Reject<T>(Action action, string? message = null) where T : Exception
    {
        try
        {
            action();
        }
        catch (T error) when (message is null || error.Message.Contains(message, StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
