using Electron2D;
using System.Diagnostics;

internal static class ProjectThemeTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    internal static void RunManaged()
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Electron2D-theme-" + Guid.NewGuid().ToString("N")); System.IO.Directory.CreateDirectory(folder);
        try
        {
            using var theme = CreateTheme(); var path = System.IO.Path.Combine(folder, "theme.e2dres"); ResourceSaver.Save(theme, path);
            using var loaded = ResourceLoader.Load<Theme>(path, ResourceLoader.CacheMode.Ignore); VerifyTheme(loaded);
            Check(!ReferenceEquals(theme.DefaultFont, loaded.DefaultFont), "Embedded graph is restored independently.");
            var root = new SubViewport { Size = new(200, 120) }; var bar = new ProgressBar { Name = "Progress", Theme = theme, ThemeTypeVariation = "ProjectProgress", Value = 37.25, Size = new(180, 28) }; root.AddChild(bar); bar.Owner = root;
            using var tree = new SceneTree(root); using var scene = new PackedScene(); scene.Pack(root); var scenePath = System.IO.Path.Combine(folder, "theme.e2dscene"); ResourceSaver.Save(scene, scenePath);
            using var restoredScene = ResourceLoader.Load<PackedScene>(scenePath, ResourceLoader.CacheMode.Ignore); using var restored = restoredScene.Instantiate(); var restoredBar = (ProgressBar)restored.GetNode("Progress"); VerifyTheme(restoredBar.Theme!);
            Check(restoredBar.Value == 37.25 && restoredBar.ThemeTypeVariation == "ProjectProgress", "Scene preserves actual authored theme and widget settings.");
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(ProjectThemeTests).Assembly.Location); start.ArgumentList.Add("--project-theme-archive"); start.ArgumentList.Add(folder); start.Environment.Remove("ELECTRON2D_TEST_PROJECT_THEME");
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Theme archive child exceeded its deadline."); }
            Check(child.ExitCode == 0, "Fresh theme process: " + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
            VerifyStartup(folder, path);
            Console.WriteLine("Theme archive/startup: six categories, defaults/nulls, empty groups, variation order, aliases, actual themed scene, precedence, ownership/failure and fresh process passed.");
        }
        finally { System.IO.Directory.Delete(folder, true); }
    }
    private static Theme CreateTheme()
    {
        var theme = new Theme { DefaultBaseScale = 1.25f, DefaultFontSize = 20, DefaultFont = ThemeDB.FallbackFont };
        theme.AddType("EmptyType"); theme.SetColor("accent", "OnlyColor", new(.2f, .4f, .6f)); theme.SetConstant("signed", "ProjectProgress", -7); theme.SetFontSize("raw_size", "ProjectProgress", -3);
        theme.SetFont("font", "ProjectProgress", theme.DefaultFont); theme.SetFont("null_font", "ProjectProgress", null); theme.SetIcon("null_icon", "ProjectProgress", null); theme.SetStyleBox("null_style", "ProjectProgress", null);
        var fill = new StyleBoxFlat { BGColor = Colors.Red, AntiAliasing = false }; var background = new StyleBoxFlat { BGColor = Colors.Blue, AntiAliasing = false }; theme.SetStyleBox("fill", "ProjectProgress", fill); theme.SetStyleBox("background", "ProjectProgress", background); theme.SetStyleBox("same_fill", "ProjectProgress", fill);
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.Green); var icon = ImageTexture.CreateFromImage(image); theme.SetIcon("icon", "ProjectProgress", icon); theme.SetIcon("same_icon", "ProjectProgress", icon);
        theme.SetTypeVariation("ProjectProgress", "ProgressBar"); theme.SetTypeVariation("First", "ProjectProgress"); theme.SetTypeVariation("Second", "ProjectProgress"); theme.SetTypeVariation("First", "OtherBase"); theme.SetTypeVariation("First", "ProjectProgress");
        return theme;
    }
    private static void VerifyTheme(Theme theme)
    {
        Check(theme.DefaultBaseScale == 1.25f && theme.DefaultFontSize == 20 && theme.DefaultFont is not null && ReferenceEquals(theme.DefaultFont, theme.GetFont("font", "ProjectProgress")), "Defaults and font aliases.");
        Check(theme.GetColor("accent", "OnlyColor") == new Color(.2f, .4f, .6f) && !theme.GetConstantTypeList().Contains("OnlyColor"), "Category groups remain distinct.");
        foreach (var category in Enum.GetValues<Theme.DataType>().Where(kind => kind != Theme.DataType.Max)) Check(theme.GetThemeItemTypeList(category).Contains("EmptyType"), "Empty group retained for " + category);
        Check(theme.GetConstant("signed", "ProjectProgress") == -7 && theme.GetPropertyList().OfType<PropertyDescriptor<Theme, int>>().Single(p => p.Name == "ProjectProgress/font_sizes/raw_size").GetValue(theme) == -3, "Signed constants and raw font-size slots.");
        Check(theme.GetFontList("ProjectProgress").Contains("null_font") && theme.GetIconList("ProjectProgress").Contains("null_icon") && theme.GetStyleBoxList("ProjectProgress").Contains("null_style"), "Explicit null entries survive.");
        Check(ReferenceEquals(theme.GetStyleBox("fill", "ProjectProgress"), theme.GetStyleBox("same_fill", "ProjectProgress")) && ReferenceEquals(theme.GetIcon("icon", "ProjectProgress"), theme.GetIcon("same_icon", "ProjectProgress")), "Resource aliases survive.");
        Check(theme.GetTypeVariationList("ProjectProgress").SequenceEqual(new[] { "Second", "First" }), "Reparented variation child order survives.");
    }
    internal static void RunArchive(string folder)
    {
        using var theme = ResourceLoader.Load<Theme>(System.IO.Path.Combine(folder, "theme.e2dres"), ResourceLoader.CacheMode.Ignore); VerifyTheme(theme);
        using var scene = ResourceLoader.Load<PackedScene>(System.IO.Path.Combine(folder, "theme.e2dscene"), ResourceLoader.CacheMode.Ignore); using var root = scene.Instantiate(); using var tree = new SceneTree(root); var bar = (ProgressBar)root.GetNode("Progress"); VerifyTheme(bar.Theme!); tree.ProcessFrame(.01);
        Check(bar.GetThemeStyleBox("fill") is StyleBoxFlat { BGColor: var color } && color == Colors.Red, "Fresh scene consumes its real variation style.");
    }
    private static void VerifyStartup(string folder, string path)
    {
        var oldPath = ProjectSettings.Get(ProjectSettings.GUIThemeCustom); var oldFontPath = ProjectSettings.Get(ProjectSettings.GUIThemeCustomFont);
        var originalDefault = ThemeDB.GetDefaultTheme().DefaultFont; var originalFallback = ThemeDB.FallbackFont;
        try
        {
            ProjectSettings.Set(ProjectSettings.GUIThemeCustom, path); ProjectSettings.Set(ProjectSettings.GUIThemeCustomFont, "");
            var root = new SubViewport { Size = new(300, 150) }; var bar = new ProgressBar { Name = "ProjectBar", ThemeTypeVariation = "ProjectProgress", Size = new(200, 28), ShowPercentage = false }; root.AddChild(bar); using var tree = new SceneTree(root);
            Engine.Start(tree); var project = ThemeDB.GetProjectTheme()!; VerifyTheme(project); tree.ProcessFrame(0);
            Check(bar.GetThemeConstant("signed") == -7 && bar.GetThemeStyleBox("fill") is StyleBoxFlat { BGColor: var color } && color == Colors.Red && bar.GetThemeDefaultFontSize() == 20, "Engine startup applies project styles, variation and defaults to attached controls.");
            Check(ReferenceEquals(ThemeDB.GetDefaultTheme().DefaultFont, originalDefault) && ReferenceEquals(ThemeDB.FallbackFont, originalFallback), "Theme-only startup preserves programmatic/default font identities.");
            bar.AddThemeConstantOverride("signed", 3); Check(bar.GetThemeConstant("signed") == 3, "Local override precedes project theme."); bar.RemoveThemeConstantOverride("signed");
            using var branch = new Theme(); branch.SetConstant("signed", "ProjectProgress", 9); bar.Theme = branch; Check(bar.GetThemeConstant("signed") == 9, "Branch theme precedes project theme."); bar.Theme = null;
            project.SetConstant("signed", "ProjectProgress", 11); tree.ProcessFrame(0); Check(bar.GetThemeConstant("signed") == 11, "Project content edits invalidate live owners.");
            var fontPath = System.IO.Path.Combine(folder, "font.e2dres"); ResourceSaver.Save(originalDefault!, fontPath); ProjectSettings.Set(ProjectSettings.GUIThemeCustomFont, fontPath);
            Engine.Stop(); using var fontTree = new SceneTree(new Node()); Engine.Start(fontTree); var projectFont = ThemeDB.FallbackFont;
            Check(!ReferenceEquals(projectFont, originalFallback) && ReferenceEquals(projectFont, ThemeDB.GetDefaultTheme().DefaultFont) && project.IsDisposed, "Successful startup owns a new custom font and retires the prior theme graph.");
            Engine.Stop(); var selected = ThemeDB.GetProjectTheme(); ProjectSettings.Set(ProjectSettings.GUIThemeCustom, System.IO.Path.Combine(folder, "absent.e2dres"));
            try { using var failedTree = new SceneTree(new Node()); Engine.Start(failedTree); throw new InvalidOperationException("Missing theme startup should fail."); } catch (Exception error) when (error is System.IO.FileNotFoundException or System.IO.IOException or AggregateException) { }
            Check(ReferenceEquals(ThemeDB.GetProjectTheme(), selected) && ReferenceEquals(ThemeDB.FallbackFont, projectFont) && Engine.MainLoop == null, "Failed preparation preserves selected resources and closes startup state.");
            ProjectSettings.Set(ProjectSettings.GUIThemeCustom, path); ProjectSettings.Set(ProjectSettings.GUIThemeCustomFont, path);
            try { using var wrongFontTree = new SceneTree(new Node()); Engine.Start(wrongFontTree); throw new InvalidOperationException("Wrong font type should fail."); } catch (InvalidDataException) { }
            Check(ReferenceEquals(ThemeDB.GetProjectTheme(), selected) && !selected!.IsDisposed && ReferenceEquals(ThemeDB.FallbackFont, projectFont), "A valid prepared theme followed by a wrong font type preserves the complete prior selection.");
            ProjectSettings.Set(ProjectSettings.GUIThemeCustom, ""); ProjectSettings.Set(ProjectSettings.GUIThemeCustomFont, ""); using var clearedTree = new SceneTree(new Node()); Engine.Start(clearedTree); Engine.Stop();
            Check(ThemeDB.GetProjectTheme() == null && selected!.IsDisposed && projectFont!.IsDisposed && ReferenceEquals(ThemeDB.GetDefaultTheme().DefaultFont, originalDefault) && ReferenceEquals(ThemeDB.FallbackFont, originalFallback), "Clearing restart settings retires owned graph/font and restores original borrowed font identities.");
            ProjectSettings.Set(ProjectSettings.GUIThemeCustomFont, fontPath); using var customAgain = new SceneTree(new Node()); Engine.Start(customAgain); Engine.Stop(); var replacedFont = ThemeDB.FallbackFont!;
            ThemeDB.GetDefaultTheme().DefaultFont = originalDefault; ThemeDB.FallbackFont = originalFallback;
            ProjectSettings.Set(ProjectSettings.GUIThemeCustomFont, ""); using var manualTree = new SceneTree(new Node()); Engine.Start(manualTree); Engine.Stop();
            Check(replacedFont.IsDisposed && ReferenceEquals(ThemeDB.GetDefaultTheme().DefaultFont, originalDefault) && ReferenceEquals(ThemeDB.FallbackFont, originalFallback), "Clearing the project font preserves manual assignments and still retires its owned font.");
        }
        finally { ProjectSettings.Set(ProjectSettings.GUIThemeCustom, oldPath); ProjectSettings.Set(ProjectSettings.GUIThemeCustomFont, oldFontPath); }
    }
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Electron2D-native-theme-" + Guid.NewGuid().ToString("N")); System.IO.Directory.CreateDirectory(folder);
        var old = ProjectSettings.Get(ProjectSettings.GUIThemeCustom);
        try
        {
            using var theme = CreateTheme(); var path = System.IO.Path.Combine(folder, "project.e2dres"); ResourceSaver.Save(theme, path); ProjectSettings.Set(ProjectSettings.GUIThemeCustom, path);
            var window = new Window { Size = new(96, 64) }; var bar = new ProgressBar { Name = "Project", ThemeTypeVariation = "ProjectProgress", Position = new(8, 8), Size = new(80, 24), Value = 50, ShowPercentage = false }; window.AddChild(bar); using var target = window.GetTexture(); var frames = 0; var passed = false;
            window.Ready += _ => RenderingServer.FramePostDraw += Capture;
            void Capture()
            {
                if (++frames != 4) return; using var pixels = target.GetImage()!;
                Check(pixels.GetPixel(20, 20).IsEqualApprox(Colors.Red) && pixels.GetPixel(75, 20).IsEqualApprox(Colors.Blue), "Configured project theme reaches actual native fill/background.");
                Check(ThemeDB.GetProjectTheme() is not null && bar.Theme == null && bar.GetThemeDefaultFontSize() == 20, "Native startup uses global project lookup without per-widget assignment.");
                passed = true; RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
            }
            Check(Engine.Run(window) == 0 && passed, "Native project theme run completes.");
            Console.WriteLine("Project theme native startup/global variation/styles passed: " + backend);
        }
        finally { ProjectSettings.Set(ProjectSettings.GUIThemeCustom, old); System.IO.Directory.Delete(folder, true); ThemeDB.LoadProjectTheme(); }
    }
}
