using System.Text.Json;

var projectFile = Path.Combine(AppContext.BaseDirectory, "project.e2d.json");
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

var scenePath = Path.Combine(
    AppContext.BaseDirectory,
    mainScene.Replace('/', Path.DirectorySeparatorChar));

if (!File.Exists(scenePath))
{
    Console.Error.WriteLine($"Electron2D main scene was not found: {mainScene}");
    return 1;
}

Console.WriteLine($"Electron2D empty scene loaded: {mainScene}");
return 0;
