using Electron2D;
using Electron2D.Editor;

if (OperatingSystem.IsLinux()) RegisterDesktopApplication();
using var texture = ResourceLoader.Load<ImageTexture>(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "logo-stacked-dark.svg"));
using var mark = ResourceLoader.Load<ImageTexture>(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "mark-dark.svg"));
using var wordmark = new AtlasTexture { Atlas = texture, Region = new(24, 160, 440, 68), FilterClip = true };
using var font = new FontFile();
font.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf"));
Engine.MaxFPS = 60;
return Engine.Run(EditorScene.CreateWindow(mark, wordmark, font));

// Desktop shells match the executable's application ID to this entry on both Linux display protocols.
static void RegisterDesktopApplication()
{
    var executable = Environment.ProcessPath ?? throw new InvalidOperationException("The editor executable path is unavailable.");
    if (System.IO.Path.GetFileName(executable) != "Electron2D.Editor")
        throw new InvalidOperationException("Launch the Electron2D.Editor executable rather than its DLL.");
    var data = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
    if (string.IsNullOrEmpty(data) || !System.IO.Path.IsPathFullyQualified(data))
        data = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
    var applications = System.IO.Path.Combine(data, "applications");
    var icons = System.IO.Path.Combine(data, "icons", "hicolor", "scalable", "apps");
    Directory.CreateDirectory(applications);
    Directory.CreateDirectory(icons);
    File.Copy(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "mark-dark.svg"),
        System.IO.Path.Combine(icons, "Electron2D.Editor.svg"), overwrite: true);
    // Exec quoting and desktop-string escaping are separate layers; %% is a literal percent in Exec.
    var quoted = executable.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("`", "\\`").Replace("$", "\\$").Replace("%", "%%");
    var command = ("\"" + quoted + "\"").Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r");
    File.WriteAllText(System.IO.Path.Combine(applications, "Electron2D.Editor.desktop"), $"""
        [Desktop Entry]
        Type=Application
        Name=Electron2D
        Comment=Agent-native cross-platform game engine
        Exec=/usr/bin/env {command}
        Icon=Electron2D.Editor
        StartupWMClass=Electron2D.Editor
        Terminal=false
        Categories=Development;IDE;

        """);
}
