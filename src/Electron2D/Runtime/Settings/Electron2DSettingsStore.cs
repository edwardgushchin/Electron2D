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
using System.Text.Json.Nodes;

namespace Electron2D;

internal static class Electron2DSettingsStore
{
    private const string ProjectFormat = "Electron2D.ProjectSettings";
    private const string UserFormat = "Electron2D.UserSettings";
    private const int FormatVersion = 1;
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static void SaveProject(string path, Electron2DProjectSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        WriteText(path, SerializeProject(settings));
    }

    public static Electron2DSettingsLoadResult<Electron2DProjectSettings> LoadProject(string path)
    {
        return Load(path, DeserializeProject);
    }

    internal static Electron2DSettingsLoadResult<Electron2DProjectSettings> LoadProjectFromText(string text, string sourcePath)
    {
        return LoadText(text, sourcePath, DeserializeProject);
    }

    public static Electron2DSettingsLoadResult<Electron2DProjectSettings> LoadProjectAndApply(string path)
    {
        var result = LoadProject(path);
        result.Settings?.ApplyToRuntime();
        return result;
    }

    public static void SaveUser(string path, Electron2DUserSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        WriteText(path, SerializeUser(settings));
    }

    public static Electron2DSettingsLoadResult<Electron2DUserSettings> LoadUser(string path)
    {
        return Load(path, DeserializeUser);
    }

    public static Electron2DSettingsLoadResult<Electron2DUserSettings> LoadUserAndApply(string path)
    {
        var result = LoadUser(path);
        result.Settings?.ApplyToRuntime();
        return result;
    }

    private static Electron2DSettingsLoadResult<TSettings> Load<TSettings>(string path, Func<string, TSettings> deserialize)
        where TSettings : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(deserialize);

        try
        {
            return LoadText(File.ReadAllText(path), path, deserialize);
        }
        catch (IOException exception)
        {
            return Electron2DSettingsLoadResult<TSettings>.Failure(
                new Electron2DSettingsDiagnostic("settings.io_error", exception.Message, path));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Electron2DSettingsLoadResult<TSettings>.Failure(
                new Electron2DSettingsDiagnostic("settings.io_error", exception.Message, path));
        }
    }

    private static Electron2DSettingsLoadResult<TSettings> LoadText<TSettings>(
        string text,
        string sourcePath,
        Func<string, TSettings> deserialize)
        where TSettings : class
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(deserialize);

        try
        {
            return Electron2DSettingsLoadResult<TSettings>.Success(deserialize(text));
        }
        catch (JsonException exception)
        {
            return Electron2DSettingsLoadResult<TSettings>.Failure(
                new Electron2DSettingsDiagnostic("settings.malformed_json", exception.Message, sourcePath));
        }
        catch (FormatException exception)
        {
            return Electron2DSettingsLoadResult<TSettings>.Failure(
                new Electron2DSettingsDiagnostic("settings.invalid_value", exception.Message, sourcePath));
        }
        catch (InvalidOperationException exception)
        {
            return Electron2DSettingsLoadResult<TSettings>.Failure(
                new Electron2DSettingsDiagnostic("settings.invalid_value", exception.Message, sourcePath));
        }
        catch (ArgumentException exception)
        {
            return Electron2DSettingsLoadResult<TSettings>.Failure(
                new Electron2DSettingsDiagnostic("settings.invalid_value", exception.Message, sourcePath));
        }
    }

    private static string SerializeProject(Electron2DProjectSettings settings)
    {
        var root = new JsonObject
        {
            ["format"] = ProjectFormat,
            ["formatVersion"] = FormatVersion,
            ["name"] = settings.Name,
            ["version"] = settings.ProjectVersion,
            ["engineVersion"] = settings.EngineVersion,
            ["mainScene"] = settings.MainScene,
            ["rendererProfile"] = settings.RendererProfile.ToString(),
            ["physicsTicksPerSecond"] = settings.PhysicsTicksPerSecond,
            ["input"] = WriteInput(settings.InputActions),
            ["display"] = WriteDisplay(settings.Display)
        };

        return root.ToJsonString(IndentedOptions);
    }

    private static Electron2DProjectSettings DeserializeProject(string text)
    {
        var root = ExpectObject(JsonNode.Parse(text), "Project settings");
        ValidateFormat(root, ProjectFormat, "Project settings");

        var settings = new Electron2DProjectSettings
        {
            Name = ReadString(root, "name", "Project name"),
            ProjectVersion = ReadString(root, "version", "Project version"),
            EngineVersion = ReadString(root, "engineVersion", "Engine version"),
            MainScene = ReadString(root, "mainScene", "Project main scene"),
            RendererProfile = ReadEnum<Electron2DRendererProfileSetting>(root, "rendererProfile", "Renderer profile"),
            PhysicsTicksPerSecond = ReadInt32(root, "physicsTicksPerSecond", "Physics ticks per second"),
            InputActions = ReadInput(ReadObject(root, "input", "Input settings")),
            Display = ReadDisplay(ReadObject(root, "display", "Display settings"))
        };

        settings.Validate();
        return settings;
    }

    private static string SerializeUser(Electron2DUserSettings settings)
    {
        var normalized = NormalizeUserSettings(settings);
        var root = new JsonObject
        {
            ["format"] = UserFormat,
            ["formatVersion"] = FormatVersion,
            ["locale"] = normalized.Locale,
            ["lastProjectPath"] = normalized.LastProjectPath,
            ["recentProjects"] = new JsonArray(normalized.RecentProjects.Select(project => (JsonNode)project).ToArray()),
            ["window"] = WriteUserWindow(normalized.Window)
        };

        return root.ToJsonString(IndentedOptions);
    }

    private static Electron2DUserSettings DeserializeUser(string text)
    {
        var root = ExpectObject(JsonNode.Parse(text), "User settings");
        ValidateFormat(root, UserFormat, "User settings");

        var settings = new Electron2DUserSettings
        {
            Locale = ReadString(root, "locale", "User locale"),
            LastProjectPath = ReadOptionalString(root, "lastProjectPath", "User last project path"),
            RecentProjects = ReadStringArray(root, "recentProjects", "User recent projects"),
            Window = ReadUserWindow(ReadObject(root, "window", "User window settings"))
        };

        settings = NormalizeUserSettings(settings);
        settings.Validate();
        return settings;
    }

    private static Electron2DUserSettings NormalizeUserSettings(Electron2DUserSettings settings)
    {
        var recentProjects = new List<string>();
        foreach (var project in settings.RecentProjects)
        {
            if (string.IsNullOrWhiteSpace(project) || recentProjects.Contains(project, StringComparer.Ordinal))
            {
                continue;
            }

            recentProjects.Add(project);
        }

        return new Electron2DUserSettings
        {
            Locale = TranslationServer.NormalizeLocaleName(settings.Locale),
            LastProjectPath = settings.LastProjectPath ?? string.Empty,
            RecentProjects = recentProjects.ToArray(),
            Window = settings.Window
        };
    }

    private static JsonObject WriteInput(InputMapActionSnapshot[] actions)
    {
        var inputMapRoot = ExpectObject(JsonNode.Parse(InputMapProjectSettings.Serialize(actions)), "Input map settings");
        return new JsonObject
        {
            ["actions"] = CloneNode(inputMapRoot["actions"])
        };
    }

    private static InputMapActionSnapshot[] ReadInput(JsonObject input)
    {
        var wrapper = new JsonObject
        {
            ["format"] = 1,
            ["actions"] = CloneNode(ReadRequiredProperty(input, "actions", "Input actions"))
        };

        return InputMapProjectSettings.Deserialize(wrapper.ToJsonString());
    }

    private static JsonObject WriteDisplay(Electron2DDisplaySettings display)
    {
        display.Validate();
        return new JsonObject
        {
            ["windowWidth"] = display.WindowSize.X,
            ["windowHeight"] = display.WindowSize.Y,
            ["fullscreen"] = display.Fullscreen,
            ["dpiScale"] = display.DpiScale,
            ["stretchMode"] = display.StretchMode.ToString(),
            ["stretchAspect"] = display.StretchAspect.ToString(),
            ["stretchScaleMode"] = display.StretchScaleMode.ToString(),
            ["stretchScale"] = display.StretchScale,
            ["orientation"] = display.Orientation.ToString(),
            ["safeArea"] = new JsonObject
            {
                ["x"] = display.SafeArea.Position.X,
                ["y"] = display.SafeArea.Position.Y,
                ["width"] = display.SafeArea.Size.X,
                ["height"] = display.SafeArea.Size.Y
            }
        };
    }

    private static Electron2DDisplaySettings ReadDisplay(JsonObject display)
    {
        return new Electron2DDisplaySettings
        {
            WindowSize = new Vector2I(
                ReadInt32(display, "windowWidth", "Display window width"),
                ReadInt32(display, "windowHeight", "Display window height")),
            Fullscreen = ReadBoolean(display, "fullscreen", "Display fullscreen"),
            DpiScale = ReadSingle(display, "dpiScale", "Display DPI scale"),
            StretchMode = ReadEnum<ViewportStretchMode>(display, "stretchMode", "Display stretch mode"),
            StretchAspect = ReadEnum<ViewportStretchAspect>(display, "stretchAspect", "Display stretch aspect"),
            StretchScaleMode = ReadEnum<ViewportStretchScaleMode>(display, "stretchScaleMode", "Display stretch scale mode"),
            StretchScale = ReadSingle(display, "stretchScale", "Display stretch scale"),
            Orientation = ReadEnum<DisplayServer.ScreenOrientation>(display, "orientation", "Display orientation"),
            SafeArea = ReadSafeArea(ReadObject(display, "safeArea", "Display safe area"))
        };
    }

    private static JsonObject WriteUserWindow(Electron2DUserWindowSettings window)
    {
        window.Validate();
        return new JsonObject
        {
            ["x"] = window.Position.X,
            ["y"] = window.Position.Y,
            ["width"] = window.Size.X,
            ["height"] = window.Size.Y,
            ["maximized"] = window.Maximized
        };
    }

    private static Electron2DUserWindowSettings ReadUserWindow(JsonObject window)
    {
        return new Electron2DUserWindowSettings
        {
            Position = new Vector2I(
                ReadInt32(window, "x", "User window x"),
                ReadInt32(window, "y", "User window y")),
            Size = new Vector2I(
                ReadInt32(window, "width", "User window width"),
                ReadInt32(window, "height", "User window height")),
            Maximized = ReadBoolean(window, "maximized", "User window maximized")
        };
    }

    private static Rect2I ReadSafeArea(JsonObject safeArea)
    {
        return new Rect2I(
            ReadInt32(safeArea, "x", "Display safe area x"),
            ReadInt32(safeArea, "y", "Display safe area y"),
            ReadInt32(safeArea, "width", "Display safe area width"),
            ReadInt32(safeArea, "height", "Display safe area height"));
    }

    private static void ValidateFormat(JsonObject root, string expectedFormat, string description)
    {
        var format = ReadString(root, "format", $"{description} format");
        if (format != expectedFormat)
        {
            throw new FormatException($"{description} format '{format}' is not supported.");
        }

        var version = ReadInt32(root, "formatVersion", $"{description} format version");
        if (version != FormatVersion)
        {
            throw new FormatException($"{description} version '{version}' is not supported.");
        }
    }

    private static JsonObject ReadObject(JsonObject obj, string propertyName, string description)
    {
        return ExpectObject(ReadRequiredProperty(obj, propertyName, description), description);
    }

    private static JsonNode ReadRequiredProperty(JsonObject obj, string propertyName, string description)
    {
        return obj.TryGetPropertyValue(propertyName, out var node) && node is not null
            ? node
            : throw new FormatException($"{description} is missing required property '{propertyName}'.");
    }

    private static string ReadString(JsonObject obj, string propertyName, string description)
    {
        var node = ReadRequiredProperty(obj, propertyName, description);
        if (node is not JsonValue value || !value.TryGetValue<string>(out var result) || string.IsNullOrWhiteSpace(result))
        {
            throw new FormatException($"{description} must be a non-empty JSON string.");
        }

        return result;
    }

    private static string ReadOptionalString(JsonObject obj, string propertyName, string description)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return string.Empty;
        }

        if (node is not JsonValue value || !value.TryGetValue<string>(out var result))
        {
            throw new FormatException($"{description} must be a JSON string.");
        }

        return result ?? string.Empty;
    }

    private static string[] ReadStringArray(JsonObject obj, string propertyName, string description)
    {
        var node = ReadRequiredProperty(obj, propertyName, description);
        if (node is not JsonArray array)
        {
            throw new FormatException($"{description} must be a JSON array.");
        }

        return array.Select(item =>
        {
            if (item is not JsonValue value || !value.TryGetValue<string>(out var result))
            {
                throw new FormatException($"{description} item must be a JSON string.");
            }

            return result ?? string.Empty;
        }).ToArray();
    }

    private static int ReadInt32(JsonObject obj, string propertyName, string description)
    {
        var node = ReadRequiredProperty(obj, propertyName, description);
        if (node is not JsonValue value || !value.TryGetValue<int>(out var result))
        {
            throw new FormatException($"{description} must be a JSON integer.");
        }

        return result;
    }

    private static float ReadSingle(JsonObject obj, string propertyName, string description)
    {
        var node = ReadRequiredProperty(obj, propertyName, description);
        if (node is not JsonValue value || !value.TryGetValue<float>(out var result))
        {
            throw new FormatException($"{description} must be a JSON number.");
        }

        return result;
    }

    private static bool ReadBoolean(JsonObject obj, string propertyName, string description)
    {
        var node = ReadRequiredProperty(obj, propertyName, description);
        if (node is not JsonValue value || !value.TryGetValue<bool>(out var result))
        {
            throw new FormatException($"{description} must be a JSON boolean.");
        }

        return result;
    }

    private static TEnum ReadEnum<TEnum>(JsonObject obj, string propertyName, string description)
        where TEnum : struct, Enum
    {
        var value = ReadString(obj, propertyName, description);
        return Enum.TryParse<TEnum>(value, ignoreCase: false, out var result) && Enum.IsDefined(result)
            ? result
            : throw new FormatException($"{description} '{value}' is not supported.");
    }

    private static JsonObject ExpectObject(JsonNode? node, string description)
    {
        return node as JsonObject ?? throw new FormatException($"{description} must be a JSON object.");
    }

    private static JsonNode CloneNode(JsonNode? node)
    {
        return JsonNode.Parse((node ?? throw new FormatException("JSON node is missing.")).ToJsonString()) ??
            throw new FormatException("JSON node is missing.");
    }

    private static void WriteText(string path, string text)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, text.Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
