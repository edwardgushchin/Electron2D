using Electron2D;
using IOPath = System.IO.Path;

internal static class InputActionSettingsTests
{
    internal static void Run()
    {
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
        }
        finally
        {
            map.LoadFromProjectSettings();
            Directory.Delete(root, recursive: true);
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
