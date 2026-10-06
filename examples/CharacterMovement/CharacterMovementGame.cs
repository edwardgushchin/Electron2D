namespace Electron2D.Examples;

internal static class CharacterMovementGame
{
    internal static async Task<int> RunAsync()
    {
        if (OperatingSystem.IsLinux()) RegisterDesktopIcon();
        using var image = new Image();
        image.LoadSVGFromBuffer(LoadAsset("mark-dark.svg"));
        using var character = ImageTexture.CreateFromImage(image);
        using var font = new FontFile { Data = LoadAsset("IBMPlexSans-Regular.ttf") };
        using var appIcon = new Image();
        appIcon.LoadSVGFromBuffer(LoadAsset("app-icon.svg"));
        var fullscreen = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() ||
            OperatingSystem.IsTvOS() || OperatingSystem.IsBrowser();
        using var window = CharacterMovementScene.CreateWindow(character, font, fullscreen);
        if (OperatingSystem.IsLinux() || OperatingSystem.IsWindows())
            window.Ready += _ => DisplayServer.SetIcon(appIcon);
        Engine.MaxFPS = 60;
        return fullscreen ? await Engine.RunAsync(window) : Engine.Run(window);
    }

    private static void RegisterDesktopIcon()
    {
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("The game executable path is unavailable.");
        var data = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrEmpty(data) || !System.IO.Path.IsPathFullyQualified(data))
            data = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        var applications = System.IO.Path.Combine(data, "applications");
        var icons = System.IO.Path.Combine(data, "icons", "hicolor", "512x512", "apps");
        Directory.CreateDirectory(applications);
        Directory.CreateDirectory(icons);
        var icon = System.IO.Path.Combine(icons, "CharacterMovement.png");
        File.WriteAllBytes(icon, LoadAsset("app-icon.png"));
        var quoted = executable.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("`", "\\`").Replace("$", "\\$").Replace("%", "%%");
        var command = ("\"" + quoted + "\"").Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r");
        var iconEntry = icon.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r");
        File.WriteAllText(System.IO.Path.Combine(applications, "CharacterMovement.desktop"), $"""
            [Desktop Entry]
            Type=Application
            Name=CharacterMovement
            Exec={command}
            Icon={iconEntry}
            StartupWMClass=CharacterMovement
            Terminal=false
            Categories=Game;

            """);
    }

    private static byte[] LoadAsset(string name)
    {
        using var source = typeof(CharacterMovementGame).Assembly.GetManifestResourceStream("CharacterMovement." + name)
            ?? throw new InvalidOperationException("Missing bundled game asset: " + name);
        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        return buffer.ToArray();
    }
}
