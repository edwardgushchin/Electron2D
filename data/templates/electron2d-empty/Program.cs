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
using System.Text.Json;

using Electron2D;
using Electron2D.Empty.Scripts;

const string CurrentSceneEnvironmentVariable = "ELECTRON2D_CURRENT_SCENE";

var projectRoot = ResolveProjectRoot();
var projectFile = Path.Combine(projectRoot, "project.e2d.json");
if (!File.Exists(projectFile))
{
    Console.Error.WriteLine("Electron2D project manifest was not found.");
    return 1;
}

using var document = JsonDocument.Parse(File.ReadAllText(projectFile));
if (!document.RootElement.TryGetProperty("mainScene", out var mainSceneElement))
{
    Console.Error.WriteLine("Electron2D project manifest does not define mainScene.");
    return 1;
}

var mainScene = mainSceneElement.GetString();
if (string.IsNullOrWhiteSpace(mainScene))
{
    Console.Error.WriteLine("Electron2D project manifest mainScene is empty.");
    return 1;
}

var requestedScene = Environment.GetEnvironmentVariable(CurrentSceneEnvironmentVariable);
var sceneToLoad = string.IsNullOrWhiteSpace(requestedScene)
    ? mainScene
    : NormalizeScenePath(requestedScene);

var scenePath = Path.Combine(
    projectRoot,
    sceneToLoad.Replace('/', Path.DirectorySeparatorChar));

if (!File.Exists(scenePath))
{
    Console.Error.WriteLine($"Electron2D scene was not found: {sceneToLoad}");
    return 1;
}

Console.WriteLine($"Electron2D empty scene loaded: {sceneToLoad}");

var tree = new SceneTree();
var script = new MainScene { Name = "MainScene" };
tree.Root.AddChild(script);

Console.WriteLine($"Electron2D C# script lifecycle: {script.LifecycleSummary}");
Console.WriteLine(
    $"Electron2D C# script services: tree={script.TreeWasAvailable},text={script.TextFeatureWasAvailable}");

return script.IsReady ? 0 : 1;

static string ResolveProjectRoot()
{
    var currentDirectoryProjectFile = Path.Combine(Environment.CurrentDirectory, "project.e2d.json");
    if (File.Exists(currentDirectoryProjectFile))
    {
        return Environment.CurrentDirectory;
    }

    var outputDirectoryProjectFile = Path.Combine(AppContext.BaseDirectory, "project.e2d.json");
    if (File.Exists(outputDirectoryProjectFile))
    {
        return AppContext.BaseDirectory;
    }

    return Environment.CurrentDirectory;
}

static string NormalizeScenePath(string value)
{
    var normalized = value.Replace('\\', '/').Trim();
    if (Path.IsPathRooted(normalized) ||
        normalized.Length == 0 ||
        normalized.Split('/').Any(part => part is "" or "." or ".."))
    {
        Console.Error.WriteLine("Electron2D current scene override must be a relative project path.");
        Environment.ExitCode = 1;
        return string.Empty;
    }

    return normalized;
}
