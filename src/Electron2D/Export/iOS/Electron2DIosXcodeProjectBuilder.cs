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
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D;

internal static class Electron2DIosXcodeProjectBuilder
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    public static Electron2DIosXcodeProjectBuildResult Build(
        Electron2DIosExportPlan plan,
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
            Directory.CreateDirectory(plan.XcodeProjectDirectory);
            Directory.CreateDirectory(plan.ProjectAssetsDirectory);
            Directory.CreateDirectory(plan.ArtifactsDirectory);
            Directory.CreateDirectory(plan.SmokeDirectory);

            WriteText(plan.IosProjectFilePath, CreateProjectFile(plan, projectSettings));
            files.Add("Electron2D.iOS.csproj");
            WriteText(plan.AppDelegatePath, CreateAppDelegate(plan, projectSettings));
            files.Add("AppDelegate.cs");
            WriteText(plan.InfoPlistPath, CreateInfoPlist(plan, projectSettings));
            files.Add("Info.plist");
            WriteText(plan.EntitlementsPath, CreateEntitlements());
            files.Add("Entitlements.plist");
            WriteText(plan.XcodeProjectFilePath, CreateXcodeProjectFile(plan));
            files.Add("Electron2D.iOS.xcodeproj/project.pbxproj");
            WriteText(plan.ExportMetadataPath, CreateExportMetadata(plan, projectSettings));
            files.Add("ExportMetadata.json");

            CopyProjectSettings(projectRoot, plan, files, diagnostics);
            CopyMainScene(projectRoot, plan, projectSettings.MainScene, files, diagnostics);
            CopyAssets(projectRoot, plan, files, diagnostics);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException or ArgumentException or NotSupportedException)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-IOS-0008",
                "ios-xcode-project",
                $"iOS Xcode project staging could not be written: {exception.Message}"));
        }

        return new Electron2DIosXcodeProjectBuildResult(files, diagnostics);
    }

    private static string CreateProjectFile(Electron2DIosExportPlan plan, Electron2DProjectSettings settings)
    {
        return $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>{{plan.TargetFramework}}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <ApplicationId>{{EscapeXml(plan.BundleIdentifier)}}</ApplicationId>
                <ApplicationTitle>{{EscapeXml(settings.Name)}}</ApplicationTitle>
                <RuntimeIdentifier>{{plan.RuntimeIdentifier}}</RuntimeIdentifier>
                <SelfContained>{{(plan.SelfContained ? "true" : "false")}}</SelfContained>
                <MtouchLink>SdkOnly</MtouchLink>
                <CodesignKey>{{EscapeXml(plan.SigningIdentity)}}</CodesignKey>
                <CodesignEntitlements>Entitlements.plist</CodesignEntitlements>
              </PropertyGroup>

              <ItemGroup>
                <BundleResource Include="Assets\electron2d\**\*" />
              </ItemGroup>
            </Project>
            """;
    }

    private static string CreateAppDelegate(Electron2DIosExportPlan plan, Electron2DProjectSettings settings)
    {
        return $$"""
            using System;
            using Foundation;
            using UIKit;

            [Register("AppDelegate")]
            public sealed class AppDelegate : UIApplicationDelegate
            {
                public override UIWindow? Window { get; set; }

                public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
                {
                    Window = new UIWindow(UIScreen.MainScreen.Bounds);
                    Window.RootViewController = new Electron2DSmokeViewController();
                    Window.MakeKeyAndVisible();
                    WriteSmokeMarker("E2D_SMOKE_LAUNCH_READY");
                    WriteSmokeMarker("E2D_SMOKE_PRECOMPILED_RENDERING_READY");
                    WriteResourceMarker();
                    WriteFilesystemMarker();
                    return true;
                }

                public override void DidEnterBackground(UIApplication application)
                {
                    WriteSmokeMarker("E2D_SMOKE_BACKGROUND_READY");
                }

                public override void WillEnterForeground(UIApplication application)
                {
                    WriteSmokeMarker("E2D_SMOKE_FOREGROUND_READY");
                }

                public override void WillTerminate(UIApplication application)
                {
                    WriteSmokeMarker("E2D_SMOKE_SHUTDOWN_READY");
                }

                private static void WriteResourceMarker()
                {
                    try
                    {
                        var path = NSBundle.MainBundle.PathForResource("Assets/electron2d/project.e2d", "json");
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            WriteSmokeMarker("E2D_SMOKE_RESOURCES_READY");
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                private static void WriteFilesystemMarker()
                {
                    try
                    {
                        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        if (!string.IsNullOrWhiteSpace(documents))
                        {
                            System.IO.File.WriteAllText(System.IO.Path.Combine(documents, "electron2d-smoke.txt"), "ok");
                            WriteSmokeMarker("E2D_SMOKE_FILESYSTEM_READY");
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                internal static void WriteSmokeMarker(string marker)
                {
                    Console.WriteLine(marker);
                }
            }

            internal sealed class Electron2DSmokeViewController : UIViewController
            {
                public override void ViewDidLoad()
                {
                    base.ViewDidLoad();
                    View!.BackgroundColor = UIColor.Black;
                    View.UserInteractionEnabled = true;
                    AppDelegate.WriteSmokeMarker("E2D_SMOKE_RENDER_READY");
                    AppDelegate.WriteSmokeMarker("E2D_SMOKE_AUDIO_READY");
                    AppDelegate.WriteSmokeMarker("E2D_SMOKE_ORIENTATION_READY");
                }

                public override void ViewSafeAreaInsetsDidChange()
                {
                    base.ViewSafeAreaInsetsDidChange();
                    AppDelegate.WriteSmokeMarker("E2D_SMOKE_SAFE_AREA_READY");
                }

                public override void TouchesBegan(NSSet touches, UIEvent? evt)
                {
                    AppDelegate.WriteSmokeMarker("E2D_SMOKE_TOUCH_READY");
                    base.TouchesBegan(touches, evt);
                }
            }
            """;
    }

    private static string CreateInfoPlist(Electron2DIosExportPlan plan, Electron2DProjectSettings settings)
    {
        return $$"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
              <key>CFBundleDevelopmentRegion</key>
              <string>en</string>
              <key>CFBundleExecutable</key>
              <string>{{EscapeXml(plan.ExecutableName)}}</string>
              <key>CFBundleIdentifier</key>
              <string>{{EscapeXml(plan.BundleIdentifier)}}</string>
              <key>CFBundleName</key>
              <string>{{EscapeXml(plan.AppName)}}</string>
              <key>CFBundleDisplayName</key>
              <string>{{EscapeXml(settings.Name)}}</string>
              <key>CFBundlePackageType</key>
              <string>APPL</string>
              <key>CFBundleShortVersionString</key>
              <string>{{EscapeXml(settings.ProjectVersion)}}</string>
              <key>CFBundleVersion</key>
              <string>1</string>
              <key>LSRequiresIPhoneOS</key>
              <true/>
              <key>UIRequiresFullScreen</key>
              <true/>
              <key>UISupportedInterfaceOrientations</key>
              <array>
                <string>UIInterfaceOrientationLandscapeLeft</string>
                <string>UIInterfaceOrientationLandscapeRight</string>
              </array>
              <key>UILaunchStoryboardName</key>
              <string>LaunchScreen</string>
            </dict>
            </plist>
            """;
    }

    private static string CreateEntitlements()
    {
        return """
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
            </dict>
            </plist>
            """;
    }

    private static string CreateXcodeProjectFile(Electron2DIosExportPlan plan)
    {
        return $$"""
            // !$*UTF8*$!
            {
              archiveVersion = 1;
              objectVersion = 56;
              objects = {
                E2D000000000000000000001 = {
                  isa = PBXProject;
                  targets = (E2D000000000000000000002);
                };
                E2D000000000000000000002 = {
                  isa = PBXNativeTarget;
                  name = Electron2D.iOS;
                  productName = {{plan.AppName}};
                  productType = "com.apple.product-type.application";
                };
              };
              rootObject = E2D000000000000000000001;
            }
            """;
    }

    private static string CreateExportMetadata(Electron2DIosExportPlan plan, Electron2DProjectSettings settings)
    {
        var root = new JsonObject
        {
            ["format"] = "Electron2D.IosExportMetadata",
            ["formatVersion"] = 1,
            ["projectName"] = settings.Name,
            ["mainScene"] = NormalizePortablePath(settings.MainScene),
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["targetFramework"] = plan.TargetFramework,
            ["graphicsBackend"] = plan.GraphicsBackend,
            ["rendererProfile"] = plan.RendererProfile.ToString(),
            ["bundleIdentifier"] = plan.BundleIdentifier,
            ["signingRequired"] = plan.SigningRequired,
            ["signingIdentity"] = plan.SigningIdentity,
            ["signingCredentialReference"] = plan.SigningCredentialReference,
            ["mobilePolicies"] = new JsonArray(plan.MobilePolicies.Select(policy => (JsonNode?)policy).ToArray()),
            ["smokeCriteria"] = new JsonArray(plan.SmokeCriteria.Select(criterion => (JsonNode?)criterion).ToArray())
        };
        return root.ToJsonString(IndentedJsonOptions);
    }

    private static void CopyProjectSettings(
        string projectRoot,
        Electron2DIosExportPlan plan,
        List<string> files,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        var source = Path.Combine(projectRoot, "project.e2d.json");
        if (!File.Exists(source))
        {
            diagnostics.Add(Error("E2D-EXPORT-IOS-0009", "ios-xcode-project", "Project settings file project.e2d.json was not found."));
            return;
        }

        CopyFile(source, Path.Combine(plan.ProjectAssetsDirectory, "project.e2d.json"));
        files.Add("Assets/electron2d/project.e2d.json");
    }

    private static void CopyMainScene(
        string projectRoot,
        Electron2DIosExportPlan plan,
        string mainScene,
        List<string> files,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        if (!TryResolveProjectFile(projectRoot, mainScene, out var source))
        {
            diagnostics.Add(Error("E2D-EXPORT-IOS-0010", "ios-xcode-project", $"Main scene path '{mainScene}' must stay inside the project root."));
            return;
        }

        if (!File.Exists(source))
        {
            diagnostics.Add(Error("E2D-EXPORT-IOS-0009", "ios-xcode-project", $"Main scene file '{NormalizePortablePath(mainScene)}' was not found."));
            return;
        }

        var relative = NormalizePortablePath(mainScene);
        CopyFile(source, Path.Combine(plan.ProjectAssetsDirectory, mainScene));
        files.Add("Assets/electron2d/" + relative);
    }

    private static void CopyAssets(
        string projectRoot,
        Electron2DIosExportPlan plan,
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
                diagnostics.Add(Error("E2D-EXPORT-IOS-0010", "ios-xcode-project", "Asset path must stay inside the project assets directory."));
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
