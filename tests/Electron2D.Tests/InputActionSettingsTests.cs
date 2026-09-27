using Electron2D;
using IOPath = System.IO.Path;

internal static class InputActionSettingsTests
{
    internal static void Run()
    {
        Reject<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<InputActionSettings>("{\"Version\":1,\"Unknown\":0}"));
        Reject<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<InputBindingSettings>("{\"Kind\":0,\"Unknown\":0}"));
        var map = InputMap.Instance;
        var root = IOPath.Combine(IOPath.GetTempPath(), "electron2d-input-" + Guid.NewGuid().ToString("N"));
        var project = IOPath.Combine(root, "project");
        var user = IOPath.Combine(root, "user");
        Directory.CreateDirectory(project);
        Directory.CreateDirectory(user);

        try
        {
            var actionSetting = new ProjectSetting<InputActionSettings>("input/jump", new InputActionSettings());
            using (var settings = new ProjectSettings(project, user))
            {
                settings.Register(actionSetting);
                settings.Set(actionSetting, new InputActionSettings
                {
                    Deadzone = 0.3f,
                    Bindings =
                    [
                        new() { Kind = InputBindingKind.Key, Keycode = Key.Space, Modifiers = KeyModifierMask.Shift },
                        new() { Kind = InputBindingKind.Key, Keycode = Key.Space, Modifiers = KeyModifierMask.Shift },
                        new() { Kind = InputBindingKind.MouseButton, MouseButtonIndex = MouseButton.Right },
                        new() { Kind = InputBindingKind.JoypadButton, JoyButtonIndex = JoyButton.A, Device = InputMap.AllDevices },
                        new() { Kind = InputBindingKind.JoypadMotion, JoyAxis = JoyAxis.LeftX, AxisValue = -1f, Device = InputMap.AllDevices },
                        new() { Kind = InputBindingKind.Action, Action = "jump" },
                    ],
                });
                settings.Save();
            }

            using var loaded = new ProjectSettings(project, user);
            loaded.Register(actionSetting);
            loaded.Load();
            Reject<InvalidOperationException>(() => loaded.Unregister(ProjectSettings.InputUIFocusNext));
            Check(loaded.Get(actionSetting).Bindings.Length == 6, "Typed input bindings survive a project-file round trip.");
            var directions = new[]
            {
                (ProjectSettings.InputUILeft, "ui_left", Key.Left, JoyButton.DpadLeft, JoyAxis.LeftX, -1f),
                (ProjectSettings.InputUIUp, "ui_up", Key.Up, JoyButton.DpadUp, JoyAxis.LeftY, -1f),
                (ProjectSettings.InputUIRight, "ui_right", Key.Right, JoyButton.DpadRight, JoyAxis.LeftX, 1f),
                (ProjectSettings.InputUIDown, "ui_down", Key.Down, JoyButton.DpadDown, JoyAxis.LeftY, 1f),
            };
            foreach (var (setting, _, keycode, joyButton, joyAxis, axisValue) in directions)
            {
                var bindings = loaded.Get(setting).Bindings;
                Check(bindings.Length == 3 &&
                    bindings[0] is { Kind: InputBindingKind.Key, Keycode: var key } && key == keycode &&
                    bindings[1] is { Kind: InputBindingKind.JoypadButton, JoyButtonIndex: var button, Device: InputMap.AllDevices } && button == joyButton &&
                    bindings[2] is { Kind: InputBindingKind.JoypadMotion, JoyAxis: var axis, AxisValue: var value, Device: InputMap.AllDevices } && axis == joyAxis && value == axisValue,
                    "Every directional default survives project-file loading with ordered key, D-pad, and left-stick bindings.");
            }

            var endpoints = new[]
            {
                (ProjectSettings.InputUIHome, "ui_home", Key.Home),
                (ProjectSettings.InputUIEnd, "ui_end", Key.End),
            };
            foreach (var (setting, _, keycode) in endpoints)
            {
                Reject<InvalidOperationException>(() => loaded.Unregister(setting));
                var bindings = loaded.Get(setting).Bindings;
                Check(bindings is [{ Kind: InputBindingKind.Key, Keycode: var endpoint, Modifiers: 0 }] && endpoint == keycode,
                    "Home and End survive project-file loading as permanent unmodified key actions.");
            }

            map.AddAction("temporary_action");
            var loadedEvents = 0;
            void OnLoaded()
            {
                loadedEvents++;
                Check(map.HasAction("jump") && !map.HasAction("temporary_action"),
                    "Loaded notification observes the committed replacement map.");
            }
            map.ProjectSettingsLoaded += OnLoaded;
            try
            {
                map.LoadFromProjectSettings(loaded);
                Check(loadedEvents == 1 && map.ActionGetDeadzone("jump") == 0.3f &&
                    map.ActionGetEvents("jump").Count == 5 && map.HasAction("ui_focus_next"),
                    "Loading replaces transient actions, restores built-ins and deduplicates ordered bindings.");

                foreach (var (_, name, keycode, joyButton, joyAxis, axisValue) in directions)
                {
                    using var arrow = new InputEventKey { Keycode = keycode, Pressed = true };
                    using var dpad = new InputEventJoypadButton { ButtonIndex = joyButton, Device = 5, Pressed = true };
                    using var stick = new InputEventJoypadMotion { Axis = joyAxis, AxisValue = axisValue, Device = 9 };
                    Check(map.ActionGetEvents(name).Count == 3 && map.EventIsAction(arrow, name, exactMatch: true) &&
                        map.EventIsAction(dpad, name, exactMatch: true) && map.EventIsAction(stick, name, exactMatch: true),
                        "Every directional action matches all three default event kinds, including nonzero controller devices.");
                }

                foreach (var (_, name, keycode) in endpoints)
                {
                    using var endpoint = new InputEventKey { Keycode = keycode, Pressed = true };
                    Check(map.ActionGetEvents(name).Count == 1 && map.EventIsAction(endpoint, name, exactMatch: true),
                        "Loaded Home and End actions match their real key events.");
                    endpoint.ShiftPressed = true;
                    Check(!map.EventIsAction(endpoint, name, exactMatch: true), "Exact endpoint matching rejects an extra modifier.");
                }

                using var key = new InputEventKey { Keycode = Key.Space, ShiftPressed = true, Pressed = true };
                using var plainKey = new InputEventKey { Keycode = Key.Space, Pressed = true };
                using var mouse = new InputEventMouseButton { ButtonIndex = MouseButton.Right, Pressed = true };
                using var button = new InputEventJoypadButton { ButtonIndex = JoyButton.A, Device = 5, Pressed = true };
                using var motion = new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = -1f, Device = 9 };
                Check(map.EventIsAction(key, "jump", exactMatch: true) &&
                    !map.EventIsAction(plainKey, "jump", exactMatch: true) &&
                    map.EventIsAction(mouse, "jump") && map.EventIsAction(button, "jump") &&
                    map.EventIsAction(motion, "jump", exactMatch: true),
                    "Every stored event family resolves through live action matching.");
                var retained = map.ActionGetEvents("jump")[0];
                for (var index = 0; index < 64; index++) _ = map.EventIsAction(key, "jump", exactMatch: true);
                var allocationStart = GC.GetAllocatedBytesForCurrentThread();
                var lastResult = false;
                for (var index = 0; index < 1_024; index++)
                    lastResult = map.EventIsAction(key, "jump", exactMatch: true);
                var allocated = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
                Check(lastResult && allocated == 0,
                    $"Warmed active matching of a loaded binding must allocate zero managed bytes; observed {allocated}.");

                Input.Instance.ActionPress("jump");
                Check(Input.Instance.IsActionPressed("jump"), "The loaded action can become pressed.");
                loaded.Set(actionSetting, new InputActionSettings { Version = 2 });
                Reject<InvalidDataException>(() => map.LoadFromProjectSettings(loaded));
                Check(loadedEvents == 1 && map.ActionGetEvents("jump").Count == 5 &&
                    Input.Instance.IsActionPressed("jump"),
                    "A future schema version leaves bindings, pressed state and notification unchanged.");

                loaded.Set(actionSetting, new InputActionSettings
                {
                    Bindings = [new InputBindingSettings { Kind = InputBindingKind.JoypadMotion, JoyAxis = JoyAxis.LeftX }],
                });
                Reject<InvalidDataException>(() => map.LoadFromProjectSettings(loaded));
                Check(loadedEvents == 1 && map.ActionGetEvents("jump").Count == 5 &&
                    Input.Instance.IsActionPressed("jump"),
                    "An invalid binding leaves the live map and pressed state unchanged.");

                using (var lastButton = (InputEventJoypadButton)new InputBindingSettings
                {
                    Kind = InputBindingKind.JoypadButton,
                    JoyButtonIndex = (JoyButton)127,
                }.CreateEvent())
                using (var lastAxis = (InputEventJoypadMotion)new InputBindingSettings
                {
                    Kind = InputBindingKind.JoypadMotion,
                    JoyAxis = (JoyAxis)9,
                    AxisValue = -1f,
                }.CreateEvent())
                    Check(lastButton.ButtonIndex == (JoyButton)127 && lastAxis.Axis == (JoyAxis)9 &&
                        lastAxis.AxisValue == -1f,
                        "The last version-one raw controller IDs and signed direction remain valid.");

                foreach (var invalid in new[]
                {
                    new InputBindingSettings { Kind = InputBindingKind.JoypadButton, JoyButtonIndex = JoyButton.Invalid },
                    new InputBindingSettings { Kind = InputBindingKind.JoypadButton, JoyButtonIndex = JoyButton.Max },
                    new InputBindingSettings { Kind = InputBindingKind.JoypadMotion, JoyAxis = JoyAxis.Invalid, AxisValue = 1f },
                    new InputBindingSettings { Kind = InputBindingKind.JoypadMotion, JoyAxis = JoyAxis.Max, AxisValue = 1f },
                    new InputBindingSettings { Kind = InputBindingKind.JoypadMotion, JoyAxis = JoyAxis.LeftX, AxisValue = float.NaN },
                    new InputBindingSettings { Kind = InputBindingKind.JoypadMotion, JoyAxis = JoyAxis.LeftX, AxisValue = float.PositiveInfinity },
                    new InputBindingSettings { Kind = InputBindingKind.JoypadMotion, JoyAxis = JoyAxis.LeftX, AxisValue = 1.01f },
                })
                    Reject<InvalidDataException>(() => { using var _ = invalid.CreateEvent(); });
                loaded.Set(actionSetting, new InputActionSettings
                {
                    Bindings =
                    [
                        new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Space },
                        new InputBindingSettings { Kind = InputBindingKind.JoypadButton, JoyButtonIndex = JoyButton.Max },
                    ],
                });
                Reject<InvalidDataException>(() => map.LoadFromProjectSettings(loaded));
                Check(loadedEvents == 1 && map.ActionGetEvents("jump").Count == 5 &&
                    Input.Instance.IsActionPressed("jump"),
                    "A late invalid controller binding must preserve the live map and pressed state.");

                loaded.Set(actionSetting, new InputActionSettings
                {
                    Bindings =
                    [
                        new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Space },
                        new InputBindingSettings { Kind = InputBindingKind.Key, Location = KeyLocation.Left },
                    ],
                });
                Reject<InvalidDataException>(() => map.LoadFromProjectSettings(loaded));
                Check(loadedEvents == 1 && map.ActionGetEvents("jump").Count == 5 &&
                    Input.Instance.IsActionPressed("jump"),
                    "A late key-location-only binding must not replace the live map after a valid candidate.");

                loaded.Set(actionSetting, new InputActionSettings
                {
                    Bindings = Enumerable.Repeat(
                        new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Space },
                        Input.MaxEventsPerAction + 1).ToArray(),
                });
                Reject<InvalidDataException>(() => map.LoadFromProjectSettings(loaded));
                Check(loadedEvents == 1 && map.ActionGetEvents("jump").Count == 5 &&
                    Input.Instance.IsActionPressed("jump"),
                    "More than 32 serialized records must fail before duplicate collapse or map replacement.");

                loaded.Set(actionSetting, new InputActionSettings
                {
                    Bindings = [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Enter }],
                });
                void ThrowingLoaded() => throw new InvalidOperationException("expected listener failure");
                map.ProjectSettingsLoaded += ThrowingLoaded;
                try { Reject<InvalidOperationException>(() => map.LoadFromProjectSettings(loaded)); }
                finally { map.ProjectSettingsLoaded -= ThrowingLoaded; }
                Check(loadedEvents == 2 && map.ActionGetEvents("jump").Count == 1 &&
                    !Input.Instance.IsActionPressed("jump") && retained is InputEventKey { Keycode: Key.Space },
                    "A throwing listener sees the committed map; old borrowed bindings remain usable and pressed state clears.");
                ((InputEventKey)retained).Keycode = Key.Backspace;
                Check(map.ActionGetEvents("jump")[0] is InputEventKey { Keycode: Key.Enter },
                    "A former binding no longer mutates the replacement map.");

                using var malformed = new ProjectSettings(project, user);
                malformed.Register(new ProjectSetting<string>("input/wrong_type", "value"));
                Reject<InvalidDataException>(() => map.LoadFromProjectSettings(malformed));
                Check(loadedEvents == 2 && map.ActionGetEvents("jump").Count == 1,
                    "A wrong typed input record cannot replace the live map.");
            }
            finally { map.ProjectSettingsLoaded -= OnLoaded; }

            using var featured = new ProjectSettings(project, user);
            featured.Register(actionSetting);
            featured.Set(actionSetting, new InputActionSettings
            {
                Bindings = [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Space }],
            });
            featured.SetFeatureOverride(actionSetting, "testsinput", new InputActionSettings
            {
                Bindings = [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Enter }],
            });
            featured.AddCustomFeature("testsinput");
            map.LoadFromProjectSettings(featured);
            using var featuredKey = new InputEventKey { Keycode = Key.Enter, Pressed = true };
            Check(map.EventIsAction(featuredKey, "jump", exactMatch: true) &&
                map.ActionGetEvents("jump").Count == 1,
                "The loaded map must use the active typed feature override.");
            featured.SetFeatureOverride(actionSetting, "testsinput", new InputActionSettings { Version = 2 });
            Reject<InvalidDataException>(() => map.LoadFromProjectSettings(featured));
            Check(map.EventIsAction(featuredKey, "jump", exactMatch: true) &&
                map.ActionGetEvents("jump").Count == 1,
                "An invalid active override must preserve the previous map.");
        }
        finally
        {
            map.LoadFromProjectSettings();
            Directory.Delete(root, recursive: true);
        }

        var processSetting = new ProjectSetting<InputActionSettings>(
            "input/tests_public_reload", new InputActionSettings());
        ProjectSettings.Instance.Register(processSetting);
        try
        {
            ProjectSettings.Instance.Set(processSetting, new InputActionSettings
            {
                Bindings = [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.F12 }],
            });
            map.LoadFromProjectSettings();
            using var processKey = new InputEventKey { Keycode = Key.F12, Pressed = true };
            Check(map.HasAction("tests_public_reload") &&
                map.EventIsAction(processKey, "tests_public_reload", exactMatch: true),
                "The public loader must read the process-wide typed settings registry.");
        }
        finally
        {
            ProjectSettings.Instance.Unregister(processSetting);
            map.LoadFromProjectSettings();
        }

        Console.WriteLine("Typed project input-action loading checks passed.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
