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
using Electron2D.UiHeavyReference.Scripts;

const string CurrentSceneEnvironmentVariable = "ELECTRON2D_CURRENT_SCENE";
const string SavePathEnvironmentVariable = "ELECTRON2D_UI_HEAVY_REFERENCE_SAVE";

var projectRoot = ResolveProjectRoot();
var projectFile = Path.Combine(projectRoot, "project.e2d.json");
if (!File.Exists(projectFile))
{
    Console.Error.WriteLine("Electron2D UI-heavy reference manifest was not found.");
    return 1;
}

using var document = JsonDocument.Parse(File.ReadAllText(projectFile));
if (!document.RootElement.TryGetProperty("mainScene", out var mainSceneElement))
{
    Console.Error.WriteLine("Electron2D UI-heavy reference manifest does not define mainScene.");
    return 1;
}

var mainScene = mainSceneElement.GetString();
if (string.IsNullOrWhiteSpace(mainScene))
{
    Console.Error.WriteLine("Electron2D UI-heavy reference manifest mainScene is empty.");
    return 1;
}

var requestedScene = Environment.GetEnvironmentVariable(CurrentSceneEnvironmentVariable);
var sceneToLoad = string.IsNullOrWhiteSpace(requestedScene)
    ? mainScene
    : NormalizeScenePath(requestedScene);
if (string.IsNullOrWhiteSpace(sceneToLoad))
{
    return 1;
}

var scenePath = Path.Combine(projectRoot, sceneToLoad.Replace('/', Path.DirectorySeparatorChar));
if (!File.Exists(scenePath))
{
    Console.Error.WriteLine($"Electron2D UI-heavy reference scene was not found: {sceneToLoad}");
    return 1;
}

ConfigureInputMap();

var tree = new SceneTree();
var game = new CardPuzzleGame
{
    Name = "UiHeavyReference",
    ProjectRoot = projectRoot
};
tree.Root.AddChild(game);

var savePath = Environment.GetEnvironmentVariable(SavePathEnvironmentVariable);
if (string.IsNullOrWhiteSpace(savePath))
{
    savePath = Path.Combine(projectRoot, ".electron2d", "user", "ui-heavy-progress.json");
}

var result = game.RunHeadlessVerification(savePath);
Console.WriteLine($"UI-heavy reference scene loaded: {sceneToLoad}");
Console.WriteLine($"UI-heavy reference subsystems: {result.ToSubsystemSummary()}");
Console.WriteLine($"UI-heavy reference progress: scene={result.Scene},locale={result.Locale},score={result.Score},moves={result.Moves},save={result.SavePath}");

return result.AllPassed ? 0 : 1;

static void ConfigureInputMap()
{
    AddAction("accept", 0.2f);
    InputMap.ActionAddEvent("accept", new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter });
    InputMap.ActionAddEvent("accept", new InputEventKey { Keycode = Key.Space, PhysicalKeycode = Key.Space });
    InputMap.ActionAddEvent("accept", new InputEventJoypadButton { ButtonIndex = JoyButton.A });

    AddAction("cancel", 0.2f);
    InputMap.ActionAddEvent("cancel", new InputEventKey { Keycode = Key.Escape, PhysicalKeycode = Key.Escape });
    InputMap.ActionAddEvent("cancel", new InputEventJoypadButton { ButtonIndex = JoyButton.B });

    AddAction("next_card", 0.2f);
    InputMap.ActionAddEvent("next_card", new InputEventKey { Keycode = Key.Right, PhysicalKeycode = Key.Right });
    InputMap.ActionAddEvent("next_card", new InputEventJoypadButton { ButtonIndex = JoyButton.DpadRight });

    AddAction("previous_card", 0.2f);
    InputMap.ActionAddEvent("previous_card", new InputEventKey { Keycode = Key.Left, PhysicalKeycode = Key.Left });
    InputMap.ActionAddEvent("previous_card", new InputEventJoypadButton { ButtonIndex = JoyButton.DpadLeft });

    AddAction("switch_locale", 0.2f);
    InputMap.ActionAddEvent("switch_locale", new InputEventKey { Keycode = Key.L, PhysicalKeycode = Key.L });
    InputMap.ActionAddEvent("switch_locale", new InputEventJoypadButton { ButtonIndex = JoyButton.Y });
}

static void AddAction(string action, float deadzone)
{
    if (InputMap.HasAction(action))
    {
        InputMap.ActionEraseEvents(action);
        InputMap.ActionSetDeadzone(action, deadzone);
        return;
    }

    InputMap.AddAction(action, deadzone);
}

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
        return string.Empty;
    }

    return normalized;
}
