using Electron2D;
using IOPath = System.IO.Path;

internal sealed class BuiltInIncludeFiles : IDisposable
{
    internal string DirectoryPath { get; } = IOPath.Combine(IOPath.GetTempPath(), "electron2d-includes-" + Guid.NewGuid().ToString("N"));
    internal BuiltInIncludeFiles()
    {
        try
        {
            foreach (var name in ShaderIncludeDB.ListBuiltInIncludeFiles())
            {
                var path = IOPath.Combine(DirectoryPath, name);
                Directory.CreateDirectory(IOPath.GetDirectoryName(path)!);
                File.WriteAllText(path, ShaderIncludeDB.GetBuiltInIncludeFile(name), new System.Text.UTF8Encoding(false, true));
            }
        }
        catch { Dispose(); throw; }
    }
    public void Dispose() { if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, recursive: true); }
}
