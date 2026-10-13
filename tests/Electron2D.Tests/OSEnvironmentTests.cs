using System.Reflection;
using Path = System.IO.Path;
using System.Text.Json;
using Electron2D;
using static TextureArrayTests;

internal static class OSEnvironmentTests
{
    internal static void Child(string[] args) => File.WriteAllText(args[1], JsonSerializer.Serialize(new { Value = OS.GetEnvironment(args[2]), Exists = OS.HasEnvironment(args[2]) }));

    internal static void Run()
    {
        var variable = "ELECTRON2D_ENVIRONMENT_" + Guid.NewGuid().ToString("N");
        var root = Path.Combine(Path.GetTempPath(), "electron2d-os-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Check(!OS.HasEnvironment(variable) && OS.GetEnvironment(variable) == "", "Absent environment.");
            OS.UnsetEnvironment(variable);
            foreach (var value in new[] { "literal λ = ; $()\n", "", "replacement" })
            {
                OS.SetEnvironment(variable, value);
                Check(OS.HasEnvironment(variable) && OS.GetEnvironment(variable) == value && Environment.GetEnvironmentVariable(variable) == value, "Managed environment preserves literal and empty values.");
                ChildCheck(root, variable, value, true);
                if (OperatingSystem.IsLinux())
                {
                    var output = new List<string>();
                    var code = OS.Execute("/bin/sh", ["-c", "printf '%s' \"${" + variable + "}\""], output);
                    Check(code == 0 && output.Single() == value, "Native SDL launch inherits updated value.");
                }
            }
            OS.UnsetEnvironment(variable);
            Check(!OS.HasEnvironment(variable), "Unset value.");
            ChildCheck(root, variable, "", false);
            foreach (var invalid in new[] { "", "a=b", "a\0b" })
            {
                Reject<ArgumentException>(() => OS.GetEnvironment(invalid));
                Reject<ArgumentException>(() => OS.HasEnvironment(invalid));
                Reject<ArgumentException>(() => OS.SetEnvironment(invalid, "x"));
                Reject<ArgumentException>(() => OS.UnsetEnvironment(invalid));
            }
            Reject<ArgumentNullException>(() => OS.SetEnvironment(variable, null!));
            Reject<ArgumentException>(() => OS.SetEnvironment(variable, "x\0y"));
            Reject<System.Text.EncoderFallbackException>(() => OS.SetEnvironment(variable, "\ud800"));
            Reject<System.Text.EncoderFallbackException>(() => OS.SetEnvironment("\ud800", "x"));
            Check(!OS.HasEnvironment(variable), "Rejected edits preserve absent state.");
            Directories(root);
            Console.WriteLine("OS environment, child inheritance, directories and user:// checks passed.");
        }
        finally { OS.UnsetEnvironment(variable); Directory.Delete(root, true); }
    }

    private static void ChildCheck(string root, string variable, string expected, bool exists)
    {
        var file = Path.Combine(root, "child.json");
        File.Delete(file);
        var path = OS.GetExecutablePath();
        var args = new List<string>();
        if (Path.GetFileNameWithoutExtension(path).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) args.Add(Assembly.GetExecutingAssembly().Location);
        args.AddRange(["--os-environment-child", file, variable]);
        var pid = OS.CreateProcess(path, args.ToArray());
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        while (OS.IsProcessRunning(pid) && deadline.Elapsed < TimeSpan.FromSeconds(15)) Thread.Sleep(10);
        if (OS.IsProcessRunning(pid)) { OS.Kill(pid); throw new InvalidOperationException("Environment child did not exit."); }
        Check(OS.GetProcessExitCode(pid) == 0 && File.Exists(file), "Environment child exited and wrote its result.");
        using var result = JsonDocument.Parse(File.ReadAllText(file));
        Check(result.RootElement.GetProperty("Value").GetString() == expected && result.RootElement.GetProperty("Exists").GetBoolean() == exists, "Fresh child sees the selected environment.");
    }

    private static void Directories(string root)
    {
        Check(Path.IsPathFullyQualified(OS.GetConfigDir()) && Path.IsPathFullyQualified(OS.GetDataDir()) && Path.IsPathFullyQualified(OS.GetCacheDir()) && Path.IsPathFullyQualified(OS.GetTempDir()), "Global directories are rooted.");
        Reject<ArgumentOutOfRangeException>(() => OS.GetSystemDir((OS.SystemDir)(-1)));
        Reject<ArgumentOutOfRangeException>(() => OS.GetSystemDir((OS.SystemDir)8));
        foreach (var dir in Enum.GetValues<OS.SystemDir>())
        {
            if (OperatingSystem.IsMacOS() && dir is OS.SystemDir.DCIM or OS.SystemDir.Ringtones) { Reject<PlatformNotSupportedException>(() => OS.GetSystemDir(dir)); continue; }
            var path = OS.GetSystemDir(dir);
            Check(Path.IsPathFullyQualified(path) && path == OS.GetSystemDir(dir, false), "Native directory and desktop storage selector.");
        }
        if (OperatingSystem.IsLinux())
        {
            Check(OS.GetTempDir() == "/tmp", "Linux global temporary directory.");
            Check(OS.GetSystemDir(OS.SystemDir.DCIM) == OS.GetSystemDir(OS.SystemDir.Pictures) && OS.GetSystemDir(OS.SystemDir.Ringtones) == OS.GetSystemDir(OS.SystemDir.Music), "Desktop camera/ringtone roles.");
            foreach (var entry in new (string Name, Func<string> Get, string Suffix)[] { ("XDG_CONFIG_HOME", OS.GetConfigDir, ".config"), ("XDG_DATA_HOME", OS.GetDataDir, ".local/share"), ("XDG_CACHE_HOME", OS.GetCacheDir, ".cache") })
            {
                var prior = Environment.GetEnvironmentVariable(entry.Name);
                try
                {
                    var absent = Path.Combine(root, entry.Name, "λ");
                    OS.SetEnvironment(entry.Name, absent);
                    Check(entry.Get() == absent && !Directory.Exists(absent), "XDG lookup does not create directories.");
                    foreach (var value in new[] { "relative/path", "" })
                    {
                        OS.SetEnvironment(entry.Name, value);
                        Check(entry.Get() == Path.Combine(Environment.GetEnvironmentVariable("HOME")!, entry.Suffix), "Relative/empty XDG falls back to home.");
                    }
                }
                finally { if (prior is null) OS.UnsetEnvironment(entry.Name); else OS.SetEnvironment(entry.Name, prior); }
            }
        }
        var project = ProjectSettings.ProjectRoot; var user = ProjectSettings.UserDataRoot; var configured = false;
        try
        {
            var target = Path.Combine(root, "user"); Directory.CreateDirectory(target);
            ProjectSettings.ConfigurePaths(root, target); configured = true;
            Check(OS.GetUserDataDir() == target && ProjectSettings.GlobalizePath("user://") == target, "User root matches virtual resolution.");
            using (var file = Electron2D.FileAccess.Open("user://saved.txt", FileAccessModeFlags.Write)) file.WriteString("saved λ");
            Check(File.ReadAllText(Path.Combine(OS.GetUserDataDir(), "saved.txt")) == "saved λ", "Public FileAccess saves at the reported user root.");
        }
        finally { if (configured) ProjectSettings.ConfigurePaths(project, user); }
    }
}
