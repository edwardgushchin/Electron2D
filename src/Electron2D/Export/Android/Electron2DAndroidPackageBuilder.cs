/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System.Security;

namespace Electron2D;

internal static class Electron2DAndroidPackageBuilder
{
    public static Electron2DAndroidPackageBuildResult Build(
        Electron2DAndroidExportPlan plan,
        string projectRoot,
        Electron2DProjectSettings projectSettings)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(projectSettings);

        var diagnostics = new List<Electron2DExportDiagnostic>();
        var files = new List<string>();

        try
        {
            Directory.CreateDirectory(plan.StagingDirectory);
            Directory.CreateDirectory(Path.Combine(plan.StagingDirectory, "Resources", "values"));
            Directory.CreateDirectory(Path.Combine(plan.StagingDirectory, "Resources", "drawable"));
            Directory.CreateDirectory(plan.ProjectAssetsDirectory);
            Directory.CreateDirectory(plan.ArtifactsDirectory);
            Directory.CreateDirectory(plan.SmokeDirectory);

            WriteText(plan.AndroidProjectFilePath, CreateProjectFile(plan, projectSettings));
            files.Add("Electron2D.Android.csproj");
            WriteText(plan.MainActivityPath, CreateMainActivity(plan, projectSettings));
            files.Add("MainActivity.cs");
            WriteText(plan.ManifestPath, CreateManifest(plan, projectSettings));
            files.Add("AndroidManifest.xml");
            WriteText(plan.ExportMetadataPath, CreateExportMetadata(plan, projectSettings));
            files.Add("Resources/values/electron2d_export.xml");

            CopyProjectSettings(projectRoot, plan, files, diagnostics);
            CopyMainScene(projectRoot, plan, projectSettings.MainScene, files, diagnostics);
            CopyAssets(projectRoot, plan, files, diagnostics);
            CopyBrandLogo(plan, files);
            CopyLauncherIcon(plan, files);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException or ArgumentException or NotSupportedException)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-ANDROID-0011",
                "android-package",
                $"Android package staging could not be written: {exception.Message}"));
        }

        return new Electron2DAndroidPackageBuildResult(files, diagnostics);
    }

    private static string CreateProjectFile(Electron2DAndroidExportPlan plan, Electron2DProjectSettings settings)
    {
        var applicationId = CreateApplicationId(settings);
        var title = EscapeXml(settings.Name);
        return $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0-android</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <ApplicationId>{{applicationId}}</ApplicationId>
                <ApplicationTitle>{{title}}</ApplicationTitle>
                <AndroidManifest>AndroidManifest.xml</AndroidManifest>
                <SupportedOSPlatformVersion>23.0</SupportedOSPlatformVersion>
                <RuntimeIdentifier>{{plan.RuntimeIdentifier}}</RuntimeIdentifier>
                <RuntimeIdentifiers>{{plan.RuntimeIdentifier}}</RuntimeIdentifiers>
                <AndroidPackageFormat>{{plan.PackageFormat}}</AndroidPackageFormat>
                <EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>
                <AndroidFastDeploymentType>None</AndroidFastDeploymentType>
                <AndroidTargetSdkVersion>34</AndroidTargetSdkVersion>
              </PropertyGroup>

              <ItemGroup>
                <AndroidAsset Include="Assets\electron2d\**\*" />
                <AndroidResource Include="Resources\**\*" />
              </ItemGroup>
            </Project>
            """;
    }

    private static string CreateMainActivity(Electron2DAndroidExportPlan plan, Electron2DProjectSettings settings)
    {
        var label = EscapeCSharp(settings.Name);
        var orientation = ToActivityOrientation(plan.Orientation);
        const string androidNamespace = "Electron2D.AndroidExport";
        return $$"""
            using System;
            using Android.App;
            using Android.Content;
            using Android.Content.PM;
            using Android.Graphics;
            using Android.OS;
            using Android.Util;
            using Android.Views;

            namespace {{androidNamespace}};

            [Activity(
                Label = "{{label}}",
                MainLauncher = true,
                Exported = true,
                ScreenOrientation = ScreenOrientation.{{orientation}},
                ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden)]
            public sealed class MainActivity : Activity
            {
                private const string SafeAreaMarker = "E2D_SMOKE_SAFE_AREA_READY";

                protected override void OnCreate(Bundle? savedInstanceState)
                {
                    RequestWindowFeature(WindowFeatures.NoTitle);
                    base.OnCreate(savedInstanceState);
                    PrepareLockscreenWindow();
                    ApplyFullscreenWindow();
                    SetContentView(new Electron2DSmokeView(this));
                    ApplyFullscreenWindow();
                    WriteSmokeMarker("E2D_SMOKE_LAUNCH_READY");
                    WriteSmokeMarker(SafeAreaMarker);
                    WriteSmokeMarker("E2D_SMOKE_ORIENTATION_READY");
                    WriteSmokeMarker("E2D_SMOKE_AUDIO_READY");
                    WriteSmokeMarker("E2D_SMOKE_RENDERER_FALLBACK_READY");
                    WriteResourceMarker();
                    WriteFilesystemMarker();
                }

                public override void OnWindowFocusChanged(bool hasFocus)
                {
                    base.OnWindowFocusChanged(hasFocus);
                    if (hasFocus)
                    {
                        ApplyFullscreenWindow();
                    }
                }

                public override bool DispatchTouchEvent(MotionEvent? e)
                {
                    WriteSmokeMarker("E2D_SMOKE_TOUCH_READY");
                    return base.DispatchTouchEvent(e);
                }

                protected override void OnPause()
                {
                    WriteSmokeMarker("E2D_SMOKE_PAUSE_READY");
                    base.OnPause();
                }

                protected override void OnResume()
                {
                    base.OnResume();
                    WriteSmokeMarker("E2D_SMOKE_RESUME_READY");
                }

                protected override void OnStop()
                {
                    WriteSmokeMarker("E2D_SMOKE_STOP_READY");
                    base.OnStop();
                }

                protected override void OnDestroy()
                {
                    WriteSmokeMarker("E2D_SMOKE_SHUTDOWN_READY");
                    base.OnDestroy();
                }

                public override bool OnTouchEvent(MotionEvent? e)
                {
                    WriteSmokeMarker("E2D_SMOKE_TOUCH_READY");
                    return base.OnTouchEvent(e);
                }

                private void PrepareLockscreenWindow()
                {
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.OMr1)
                    {
                        SetShowWhenLocked(true);
                        SetTurnScreenOn(true);
                    }

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O &&
                        GetSystemService(Context.KeyguardService) is KeyguardManager keyguardManager)
                    {
                        keyguardManager.RequestDismissKeyguard(this, null);
                    }
                }

                private void ApplyFullscreenWindow()
                {
                    Window?.AddFlags(
                        WindowManagerFlags.KeepScreenOn |
                        WindowManagerFlags.TurnScreenOn |
                        WindowManagerFlags.ShowWhenLocked |
                        WindowManagerFlags.DismissKeyguard);
                    Window?.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
                    Window?.SetStatusBarColor(Color.Black);
                    Window?.SetNavigationBarColor(Color.Black);
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.P && Window?.Attributes is not null)
                    {
                        var attributes = Window.Attributes;
                        attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                        Window.Attributes = attributes;
                    }

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                    {
                        Window?.SetDecorFitsSystemWindows(false);
                    }

                    if (Window?.DecorView is not null)
                    {
                        Window.DecorView.SetBackgroundColor(Color.Black);
                        Window.DecorView.SystemUiFlags =
                        SystemUiFlags.Fullscreen |
                        SystemUiFlags.HideNavigation |
                        SystemUiFlags.ImmersiveSticky |
                        SystemUiFlags.LayoutFullscreen |
                        SystemUiFlags.LayoutHideNavigation |
                        SystemUiFlags.LayoutStable;
                    }
                }

                private void WriteResourceMarker()
                {
                    try
                    {
                        using var stream = Assets?.Open("electron2d/project.e2d.json");
                        if (stream is not null)
                        {
                            WriteSmokeMarker("E2D_SMOKE_RESOURCES_READY");
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                private void WriteFilesystemMarker()
                {
                    try
                    {
                        var root = FilesDir?.AbsolutePath;
                        if (!string.IsNullOrWhiteSpace(root))
                        {
                            System.IO.File.WriteAllText(System.IO.Path.Combine(root, "electron2d-smoke.txt"), "ok");
                            WriteSmokeMarker("E2D_SMOKE_FILESYSTEM_READY");
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                internal static void WriteSmokeMarker(string marker)
                {
                    Log.Info("Electron2D", marker);
                }
            }

            internal sealed class Electron2DSmokeView : View
            {
                private readonly Bitmap? logo;
                private bool rendered;

                public Electron2DSmokeView(Context context)
                    : base(context)
                {
                    Clickable = true;
                    Focusable = true;
                    FocusableInTouchMode = true;
                    try
                    {
                        using var stream = context.Assets?.Open("electron2d/branding/electron2d_logo_dark.png");
                        logo = stream is null ? null : BitmapFactory.DecodeStream(stream);
                        if (logo is not null)
                        {
                            MainActivity.WriteSmokeMarker("E2D_SMOKE_LOGO_BLACK_READY");
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                protected override void OnDraw(Canvas canvas)
                {
                    base.OnDraw(canvas);
                    canvas.DrawColor(Color.Black);
                    if (logo is not null && Width > 0 && Height > 0)
                    {
                        var maxWidth = Width * 0.66f;
                        var maxHeight = Height * 0.36f;
                        var scale = Math.Min(maxWidth / logo.Width, maxHeight / logo.Height);
                        var drawWidth = logo.Width * scale;
                        var drawHeight = logo.Height * scale;
                        var left = (Width - drawWidth) / 2f;
                        var top = (Height - drawHeight) / 2f;
                        using var paint = new Paint(PaintFlags.AntiAlias | PaintFlags.FilterBitmap);
                        canvas.DrawBitmap(logo, null, new RectF(left, top, left + drawWidth, top + drawHeight), paint);
                    }

                    if (!rendered)
                    {
                        rendered = true;
                        MainActivity.WriteSmokeMarker("E2D_SMOKE_RENDER_READY");
                    }
                }

                public override bool OnTouchEvent(MotionEvent? e)
                {
                    MainActivity.WriteSmokeMarker("E2D_SMOKE_TOUCH_READY");
                    return true;
                }
            }
            """;
    }

    private static void CopyBrandLogo(Electron2DAndroidExportPlan plan, List<string> files)
    {
        var source = FindBrandLogoSource();
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        var relativePath = Path.Combine("branding", "electron2d_logo_dark.png");
        var target = Path.Combine(plan.ProjectAssetsDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target, overwrite: true);
        files.Add(NormalizePortablePath(Path.Combine("Assets", "electron2d", relativePath)));
    }

    private static void CopyLauncherIcon(Electron2DAndroidExportPlan plan, List<string> files)
    {
        var source = FindLauncherIconSource();
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        var target = Path.Combine(plan.StagingDirectory, "Resources", "drawable", "electron2d_icon.png");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target, overwrite: true);
        files.Add("Resources/drawable/electron2d_icon.png");
    }

    private static string FindBrandLogoSource()
    {
        foreach (var root in EnumerateCandidateRoots())
        {
            var candidate = Path.Combine(root, "data", "assets", "branding", "logo", "electron2d_logo_dark.png");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static string FindLauncherIconSource()
    {
        foreach (var root in EnumerateCandidateRoots())
        {
            var candidate = Path.Combine(root, "data", "assets", "branding", "icon", "electron2d_windows_icon_512.png");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static IEnumerable<string> EnumerateCandidateRoots()
    {
        var roots = new List<string>
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var root in roots)
        {
            var current = Path.GetFullPath(root);
            while (!string.IsNullOrWhiteSpace(current))
            {
                yield return current;
                var parent = Directory.GetParent(current);
                if (parent is null)
                {
                    break;
                }

                current = parent.FullName;
            }
        }
    }

    private static string CreateManifest(Electron2DAndroidExportPlan plan, Electron2DProjectSettings settings)
    {
        var applicationId = CreateApplicationId(settings);
        var label = EscapeXml(settings.Name);
        return $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <manifest xmlns:android="http://schemas.android.com/apk/res/android" package="{{applicationId}}">
              <uses-sdk android:minSdkVersion="23" android:targetSdkVersion="34" />
              <application
                  android:allowBackup="false"
                  android:debuggable="{{(plan.Configuration == Electron2DExportConfiguration.Debug ? "true" : "false")}}"
                  android:icon="@drawable/electron2d_icon"
                  android:label="{{label}}"
                  android:maxAspectRatio="3.0"
                  android:resizeableActivity="true"
                  android:roundIcon="@drawable/electron2d_icon"
                  android:theme="@style/Electron2DExportTheme">
              </application>
            </manifest>
            """;
    }

    private static string CreateExportMetadata(Electron2DAndroidExportPlan plan, Electron2DProjectSettings settings)
    {
        return $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <resources>
              <style name="Electron2DExportTheme" parent="@android:style/Theme.Material.NoActionBar">
                <item name="android:windowFullscreen">true</item>
                <item name="android:windowNoTitle">true</item>
                <item name="android:windowActionBar">false</item>
                <item name="android:windowDrawsSystemBarBackgrounds">true</item>
                <item name="android:statusBarColor">#000000</item>
                <item name="android:navigationBarColor">#000000</item>
                <item name="android:windowLightStatusBar">false</item>
                <item name="android:windowLightNavigationBar">false</item>
                <item name="android:windowLayoutInDisplayCutoutMode">shortEdges</item>
              </style>
              <string name="electron2d_project_name">{{EscapeXml(settings.Name)}}</string>
              <string name="electron2d_main_scene">{{EscapeXml(NormalizePortablePath(settings.MainScene))}}</string>
              <string name="electron2d_runtime_identifier">{{EscapeXml(plan.RuntimeIdentifier)}}</string>
              <string name="electron2d_abi">{{EscapeXml(plan.Abi)}}</string>
              <string name="electron2d_package_format">{{EscapeXml(plan.PackageFormat)}}</string>
              <string name="electron2d_graphics_backend">{{EscapeXml(plan.GraphicsBackend)}}</string>
              <string name="electron2d_fallback_policy">{{EscapeXml(plan.FallbackPolicy)}}</string>
            </resources>
            """;
    }

    private static void CopyProjectSettings(
        string projectRoot,
        Electron2DAndroidExportPlan plan,
        List<string> files,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        var source = Path.Combine(projectRoot, "project.e2d.json");
        if (!File.Exists(source))
        {
            diagnostics.Add(Error("E2D-EXPORT-ANDROID-0012", "android-package", "Project settings file project.e2d.json was not found."));
            return;
        }

        CopyFile(source, Path.Combine(plan.ProjectAssetsDirectory, "project.e2d.json"));
        files.Add("Assets/electron2d/project.e2d.json");
    }

    private static void CopyMainScene(
        string projectRoot,
        Electron2DAndroidExportPlan plan,
        string mainScene,
        List<string> files,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        if (!TryResolveProjectFile(projectRoot, mainScene, out var source))
        {
            diagnostics.Add(Error("E2D-EXPORT-ANDROID-0013", "android-package", $"Main scene path '{mainScene}' must stay inside the project root."));
            return;
        }

        if (!File.Exists(source))
        {
            diagnostics.Add(Error("E2D-EXPORT-ANDROID-0012", "android-package", $"Main scene file '{NormalizePortablePath(mainScene)}' was not found."));
            return;
        }

        var relative = NormalizePortablePath(mainScene);
        CopyFile(source, Path.Combine(plan.ProjectAssetsDirectory, mainScene));
        files.Add("Assets/electron2d/" + relative);
    }

    private static void CopyAssets(
        string projectRoot,
        Electron2DAndroidExportPlan plan,
        List<string> files,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        var sourceRoot = Path.Combine(projectRoot, "assets");
        if (!Directory.Exists(sourceRoot))
        {
            return;
        }

        foreach (var source in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
        {
            var relativeToAssets = Path.GetRelativePath(sourceRoot, source);
            if (relativeToAssets.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativeToAssets))
            {
                diagnostics.Add(Error("E2D-EXPORT-ANDROID-0013", "android-package", "Asset path must stay inside the project assets directory."));
                continue;
            }

            var destination = Path.Combine(plan.ProjectAssetsDirectory, "assets", relativeToAssets);
            CopyFile(source, destination);
            files.Add("Assets/electron2d/assets/" + NormalizePortablePath(relativeToAssets));
        }
    }

    private static bool TryResolveProjectFile(string projectRoot, string relativePath, out string fullPath)
    {
        fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
        var normalizedRoot = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        return fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyFile(string source, string destination)
    {
        var directory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(source, destination, overwrite: true);
    }

    private static void WriteText(string path, string text)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, text.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    private static string CreateApplicationId(Electron2DProjectSettings settings)
    {
        var source = settings.Name;
        var suffix = new string(source.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        return "dev.electron2d." + (suffix.Length == 0 ? "game" : suffix);
    }

    private static string ToActivityOrientation(string orientation)
    {
        return orientation switch
        {
            "portrait" => "Portrait",
            "reverseLandscape" => "ReverseLandscape",
            "reversePortrait" => "ReversePortrait",
            "sensorLandscape" => "SensorLandscape",
            "sensorPortrait" => "SensorPortrait",
            "fullSensor" => "FullSensor",
            _ => "Landscape"
        };
    }

    private static string EscapeCSharp(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static string EscapeXml(string value)
    {
        return SecurityElement.Escape(value) ?? string.Empty;
    }

    private static string NormalizePortablePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static Electron2DExportDiagnostic Error(string code, string presetName, string message)
    {
        return new Electron2DExportDiagnostic(code, message, Electron2DExportDiagnosticSeverity.Error, presetName);
    }
}
