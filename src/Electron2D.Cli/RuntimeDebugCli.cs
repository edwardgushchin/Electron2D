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
using System.Globalization;
using System.Text.Json.Nodes;
using Electron2D.ProjectSystem;

internal static partial class Electron2DCommandLine
{
    private static bool IsRuntimeDebugCommand(CliOptions options)
    {
        return options.Values.Count > 0 &&
            string.Equals(options.Values[0], "debug", StringComparison.OrdinalIgnoreCase);
    }

    private static int RunRuntimeDebug(CliOptions options, TextWriter output, TextWriter error)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var scenePath = options.RequireOption("--scene", "run debug requires --scene <project-relative-scene>.");
        var sessionKind = ParseRuntimeDebugSessionKind(options.GetOption("--session-kind") ?? "headless", options);
        var start = RuntimeDebugBridge.Start(new RuntimeDebugStartRequest(
            projectRoot,
            scenePath,
            sessionKind,
            developmentMode: true,
            options.GetOption("--input-build-configuration-hash") ?? "sha256:debug"));
        if (!start.Succeeded)
        {
            return WriteResult(
                CliResult.Blocked(
                    "run debug",
                    options,
                    "Runtime debug bridge could not start.",
                    start.Diagnostics.First()),
                output,
                error);
        }

        var session = start.Session!;
        var fixedDelta = ParseOptionalPositiveDouble(options, "--fixed-delta", defaultValue: 1.0 / 60.0);
        var physicsDelta = ParseOptionalPositiveDouble(options, "--physics-delta", defaultValue: fixedDelta);
        var stepFrames = ParseOptionalPositiveInt32(options, "--step-frames", defaultValue: 0);
        var stepPhysics = ParseOptionalPositiveInt32(options, "--step-physics", defaultValue: 0);

        if (stepFrames > 0 || stepPhysics > 0)
        {
            session.Pause();
        }

        if (stepFrames > 0)
        {
            session.StepFrame(stepFrames, fixedDelta);
        }

        if (stepPhysics > 0)
        {
            session.StepPhysics(stepPhysics, physicsDelta);
        }

        if (options.GetOption("--input-action") is { } inputAction)
        {
            ApplyRuntimeDebugInput(session, inputAction, options);
        }

        RuntimeDebugCommandResult? inspected = null;
        if (options.GetOption("--inspect-node") is { } nodePath)
        {
            inspected = session.InspectNode(nodePath);
            if (!inspected.Succeeded)
            {
                return WriteResult(
                    CliResult.Blocked(
                        "run debug",
                        options,
                        "Runtime node inspection failed.",
                        inspected.Diagnostics.First()),
                    output,
                    error);
            }
        }

        JsonObject? screenshotJson = null;
        if (options.GetOption("--screenshot") is { } screenshotPath)
        {
            var screenshot = session.CaptureScreenshot();
            var fullPath = ResolveRuntimeDebugOutputPath(projectRoot, screenshotPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllBytes(fullPath, screenshot.Bytes);
            var outputPath = Path.IsPathRooted(screenshotPath)
                ? fullPath
                : ProjectDocumentPaths.NormalizeRelativePath(screenshotPath);
            screenshotJson = RuntimeDebugJsonSerializer.ToJson(screenshot, outputPath);
        }

        var result = CliResult.Success(
            "run debug",
            options,
            projectRoot,
            CliRoute.Headless,
            "Runtime debug bridge completed.",
            changedFiles: screenshotJson is null ? [] : [screenshotJson["path"]!.GetValue<string>()],
            dirtyDocuments: [],
            operation: null,
            job: null,
            data: new JsonObject
            {
                ["mode"] = "runtime.debugBridge",
                ["session"] = RuntimeDebugJsonSerializer.ToJson(session),
                ["sceneTree"] = RuntimeDebugJsonSerializer.ToJson(session.GetSceneTree()),
                ["inspectedNode"] = inspected?.Node is null ? null : RuntimeDebugJsonSerializer.ToJson(inspected.Node),
                ["metrics"] = RuntimeDebugJsonSerializer.ToJson(session.GetMetrics()),
                ["diagnostics"] = WriteDiagnostics(session.Diagnostics),
                ["screenshot"] = screenshotJson
            });

        return WriteResult(result, output, error);
    }

    private static RuntimeDebugSessionKind ParseRuntimeDebugSessionKind(string value, CliOptions options)
    {
        return value.ToLowerInvariant() switch
        {
            "headless" => RuntimeDebugSessionKind.HeadlessPreview,
            "editor" => RuntimeDebugSessionKind.EditorAttachedPreview,
            _ => throw new CliCommandException(
                "run debug",
                options,
                "--session-kind must be headless or editor.",
                CreateCliDiagnostic("E2D-CLI-0002", "--session-kind must be headless or editor."))
        };
    }

    private static void ApplyRuntimeDebugInput(RuntimeDebugSession session, string value, CliOptions options)
    {
        var separator = value.IndexOf('=');
        if (separator <= 0 || separator == value.Length - 1)
        {
            throw new CliCommandException(
                "run debug",
                options,
                "--input-action must use action=pressed or action=released.",
                CreateCliDiagnostic("E2D-CLI-0002", "--input-action must use action=pressed or action=released."));
        }

        var action = value[..separator];
        var state = value[(separator + 1)..].ToLowerInvariant();
        var pressed = state switch
        {
            "pressed" => true,
            "released" => false,
            _ => throw new CliCommandException(
                "run debug",
                options,
                "--input-action state must be pressed or released.",
                CreateCliDiagnostic("E2D-CLI-0002", "--input-action state must be pressed or released."))
        };
        session.InjectInput(action, pressed);
    }

    private static int ParseOptionalPositiveInt32(CliOptions options, string name, int defaultValue)
    {
        if (options.GetOption(name) is not { } value)
        {
            return defaultValue;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
        {
            throw new CliCommandException(
                "run debug",
                options,
                $"{name} must be a non-negative integer.",
                CreateCliDiagnostic("E2D-CLI-0002", $"{name} must be a non-negative integer."));
        }

        return parsed;
    }

    private static double ParseOptionalPositiveDouble(CliOptions options, string name, double defaultValue)
    {
        if (options.GetOption(name) is not { } value)
        {
            return defaultValue;
        }

        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ||
            double.IsNaN(parsed) ||
            parsed <= 0)
        {
            throw new CliCommandException(
                "run debug",
                options,
                $"{name} must be a positive number.",
                CreateCliDiagnostic("E2D-CLI-0002", $"{name} must be a positive number."));
        }

        return parsed;
    }

    private static string ResolveRuntimeDebugOutputPath(string projectRoot, string outputPath)
    {
        var root = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullPath = Path.IsPathRooted(outputPath)
            ? Path.GetFullPath(outputPath)
            : Path.GetFullPath(Path.Combine(root, outputPath));
        if (!Path.IsPathRooted(outputPath))
        {
            WorkspaceSnapshotMaterializer.EnsureChildPath(root, fullPath);
        }

        return fullPath;
    }
}
