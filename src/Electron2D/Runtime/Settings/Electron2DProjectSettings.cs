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
namespace Electron2D;

internal sealed class Electron2DProjectSettings
{
    public string Name { get; set; } = "Electron2D.Project";

    public string ProjectVersion { get; set; } = "0.1.0";

    public string EngineVersion { get; set; } = "0.1-preview";

    public string MainScene { get; set; } = "scenes/main.scene.json";

    public Electron2DRendererProfileSetting RendererProfile { get; set; } = Electron2DRendererProfileSetting.Automatic;

    public int PhysicsTicksPerSecond { get; set; } = 60;

    public InputMapActionSnapshot[] InputActions { get; set; } = [];

    public Electron2DDisplaySettings Display { get; set; } = new();

    public static Electron2DProjectSettings Capture(
        string name,
        string projectVersion,
        string engineVersion,
        string mainScene)
    {
        return new Electron2DProjectSettings
        {
            Name = name,
            ProjectVersion = projectVersion,
            EngineVersion = engineVersion,
            MainScene = mainScene,
            InputActions = InputMap.CaptureActionSettings()
        };
    }

    public void ApplyToRuntime()
    {
        Validate();
        InputMap.ReplaceActionSettings(InputActions);
        Display.ApplyToRuntime();
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new FormatException("Project name must be a non-empty string.");
        }

        if (string.IsNullOrWhiteSpace(ProjectVersion))
        {
            throw new FormatException("Project version must be a non-empty string.");
        }

        if (string.IsNullOrWhiteSpace(EngineVersion))
        {
            throw new FormatException("Engine version must be a non-empty string.");
        }

        if (string.IsNullOrWhiteSpace(MainScene))
        {
            throw new FormatException("Project main scene must be a non-empty string.");
        }

        if (!Enum.IsDefined(RendererProfile))
        {
            throw new FormatException($"Renderer profile setting '{RendererProfile}' is not supported.");
        }

        if (PhysicsTicksPerSecond <= 0)
        {
            throw new FormatException("Physics ticks per second must be positive.");
        }

        ArgumentNullException.ThrowIfNull(InputActions);
        ArgumentNullException.ThrowIfNull(Display);
        ValidateInputActions(InputActions);
        Display.Validate();
    }

    private static void ValidateInputActions(IEnumerable<InputMapActionSnapshot> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var action in actions)
        {
            var name = InputMap.ValidateActionName(action.Name);
            InputMap.ValidateDeadzone(action.Deadzone);
            if (!names.Add(name))
            {
                throw new FormatException($"Input action '{name}' is duplicated.");
            }

            foreach (var inputEvent in action.Events)
            {
                if (inputEvent is not InputEventKey and not InputEventMouseButton and not InputEventJoypadButton and not InputEventJoypadMotion)
                {
                    throw new FormatException("Input action settings contain an event type that cannot be persisted.");
                }

                _ = InputEventSignature.From(inputEvent);
            }
        }
    }
}
