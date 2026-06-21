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

internal static class InputMapProjectSettings
{
    private const int FormatVersion = 1;
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static void Save(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, Serialize(InputMap.CaptureActionSettings()));
    }

    public static void Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        InputMap.ReplaceActionSettings(Deserialize(File.ReadAllText(path)));
    }

    public static string Serialize(IEnumerable<InputMapActionSnapshot> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);

        var result = new JsonObject
        {
            ["format"] = FormatVersion,
            ["actions"] = new JsonArray(actions
                .OrderBy(action => action.Name, StringComparer.Ordinal)
                .Select(action => (JsonNode)WriteAction(action))
                .ToArray())
        };

        return result.ToJsonString(IndentedOptions);
    }

    public static InputMapActionSnapshot[] Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            var root = ExpectObject(JsonNode.Parse(text), "Input map settings");
            var format = ReadInt32(root, "format", "Input map settings format");
            if (format != FormatVersion)
            {
                throw new FormatException($"Input map settings format '{format}' is not supported.");
            }

            return ExpectArray(ReadRequiredProperty(root, "actions", "Input map actions"), "Input map actions")
                .Select(ReadAction)
                .ToArray();
        }
        catch (JsonException exception)
        {
            throw new FormatException("Input map settings JSON text is malformed.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new FormatException("Input map settings JSON text is malformed.", exception);
        }
    }

    private static JsonObject WriteAction(InputMapActionSnapshot action)
    {
        return new JsonObject
        {
            ["name"] = action.Name,
            ["deadzone"] = action.Deadzone,
            ["events"] = new JsonArray(action.Events
                .Where(IsPersistable)
                .OrderBy(inputEvent => InputEventSignature.From(inputEvent).SortKey, StringComparer.Ordinal)
                .Select(inputEvent => (JsonNode)WriteEvent(inputEvent))
                .ToArray())
        };
    }

    private static InputMapActionSnapshot ReadAction(JsonNode? node)
    {
        var action = ExpectObject(node, "Input map action");
        var name = ReadString(action, "name", "Input map action name");
        var deadzone = ReadSingle(action, "deadzone", "Input map action deadzone");
        var events = ExpectArray(ReadRequiredProperty(action, "events", "Input map action events"), "Input map action events")
            .Select(ReadEvent)
            .ToArray();

        return new InputMapActionSnapshot(name, deadzone, events);
    }

    private static JsonObject WriteEvent(InputEvent inputEvent)
    {
        return inputEvent switch
        {
            InputEventKey key => new JsonObject
            {
                ["type"] = "key",
                ["keycode"] = key.Keycode.ToString(),
                ["physical_keycode"] = key.PhysicalKeycode.ToString()
            },
            InputEventMouseButton mouse => new JsonObject
            {
                ["type"] = "mouse_button",
                ["button"] = mouse.ButtonIndex.ToString()
            },
            InputEventJoypadButton joypadButton => new JsonObject
            {
                ["type"] = "joy_button",
                ["button"] = joypadButton.ButtonIndex.ToString()
            },
            InputEventJoypadMotion joypadMotion => new JsonObject
            {
                ["type"] = "joy_motion",
                ["axis"] = joypadMotion.Axis.ToString(),
                ["axis_value"] = joypadMotion.AxisValue
            },
            _ => throw new InvalidOperationException("Input event type cannot be persisted.")
        };
    }

    private static InputEvent ReadEvent(JsonNode? node)
    {
        var inputEvent = ExpectObject(node, "Input map event");
        var type = ReadString(inputEvent, "type", "Input map event type");
        return type switch
        {
            "key" => new InputEventKey
            {
                Keycode = ReadEnum<Key>(inputEvent, "keycode", "Input map keycode"),
                PhysicalKeycode = ReadEnum<Key>(inputEvent, "physical_keycode", "Input map physical keycode")
            },
            "mouse_button" => new InputEventMouseButton
            {
                ButtonIndex = ReadEnum<MouseButton>(inputEvent, "button", "Input map mouse button")
            },
            "joy_button" => new InputEventJoypadButton
            {
                ButtonIndex = ReadEnum<JoyButton>(inputEvent, "button", "Input map gamepad button")
            },
            "joy_motion" => new InputEventJoypadMotion
            {
                Axis = ReadEnum<JoyAxis>(inputEvent, "axis", "Input map gamepad axis"),
                AxisValue = ReadSingle(inputEvent, "axis_value", "Input map gamepad axis value")
            },
            _ => throw new FormatException($"Input map event type '{type}' is not supported.")
        };
    }

    private static bool IsPersistable(InputEvent inputEvent)
    {
        return inputEvent is InputEventKey or InputEventMouseButton or InputEventJoypadButton or InputEventJoypadMotion;
    }

    private static JsonObject ExpectObject(JsonNode? node, string context)
    {
        return node as JsonObject ?? throw new FormatException($"{context} must be a JSON object.");
    }

    private static JsonArray ExpectArray(JsonNode? node, string context)
    {
        return node as JsonArray ?? throw new FormatException($"{context} must be a JSON array.");
    }

    private static JsonNode ReadRequiredProperty(JsonObject obj, string propertyName, string context)
    {
        return obj.TryGetPropertyValue(propertyName, out var value) && value is not null
            ? value
            : throw new FormatException($"{context} is missing required property '{propertyName}'.");
    }

    private static int ReadInt32(JsonObject obj, string propertyName, string context)
    {
        return ReadValue<int>(ReadRequiredProperty(obj, propertyName, context), context);
    }

    private static float ReadSingle(JsonObject obj, string propertyName, string context)
    {
        return ReadValue<float>(ReadRequiredProperty(obj, propertyName, context), context);
    }

    private static string ReadString(JsonObject obj, string propertyName, string context)
    {
        return ReadValue<string>(ReadRequiredProperty(obj, propertyName, context), context);
    }

    private static TEnum ReadEnum<TEnum>(JsonObject obj, string propertyName, string context)
        where TEnum : struct, Enum
    {
        var value = ReadString(obj, propertyName, context);
        return Enum.TryParse<TEnum>(value, ignoreCase: false, out var result)
            ? result
            : throw new FormatException($"{context} '{value}' is not supported.");
    }

    private static TValue ReadValue<TValue>(JsonNode node, string context)
    {
        try
        {
            return node.GetValue<TValue>();
        }
        catch (InvalidOperationException exception)
        {
            throw new FormatException($"{context} has an invalid JSON value.", exception);
        }
        catch (FormatException exception)
        {
            throw new FormatException($"{context} has an invalid JSON value.", exception);
        }
    }
}
