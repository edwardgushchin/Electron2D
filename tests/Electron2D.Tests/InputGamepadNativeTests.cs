using System.Runtime.InteropServices;
using Electron2D;
using SDL3;

internal static class InputGamepadNativeTests
{
    internal static void RunIgnoredDevice()
    {
        Check(Input.ShouldIgnoreDevice(0x1234, 0x5678) &&
              !Input.ShouldIgnoreDevice(0x3412, 0x7856),
            "Ignored-device identifiers follow the configured swapped hexadecimal pair.");
        using var display = DisplayServer.Open("Ignored gamepad check", new(96, 64));
        var name = Marshal.StringToCoTaskMemUTF8("Ignored Virtual Pad");
        var descriptor = new SDL.VirtualJoystickDesc();
        SDL.InitInterface(ref descriptor);
        descriptor.Type = SDL.JoystickType.Gamepad;
        descriptor.VendorID = 0x1234;
        descriptor.ProductID = 0x5678;
        descriptor.Name = name;
        descriptor.NAxes = 6;
        descriptor.NButtons = 21;
        var instance = 0u;
        try
        {
            instance = SDL.AttachVirtualJoystick(in descriptor);
            Check(instance != 0, "SDL attached the ignored virtual controller.");
            DisplayServer.ProcessEvents();
            Check(Input.GetConnectedJoypads().Length == 0,
                "Ignored vendor/product devices never enter the native controller list.");
        }
        finally
        {
            if (instance != 0) SDL.DetachVirtualJoystick(instance);
            GC.KeepAlive(descriptor);
            Marshal.FreeCoTaskMem(name);
        }
        Console.WriteLine("Ignored virtual controller policy checks passed.");
    }

    internal static void Run()
    {
        InitialConnection();
        Check(default(JoypadInfo).RawName == string.Empty,
            "A default typed controller-info value has a safe empty raw name.");
        Check(Input.GetJoyName(int.MaxValue) == string.Empty &&
              Input.GetJoyGUID(int.MaxValue) == string.Empty &&
              Input.GetJoyInfo(int.MaxValue) is null &&
              !Input.IsJoyKnown(int.MaxValue) &&
              !Input.HasJoyVibration(int.MaxValue) &&
              !Input.HasJoyLight(int.MaxValue),
            "Absent controller metadata uses typed empty defaults.");
        Reject<ArgumentOutOfRangeException>(() => Input.GetJoyName(-1));
        Reject<ArgumentOutOfRangeException>(() => Input.StartJoyVibration(0, float.NaN, 0));
        Reject<ArgumentOutOfRangeException>(() => Input.StartJoyVibration(0, 0, 0, -1));
        Reject<ArgumentException>(() => Input.SetJoyLight(0, new Color(float.NaN, 0, 0)));
        Input.StartJoyVibration(int.MaxValue, .2f, .3f, 1);
        Check(Input.GetJoyVibrationStrength(int.MaxValue) == new Vector2(.2f, .3f) &&
              !Input.IsJoyVibrating(int.MaxValue),
            "An absent device retains the request without reporting an active native effect.");
        Input.StopJoyVibration(int.MaxValue);
        using var display = DisplayServer.Open("Virtual gamepad checks", new(96, 64));
        var focusTransitions = new List<bool>();
        DisplayServer.WindowFocusChanged += focusTransitions.Add;
        var name = Marshal.StringToCoTaskMemUTF8("Electron2D Virtual Pad");
        var descriptor = new SDL.VirtualJoystickDesc();
        SDL.InitInterface(ref descriptor);
        descriptor.Type = SDL.JoystickType.Gamepad;
        descriptor.Name = name;
        descriptor.NAxes = 6;
        descriptor.NButtons = 21;
        descriptor.AxisMask = (1u << 6) - 1;
        descriptor.ButtonMask = (1u << 21) - 1;
        var rumble = new List<(ushort Low, ushort High)>();
        var leds = new List<(byte Red, byte Green, byte Blue)>();
        descriptor.Ramble = (_, low, high) => { rumble.Add((low, high)); return true; };
        descriptor.SetLED = (_, red, green, blue) => { leds.Add((red, green, blue)); return true; };
        uint instance = 0;
        nint joystick = 0;
        var transitions = new List<(int Device, bool Connected)>();
        void OnConnection(int device, bool connected) => transitions.Add((device, connected));
        Input.JoyConnectionChanged += OnConnection;
        try
        {
            instance = SDL.AttachVirtualJoystick(in descriptor);
            Check(instance != 0, "SDL attached the virtual controller.");
            DisplayServer.ProcessEvents();
            var devices = Input.GetConnectedJoypads();
            Check(devices.Length == 1 && transitions.SequenceEqual([(devices[0], true)]),
                "Connection metadata commits before one notification.");
            var device = devices[0];
            Check(Input.IsJoyKnown(device) && Input.GetJoyName(device).Length != 0 &&
                  Input.GetJoyGUID(device).Length == 32 && Input.GetJoyInfo(device) is { } info &&
                  info.RawName == "Electron2D Virtual Pad", "A mapped virtual controller exposes typed identity.");
            Check(Input.HasJoyVibration(device) && Input.HasJoyLight(device),
                "The virtual controller exposes its rumble and LED capabilities.");
            Input.StartJoyVibration(device, .25f, .5f, .2f);
            Check(rumble.Count == 1 && rumble[0].Low is >= 32767 and <= 32768 &&
                  rumble[0].High is >= 16383 and <= 16384 &&
                  Input.GetJoyVibrationStrength(device) == new Vector2(.25f, .5f) &&
                  Input.GetJoyVibrationDuration(device) == .2f &&
                  Input.GetJoyVibrationRemainingDuration(device) > 0 &&
                  Input.IsJoyVibrating(device), "Typed vibration reaches the two native motors.");
            Input.SetJoyLight(device, new Color(.2f, .4f, .6f));
            Check(leds.Count == 1 && leds[0] == (51, 102, 153), "Typed RGB light reaches the native callback.");
            Input.StopJoyVibration(device);
            Check(rumble[^1] == (0, 0) && Input.GetJoyVibrationStrength(device) == Vector2.Zero &&
                  !Input.IsJoyVibrating(device), "Stopping vibration clears the retained request.");
            Input.StartJoyVibration(device, .2f, .3f);
            Check(Input.GetJoyVibrationRemainingDuration(device) > 64,
                "Zero duration requests the native maximum rumble interval.");
            Input.StopJoyVibration(device);
            Input.StartJoyVibration(device, .1f, .2f, .05f);
            Thread.Sleep(100);
            Check(!Input.IsJoyVibrating(device) &&
                  Input.GetJoyVibrationRemainingDuration(device) == 0 &&
                  Input.GetJoyVibrationStrength(device) == new Vector2(.1f, .2f) &&
                  Input.GetJoyVibrationDuration(device) == .05f,
                "Expired rumble keeps the last request while active-state queries turn false.");
            Input.StopJoyVibration(device);

            joystick = SDL.OpenJoystick(instance);
            Check(joystick != 0 && SDL.SetJoystickVirtualButton(joystick, 0, true),
                "SDL accepted a virtual button press.");
            SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
            for (var attempt = 0; attempt < 3 && !Input.IsJoyButtonPressed(JoyButton.A, device); attempt++)
            {
                // A compositor focus-loss event in the same pump releases previously delivered input.
                Check(SDL.SetJoystickVirtualButton(joystick, 0, false), "SDL accepted a virtual button reset.");
                SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
                Check(SDL.SetJoystickVirtualButton(joystick, 0, true), "SDL accepted a repeated virtual press.");
                SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
            }
            Check(Input.IsJoyButtonPressed(JoyButton.A, device),
                $"Native button input reaches the typed state (focused={DisplayServer.WindowIsFocused()}, transitions={string.Join(',', focusTransitions)}, connected={Input.GetConnectedJoypads().Length}).");
            Check(SDL.SetJoystickVirtualAxis(joystick, 0, short.MaxValue), "SDL accepted a virtual axis update.");
            SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
            Check(Input.GetJoyAxis(JoyAxis.LeftX, device) > .99f,
                "Native axis input reaches the typed state.");

            for (var index = 0; index < 32; index++)
            {
                SDL.SetJoystickVirtualButton(joystick, 0, (index & 1) != 0);
                SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
            }
            var beforeActive = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 128; index++)
            {
                SDL.SetJoystickVirtualButton(joystick, 0, (index & 1) != 0);
                SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
            }
            var activeBytes = GC.GetAllocatedBytesForCurrentThread() - beforeActive;
            var beforeIdle = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 128; index++) DisplayServer.ProcessEvents();
            var idleBytes = GC.GetAllocatedBytesForCurrentThread() - beforeIdle;
            Console.WriteLine($"Native gamepad allocation probe: 128 active={activeBytes} bytes, 128 idle={idleBytes} bytes.");

            Input.StartJoyVibration(device, .2f, .3f, 1);
            display.SetWindowVisible(false);
            Input.IgnoreJoypadOnUnfocusedApplication = true;
            Check(!DisplayServer.WindowIsFocused() && !Input.IsJoyButtonPressed(JoyButton.A, device) &&
                  rumble[^1] == (0, 0) && !Input.IsJoyVibrating(device) &&
                  Input.GetJoyVibrationStrength(device) == Vector2.Zero,
                $"Losing focus releases held controller state and stops rumble under the ignore policy (focused={DisplayServer.WindowIsFocused()}, pressed={Input.IsJoyButtonPressed(JoyButton.A, device)}, lastRumble={rumble[^1]}, vibrating={Input.IsJoyVibrating(device)}, strength={Input.GetJoyVibrationStrength(device)}).");
            var rumbleBeforeIgnore = rumble.Count;
            var ledsBeforeIgnore = leds.Count;
            Input.StartJoyVibration(device, .4f, .6f, 1);
            Input.SetJoyLight(device, Colors.Blue);
            Check(rumble.Count == rumbleBeforeIgnore && leds.Count == ledsBeforeIgnore,
                "Unfocused controller effects are ignored.");
            Check(SDL.SetJoystickVirtualButton(joystick, 0, false), "SDL accepted the hidden release.");
            SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
            Check(SDL.SetJoystickVirtualButton(joystick, 0, true), "SDL accepted the hidden press.");
            SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
            Check(!Input.IsJoyButtonPressed(JoyButton.A, device),
                "Unfocused controller events do not repopulate raw input.");
            Input.IgnoreJoypadOnUnfocusedApplication = false;
            display.SetWindowVisible(true);

            Input.StartJoyVibration(device, .2f, .3f, 1);
            SDL.CloseJoystick(joystick); joystick = 0;
            Check(SDL.DetachVirtualJoystick(instance), "SDL detached the virtual controller.");
            instance = 0;
            DisplayServer.ProcessEvents();
            Check(Input.GetConnectedJoypads().Length == 0 &&
                  !Input.IsJoyButtonPressed(JoyButton.A, device) &&
                  Input.GetJoyAxis(JoyAxis.LeftX, device) == 0 &&
                  Input.GetJoyVibrationStrength(device) == Vector2.Zero &&
                  !Input.IsJoyVibrating(device) &&
                  transitions.SequenceEqual([(device, true), (device, false)]),
                "Disconnect clears only this controller and sends one notification.");
        }
        finally
        {
            Input.IgnoreJoypadOnUnfocusedApplication = false;
            Input.JoyConnectionChanged -= OnConnection;
            if (joystick != 0) SDL.CloseJoystick(joystick);
            if (instance != 0) SDL.DetachVirtualJoystick(instance);
            GC.KeepAlive(descriptor);
            Marshal.FreeCoTaskMem(name);
        }
        RawJoystick(display);
        DeviceIsolation(display);
        Console.WriteLine("Virtual SDL gamepad connection and typed input checks passed.");
    }

    internal static void RunSceneDelivery()
    {
        using var display = DisplayServer.Open("Gamepad scene delivery", new(96, 64));
        var root = new TestViewport();
        var probe = new GamepadProbe();
        root.AddChild(probe);
        using var tree = new SceneTree(root);
        const string action = "native_gamepad_scene_test";
        using var binding = new InputEventJoypadButton { Device = InputMap.AllDevices, ButtonIndex = JoyButton.A };
        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, binding);
        Engine.Start(tree);
        var name = Marshal.StringToCoTaskMemUTF8("Scene Virtual Pad");
        var descriptor = new SDL.VirtualJoystickDesc();
        SDL.InitInterface(ref descriptor);
        descriptor.Type = SDL.JoystickType.Gamepad;
        descriptor.Name = name;
        descriptor.NAxes = 6;
        descriptor.NButtons = 21;
        descriptor.ButtonMask = (1u << 21) - 1;
        uint instance = 0;
        nint joystick = 0;
        try
        {
            instance = SDL.AttachVirtualJoystick(in descriptor);
            Check(instance != 0, "SDL attached a controller to the active scene.");
            DisplayServer.ProcessEvents();
            var device = Input.GetConnectedJoypads().Single();
            joystick = SDL.OpenJoystick(instance);
            Check(joystick != 0 && SDL.SetJoystickVirtualButton(joystick, 0, true),
                "SDL accepted the native face-button event.");
            SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
            Check(probe.Count == 1 && probe.Device == device && probe.Borrowed?.IsDisposed == true &&
                  Input.IsActionPressed(action),
                "The native controller event reaches scene input and a typed action map exactly once.");
            Check(SDL.SetJoystickVirtualButton(joystick, 0, false), "SDL accepted the native face-button release.");
            SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
            Check(probe.Count == 2 && !Input.IsActionPressed(action),
                "Release reaches the scene and clears mapped action state.");
        }
        finally
        {
            if (joystick != 0) SDL.CloseJoystick(joystick);
            if (instance != 0) SDL.DetachVirtualJoystick(instance);
            DisplayServer.ProcessEvents();
            Engine.Stop();
            InputMap.EraseAction(action);
            GC.KeepAlive(descriptor);
            Marshal.FreeCoTaskMem(name);
        }
        Console.WriteLine("Native gamepad scene and action delivery checks passed.");
    }

    internal static void RunProjectSetting()
    {
        var settings = ProjectSettings.Service;
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var previous = ProjectSettings.Get(ProjectSettings.IgnoreJoypadOnUnfocusedApplication);
        var previousHint = SDL.GetHint(SDL.Hints.JoystickAllowBackgroundEvents);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
            ProjectSettings.Set(ProjectSettings.IgnoreJoypadOnUnfocusedApplication, true);
            var window = new Window { Size = new(96, 64) };
            var checkedReady = false;
            window.Ready += _ =>
            {
                checkedReady = true;
                Check(Input.IgnoreJoypadOnUnfocusedApplication,
                    "Engine.Run applies the typed project policy before scene readiness.");
                window.Tree!.Quit();
            };
            Check(Engine.Run(window) == 0 && checkedReady,
                "The project policy run completed and released its native host.");
            Check(StringComparer.Ordinal.Equals(SDL.GetHint(SDL.Hints.JoystickAllowBackgroundEvents), previousHint),
                "Native shutdown restores the prior background-controller hint.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.IgnoreJoypadOnUnfocusedApplication, previous);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, method);
            Input.IgnoreJoypadOnUnfocusedApplication = previous;
        }
        Console.WriteLine("Gamepad project setting host integration checks passed.");
    }

    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 96, 64); }

    private sealed class GamepadProbe : Node
    {
        internal int Count, Device = -1;
        internal InputEvent? Borrowed;
        internal GamepadProbe() => InputEnabled = true;
        protected override void OnInput(InputEvent input)
        {
            if (input is not InputEventJoypadButton { ButtonIndex: JoyButton.A }) return;
            Count++;
            Device = input.Device;
            Borrowed = input;
        }
    }

    private static void DeviceIsolation(DisplayServer display)
    {
        const string action = "native_gamepad_isolation_test";
        using var binding = new InputEventJoypadButton { Device = InputMap.AllDevices, ButtonIndex = JoyButton.A };
        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, binding);
        var firstName = Marshal.StringToCoTaskMemUTF8("First Isolated Pad");
        var secondName = Marshal.StringToCoTaskMemUTF8("Second Isolated Pad");
        var first = new SDL.VirtualJoystickDesc();
        SDL.InitInterface(ref first);
        first.Type = SDL.JoystickType.Gamepad;
        first.Name = firstName;
        first.VendorID = 0x1111;
        first.ProductID = 0x2222;
        first.NAxes = 6;
        first.NButtons = 21;
        first.ButtonMask = (1u << 21) - 1;
        var second = first;
        second.Name = secondName;
        second.VendorID = 0x3333;
        second.ProductID = 0x4444;
        uint firstInstance = 0, secondInstance = 0;
        nint firstJoystick = 0, secondJoystick = 0;
        try
        {
            firstInstance = SDL.AttachVirtualJoystick(in first);
            secondInstance = SDL.AttachVirtualJoystick(in second);
            Check(firstInstance != 0 && secondInstance != 0, "SDL attached both independent controllers.");
            DisplayServer.ProcessEvents();
            var devices = Input.GetConnectedJoypads();
            Check(devices.Length == 2, "Two controllers receive distinct logical IDs.");
            firstJoystick = SDL.OpenJoystick(firstInstance);
            secondJoystick = SDL.OpenJoystick(secondInstance);
            Check(firstJoystick != 0 && secondJoystick != 0 &&
                  SDL.SetJoystickVirtualButton(firstJoystick, 0, true) &&
                  SDL.SetJoystickVirtualButton(secondJoystick, 0, true),
                "Both controllers accepted independent button presses.");
            SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
            Check(Input.IsJoyButtonPressed(JoyButton.A, devices[0]) &&
                  Input.IsJoyButtonPressed(JoyButton.A, devices[1]) &&
                  Input.IsActionPressed(action), "Pressed state is isolated per controller and mapped action.");
            SDL.CloseJoystick(firstJoystick); firstJoystick = 0;
            Check(SDL.DetachVirtualJoystick(firstInstance), "SDL detached the first controller.");
            firstInstance = 0;
            DisplayServer.ProcessEvents();
            Check(Input.GetConnectedJoypads().SequenceEqual([devices[1]]) &&
                  !Input.IsJoyButtonPressed(JoyButton.A, devices[0]) &&
                  Input.IsJoyButtonPressed(JoyButton.A, devices[1]) &&
                  Input.IsActionPressed(action),
                "Disconnecting one controller preserves the other's raw and action state.");
        }
        finally
        {
            if (firstJoystick != 0) SDL.CloseJoystick(firstJoystick);
            if (secondJoystick != 0) SDL.CloseJoystick(secondJoystick);
            if (firstInstance != 0) SDL.DetachVirtualJoystick(firstInstance);
            if (secondInstance != 0) SDL.DetachVirtualJoystick(secondInstance);
            try
            {
                DisplayServer.ProcessEvents();
                Check(!Input.IsActionPressed(action),
                    "Disconnecting the final controller releases its mapped contribution.");
            }
            finally { InputMap.EraseAction(action); }
            GC.KeepAlive(first); GC.KeepAlive(second);
            Marshal.FreeCoTaskMem(firstName); Marshal.FreeCoTaskMem(secondName);
        }
    }

    private static void InitialConnection()
    {
        Check(SDL.InitSubSystem(SDL.InitFlags.Gamepad), "SDL initialized gamepads before the display.");
        var name = Marshal.StringToCoTaskMemUTF8("Initially Connected Pad");
        var descriptor = new SDL.VirtualJoystickDesc();
        SDL.InitInterface(ref descriptor);
        descriptor.Type = SDL.JoystickType.Gamepad;
        descriptor.Name = name;
        descriptor.NAxes = 6;
        descriptor.NButtons = 21;
        uint instance = 0;
        try
        {
            instance = SDL.AttachVirtualJoystick(in descriptor);
            Check(instance != 0, "SDL attached a controller before opening the display.");
            using var display = DisplayServer.Open("Initially connected gamepad", new(96, 64));
            Check(Input.GetConnectedJoypads().Length == 1,
                "Already connected gamepads are queryable before the first event pump.");
            var callbacks = 0;
            void Failing(int device, bool connected)
            {
                callbacks++;
                throw new ApplicationException("connection observer failed");
            }
            Input.JoyConnectionChanged += Failing;
            try { Reject<AggregateException>(DisplayServer.ProcessEvents); }
            finally { Input.JoyConnectionChanged -= Failing; }
            Check(callbacks == 1 && Input.GetConnectedJoypads().Length == 1,
                "A failing initial connection observer sees committed state and is not retried.");
            DisplayServer.ProcessEvents();
            Check(callbacks == 1, "Queued initial connection emits only once.");
            Check(SDL.DetachVirtualJoystick(instance), "SDL detached the initial controller.");
            instance = 0;
            DisplayServer.ProcessEvents();
        }
        finally
        {
            if (instance != 0) SDL.DetachVirtualJoystick(instance);
            SDL.QuitSubSystem(SDL.InitFlags.Gamepad);
            GC.KeepAlive(descriptor);
            Marshal.FreeCoTaskMem(name);
        }
    }

    private static void RawJoystick(DisplayServer display)
    {
        var name = Marshal.StringToCoTaskMemUTF8("Electron2D Raw Stick");
        var descriptor = new SDL.VirtualJoystickDesc();
        SDL.InitInterface(ref descriptor);
        descriptor.Type = SDL.JoystickType.Unknown;
        descriptor.Name = name;
        descriptor.VendorID = 0x0123;
        descriptor.ProductID = 0x0456;
        descriptor.NAxes = 2;
        descriptor.NButtons = 2;
        var rawRumble = new List<(ushort Low, ushort High)>();
        var rawLeds = new List<(byte Red, byte Green, byte Blue)>();
        descriptor.Ramble = (_, low, high) => { rawRumble.Add((low, high)); return true; };
        descriptor.SetLED = (_, red, green, blue) => { rawLeds.Add((red, green, blue)); return true; };
        uint instance = 0;
        nint joystick = 0;
        try
        {
            instance = SDL.AttachVirtualJoystick(in descriptor);
            Check(instance != 0, "SDL attached an unmapped virtual joystick.");
            DisplayServer.ProcessEvents();
            var devices = Input.GetConnectedJoypads();
            Check(devices.Length == 1 && !Input.IsJoyKnown(devices[0]),
                "Unknown joysticks remain connected with raw input identities.");
            var device = devices[0];
            Check(Input.GetJoyInfo(device) is { VendorID: 0x0123, ProductID: 0x0456 },
                "Raw joystick platform identifiers are available through typed info.");
            Check(Input.GetJoyVibrationStrength(device) == Vector2.Zero,
                "A reused controller ID does not inherit the previous controller's vibration request.");
            Check(Input.HasJoyVibration(device) && Input.HasJoyLight(device),
                "Raw joysticks report their native effect capabilities.");
            Input.StartJoyVibration(device, .25f, .5f, .1f);
            Input.SetJoyLight(device, Colors.Red);
            Check(rawRumble.Count == 1 && rawRumble[0].Low is >= 32767 and <= 32768 &&
                  rawRumble[0].High is >= 16383 and <= 16384 &&
                  rawLeds.Count == 1 && rawLeds[0] == (255, 0, 0),
                "Raw joystick rumble and LED effects reach their native callbacks.");
            Input.StopJoyVibration(device);
            joystick = SDL.OpenJoystick(instance);
            Check(joystick != 0 && SDL.SetJoystickVirtualButton(joystick, 0, true),
                "SDL accepted an unmapped button press.");
            SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
            Check(Input.IsJoyButtonPressed(JoyButton.A, device), "Raw joystick button input is delivered.");
            Check(SDL.SetJoystickVirtualAxis(joystick, 0, short.MinValue), "SDL accepted an unmapped axis value.");
            SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
            Check(Input.GetJoyAxis(JoyAxis.LeftX, device) == -1,
                "Raw joystick axis input retains the signed endpoint.");
            var guid = Input.GetJoyGUID(device);
            var mapping = $"{guid},Mapped Virtual,a:b0,b:b1,leftx:a0,lefty:a1,";
            Input.AddJoyMapping(mapping, updateExisting: true);
            DisplayServer.ProcessEvents();
            Check(Input.IsJoyKnown(device) && Input.GetJoyName(device) == "Mapped Virtual" &&
                  Input.GetConnectedJoypads().SequenceEqual([device]),
                "Adding a mapping upgrades the connected joystick without changing its logical ID.");
            Input.RemoveJoyMapping(guid);
            DisplayServer.ProcessEvents();
            Check(!Input.IsJoyKnown(device) && Input.GetConnectedJoypads().SequenceEqual([device]),
                "Removing the mapping restores raw joystick delivery without disconnecting it.");
            Input.AddJoyMapping(mapping);
            Check(!Input.IsJoyKnown(device),
                "A mapping added without updateExisting leaves the current device unchanged.");
            SDL.CloseJoystick(joystick); joystick = 0;
            Check(SDL.DetachVirtualJoystick(instance), "SDL detached the unmapped joystick.");
            instance = 0;
            DisplayServer.ProcessEvents();
            Check(Input.GetConnectedJoypads().Length == 0 &&
                  !Input.IsJoyButtonPressed(JoyButton.A, device),
                "Unknown joystick removal clears raw input.");
            instance = SDL.AttachVirtualJoystick(in descriptor);
            Check(instance != 0, "SDL reattached the virtual joystick.");
            DisplayServer.ProcessEvents();
            var reconnected = Input.GetConnectedJoypads();
            Check(reconnected.Length == 1 && Input.IsJoyKnown(reconnected[0]),
                "Stored mappings apply to the next connection.");
            Input.RemoveJoyMapping(guid);
            DisplayServer.ProcessEvents();
            Check(SDL.DetachVirtualJoystick(instance), "SDL detached the mapped reconnect.");
            instance = 0;
            DisplayServer.ProcessEvents();
        }
        finally
        {
            if (joystick != 0) SDL.CloseJoystick(joystick);
            if (instance != 0) SDL.DetachVirtualJoystick(instance);
            GC.KeepAlive(descriptor);
            Marshal.FreeCoTaskMem(name);
        }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
