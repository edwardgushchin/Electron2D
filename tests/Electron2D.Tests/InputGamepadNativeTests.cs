using System.Runtime.InteropServices;
using Electron2D;
using SDL3;

internal static class InputGamepadNativeTests
{
    internal static void RunIgnoredDevice()
    {
        Check(Input.Instance.ShouldIgnoreDevice(0x1234, 0x5678) &&
              !Input.Instance.ShouldIgnoreDevice(0x3412, 0x7856),
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
            display.ProcessEvents();
            Check(Input.Instance.GetConnectedJoypads().Length == 0,
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
        Check(Input.Instance.GetJoyName(int.MaxValue) == string.Empty &&
              Input.Instance.GetJoyGUID(int.MaxValue) == string.Empty &&
              Input.Instance.GetJoyInfo(int.MaxValue) is null &&
              !Input.Instance.IsJoyKnown(int.MaxValue) &&
              !Input.Instance.HasJoyVibration(int.MaxValue) &&
              !Input.Instance.HasJoyLight(int.MaxValue),
            "Absent controller metadata uses typed empty defaults.");
        Reject<ArgumentOutOfRangeException>(() => Input.Instance.GetJoyName(-1));
        Reject<ArgumentOutOfRangeException>(() => Input.Instance.StartJoyVibration(0, float.NaN, 0));
        Reject<ArgumentOutOfRangeException>(() => Input.Instance.StartJoyVibration(0, 0, 0, -1));
        Reject<ArgumentException>(() => Input.Instance.SetJoyLight(0, new Color(float.NaN, 0, 0)));
        Input.Instance.StartJoyVibration(int.MaxValue, .2f, .3f, 1);
        Check(Input.Instance.GetJoyVibrationStrength(int.MaxValue) == new Vector2(.2f, .3f) &&
              !Input.Instance.IsJoyVibrating(int.MaxValue),
            "An absent device retains the request without reporting an active native effect.");
        Input.Instance.StopJoyVibration(int.MaxValue);
        using var display = DisplayServer.Open("Virtual gamepad checks", new(96, 64));
        var focusTransitions = new List<bool>();
        display.WindowFocusChanged += focusTransitions.Add;
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
        Input.Instance.JoyConnectionChanged += OnConnection;
        try
        {
            instance = SDL.AttachVirtualJoystick(in descriptor);
            Check(instance != 0, "SDL attached the virtual controller.");
            display.ProcessEvents();
            var devices = Input.Instance.GetConnectedJoypads();
            Check(devices.Length == 1 && transitions.SequenceEqual([(devices[0], true)]),
                "Connection metadata commits before one notification.");
            var device = devices[0];
            Check(Input.Instance.IsJoyKnown(device) && Input.Instance.GetJoyName(device).Length != 0 &&
                  Input.Instance.GetJoyGUID(device).Length == 32 && Input.Instance.GetJoyInfo(device) is { } info &&
                  info.RawName == "Electron2D Virtual Pad", "A mapped virtual controller exposes typed identity.");
            Check(Input.Instance.HasJoyVibration(device) && Input.Instance.HasJoyLight(device),
                "The virtual controller exposes its rumble and LED capabilities.");
            Input.Instance.StartJoyVibration(device, .25f, .5f, .2f);
            Check(rumble.Count == 1 && rumble[0].Low is >= 32767 and <= 32768 &&
                  rumble[0].High is >= 16383 and <= 16384 &&
                  Input.Instance.GetJoyVibrationStrength(device) == new Vector2(.25f, .5f) &&
                  Input.Instance.GetJoyVibrationDuration(device) == .2f &&
                  Input.Instance.GetJoyVibrationRemainingDuration(device) > 0 &&
                  Input.Instance.IsJoyVibrating(device), "Typed vibration reaches the two native motors.");
            Input.Instance.SetJoyLight(device, new Color(.2f, .4f, .6f));
            Check(leds.Count == 1 && leds[0] == (51, 102, 153), "Typed RGB light reaches the native callback.");
            Input.Instance.StopJoyVibration(device);
            Check(rumble[^1] == (0, 0) && Input.Instance.GetJoyVibrationStrength(device) == Vector2.Zero &&
                  !Input.Instance.IsJoyVibrating(device), "Stopping vibration clears the retained request.");
            Input.Instance.StartJoyVibration(device, .2f, .3f);
            Check(Input.Instance.GetJoyVibrationRemainingDuration(device) > 64,
                "Zero duration requests the native maximum rumble interval.");
            Input.Instance.StopJoyVibration(device);
            Input.Instance.StartJoyVibration(device, .1f, .2f, .05f);
            Thread.Sleep(100);
            Check(!Input.Instance.IsJoyVibrating(device) &&
                  Input.Instance.GetJoyVibrationRemainingDuration(device) == 0 &&
                  Input.Instance.GetJoyVibrationStrength(device) == new Vector2(.1f, .2f) &&
                  Input.Instance.GetJoyVibrationDuration(device) == .05f,
                "Expired rumble keeps the last request while active-state queries turn false.");
            Input.Instance.StopJoyVibration(device);

            joystick = SDL.OpenJoystick(instance);
            Check(joystick != 0 && SDL.SetJoystickVirtualButton(joystick, 0, true),
                "SDL accepted a virtual button press.");
            SDL.UpdateJoysticks(); display.ProcessEvents();
            for (var attempt = 0; attempt < 3 && !Input.Instance.IsJoyButtonPressed(JoyButton.A, device); attempt++)
            {
                // A compositor focus-loss event in the same pump releases previously delivered input.
                Check(SDL.SetJoystickVirtualButton(joystick, 0, false), "SDL accepted a virtual button reset.");
                SDL.UpdateJoysticks(); display.ProcessEvents();
                Check(SDL.SetJoystickVirtualButton(joystick, 0, true), "SDL accepted a repeated virtual press.");
                SDL.UpdateJoysticks(); display.ProcessEvents();
            }
            Check(Input.Instance.IsJoyButtonPressed(JoyButton.A, device),
                $"Native button input reaches the typed state (focused={display.WindowIsFocused()}, transitions={string.Join(',', focusTransitions)}, connected={Input.Instance.GetConnectedJoypads().Length}).");
            Check(SDL.SetJoystickVirtualAxis(joystick, 0, short.MaxValue), "SDL accepted a virtual axis update.");
            SDL.UpdateJoysticks(); display.ProcessEvents();
            Check(Input.Instance.GetJoyAxis(JoyAxis.LeftX, device) > .99f,
                "Native axis input reaches the typed state.");

            for (var index = 0; index < 32; index++)
            {
                SDL.SetJoystickVirtualButton(joystick, 0, (index & 1) != 0);
                SDL.UpdateJoysticks(); display.ProcessEvents();
            }
            var beforeActive = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 128; index++)
            {
                SDL.SetJoystickVirtualButton(joystick, 0, (index & 1) != 0);
                SDL.UpdateJoysticks(); display.ProcessEvents();
            }
            var activeBytes = GC.GetAllocatedBytesForCurrentThread() - beforeActive;
            var beforeIdle = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 128; index++) display.ProcessEvents();
            var idleBytes = GC.GetAllocatedBytesForCurrentThread() - beforeIdle;
            Console.WriteLine($"Native gamepad allocation probe: 128 active={activeBytes} bytes, 128 idle={idleBytes} bytes.");

            Input.Instance.StartJoyVibration(device, .2f, .3f, 1);
            display.SetWindowVisible(false);
            Input.Instance.IgnoreJoypadOnUnfocusedApplication = true;
            Check(!display.WindowIsFocused() && !Input.Instance.IsJoyButtonPressed(JoyButton.A, device) &&
                  rumble[^1] == (0, 0) && !Input.Instance.IsJoyVibrating(device) &&
                  Input.Instance.GetJoyVibrationStrength(device) == Vector2.Zero,
                $"Losing focus releases held controller state and stops rumble under the ignore policy (focused={display.WindowIsFocused()}, pressed={Input.Instance.IsJoyButtonPressed(JoyButton.A, device)}, lastRumble={rumble[^1]}, vibrating={Input.Instance.IsJoyVibrating(device)}, strength={Input.Instance.GetJoyVibrationStrength(device)}).");
            var rumbleBeforeIgnore = rumble.Count;
            var ledsBeforeIgnore = leds.Count;
            Input.Instance.StartJoyVibration(device, .4f, .6f, 1);
            Input.Instance.SetJoyLight(device, Colors.Blue);
            Check(rumble.Count == rumbleBeforeIgnore && leds.Count == ledsBeforeIgnore,
                "Unfocused controller effects are ignored.");
            Check(SDL.SetJoystickVirtualButton(joystick, 0, false), "SDL accepted the hidden release.");
            SDL.UpdateJoysticks(); display.ProcessEvents();
            Check(SDL.SetJoystickVirtualButton(joystick, 0, true), "SDL accepted the hidden press.");
            SDL.UpdateJoysticks(); display.ProcessEvents();
            Check(!Input.Instance.IsJoyButtonPressed(JoyButton.A, device),
                "Unfocused controller events do not repopulate raw input.");
            Input.Instance.IgnoreJoypadOnUnfocusedApplication = false;
            display.SetWindowVisible(true);

            Input.Instance.StartJoyVibration(device, .2f, .3f, 1);
            SDL.CloseJoystick(joystick); joystick = 0;
            Check(SDL.DetachVirtualJoystick(instance), "SDL detached the virtual controller.");
            instance = 0;
            display.ProcessEvents();
            Check(Input.Instance.GetConnectedJoypads().Length == 0 &&
                  !Input.Instance.IsJoyButtonPressed(JoyButton.A, device) &&
                  Input.Instance.GetJoyAxis(JoyAxis.LeftX, device) == 0 &&
                  Input.Instance.GetJoyVibrationStrength(device) == Vector2.Zero &&
                  !Input.Instance.IsJoyVibrating(device) &&
                  transitions.SequenceEqual([(device, true), (device, false)]),
                "Disconnect clears only this controller and sends one notification.");
        }
        finally
        {
            Input.Instance.IgnoreJoypadOnUnfocusedApplication = false;
            Input.Instance.JoyConnectionChanged -= OnConnection;
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
        InputMap.Instance.AddAction(action);
        InputMap.Instance.ActionAddEvent(action, binding);
        Engine.Instance.Start(tree);
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
            display.ProcessEvents();
            var device = Input.Instance.GetConnectedJoypads().Single();
            joystick = SDL.OpenJoystick(instance);
            Check(joystick != 0 && SDL.SetJoystickVirtualButton(joystick, 0, true),
                "SDL accepted the native face-button event.");
            SDL.UpdateJoysticks(); display.ProcessEvents();
            Check(probe.Count == 1 && probe.Device == device && probe.Borrowed?.IsDisposed == true &&
                  Input.Instance.IsActionPressed(action),
                "The native controller event reaches scene input and a typed action map exactly once.");
            Check(SDL.SetJoystickVirtualButton(joystick, 0, false), "SDL accepted the native face-button release.");
            SDL.UpdateJoysticks(); display.ProcessEvents();
            Check(probe.Count == 2 && !Input.Instance.IsActionPressed(action),
                "Release reaches the scene and clears mapped action state.");
        }
        finally
        {
            if (joystick != 0) SDL.CloseJoystick(joystick);
            if (instance != 0) SDL.DetachVirtualJoystick(instance);
            display.ProcessEvents();
            Engine.Instance.Stop();
            InputMap.Instance.EraseAction(action);
            GC.KeepAlive(descriptor);
            Marshal.FreeCoTaskMem(name);
        }
        Console.WriteLine("Native gamepad scene and action delivery checks passed.");
    }

    internal static void RunProjectSetting()
    {
        var settings = ProjectSettings.Instance;
        var method = settings.Get(ProjectSettings.RenderingMethod);
        var previous = settings.Get(ProjectSettings.IgnoreJoypadOnUnfocusedApplication);
        var previousHint = SDL.GetHint(SDL.Hints.JoystickAllowBackgroundEvents);
        try
        {
            settings.Set(ProjectSettings.RenderingMethod, "compatibility");
            settings.Set(ProjectSettings.IgnoreJoypadOnUnfocusedApplication, true);
            var window = new Window { Size = new(96, 64) };
            var checkedReady = false;
            window.Ready += _ =>
            {
                checkedReady = true;
                Check(Input.Instance.IgnoreJoypadOnUnfocusedApplication,
                    "Engine.Run applies the typed project policy before scene readiness.");
                window.Tree!.Quit();
            };
            Check(Engine.Instance.Run(window) == 0 && checkedReady,
                "The project policy run completed and released its native host.");
            Check(StringComparer.Ordinal.Equals(SDL.GetHint(SDL.Hints.JoystickAllowBackgroundEvents), previousHint),
                "Native shutdown restores the prior background-controller hint.");
        }
        finally
        {
            settings.Set(ProjectSettings.IgnoreJoypadOnUnfocusedApplication, previous);
            settings.Set(ProjectSettings.RenderingMethod, method);
            Input.Instance.IgnoreJoypadOnUnfocusedApplication = previous;
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
        InputMap.Instance.AddAction(action);
        InputMap.Instance.ActionAddEvent(action, binding);
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
            display.ProcessEvents();
            var devices = Input.Instance.GetConnectedJoypads();
            Check(devices.Length == 2, "Two controllers receive distinct logical IDs.");
            firstJoystick = SDL.OpenJoystick(firstInstance);
            secondJoystick = SDL.OpenJoystick(secondInstance);
            Check(firstJoystick != 0 && secondJoystick != 0 &&
                  SDL.SetJoystickVirtualButton(firstJoystick, 0, true) &&
                  SDL.SetJoystickVirtualButton(secondJoystick, 0, true),
                "Both controllers accepted independent button presses.");
            SDL.UpdateJoysticks(); display.ProcessEvents();
            Check(Input.Instance.IsJoyButtonPressed(JoyButton.A, devices[0]) &&
                  Input.Instance.IsJoyButtonPressed(JoyButton.A, devices[1]) &&
                  Input.Instance.IsActionPressed(action), "Pressed state is isolated per controller and mapped action.");
            SDL.CloseJoystick(firstJoystick); firstJoystick = 0;
            Check(SDL.DetachVirtualJoystick(firstInstance), "SDL detached the first controller.");
            firstInstance = 0;
            display.ProcessEvents();
            Check(Input.Instance.GetConnectedJoypads().SequenceEqual([devices[1]]) &&
                  !Input.Instance.IsJoyButtonPressed(JoyButton.A, devices[0]) &&
                  Input.Instance.IsJoyButtonPressed(JoyButton.A, devices[1]) &&
                  Input.Instance.IsActionPressed(action),
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
                display.ProcessEvents();
                Check(!Input.Instance.IsActionPressed(action),
                    "Disconnecting the final controller releases its mapped contribution.");
            }
            finally { InputMap.Instance.EraseAction(action); }
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
            Check(Input.Instance.GetConnectedJoypads().Length == 1,
                "Already connected gamepads are queryable before the first event pump.");
            var callbacks = 0;
            void Failing(int device, bool connected)
            {
                callbacks++;
                throw new ApplicationException("connection observer failed");
            }
            Input.Instance.JoyConnectionChanged += Failing;
            try { Reject<AggregateException>(display.ProcessEvents); }
            finally { Input.Instance.JoyConnectionChanged -= Failing; }
            Check(callbacks == 1 && Input.Instance.GetConnectedJoypads().Length == 1,
                "A failing initial connection observer sees committed state and is not retried.");
            display.ProcessEvents();
            Check(callbacks == 1, "Queued initial connection emits only once.");
            Check(SDL.DetachVirtualJoystick(instance), "SDL detached the initial controller.");
            instance = 0;
            display.ProcessEvents();
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
            display.ProcessEvents();
            var devices = Input.Instance.GetConnectedJoypads();
            Check(devices.Length == 1 && !Input.Instance.IsJoyKnown(devices[0]),
                "Unknown joysticks remain connected with raw input identities.");
            var device = devices[0];
            Check(Input.Instance.GetJoyInfo(device) is { VendorID: 0x0123, ProductID: 0x0456 },
                "Raw joystick platform identifiers are available through typed info.");
            Check(Input.Instance.GetJoyVibrationStrength(device) == Vector2.Zero,
                "A reused controller ID does not inherit the previous controller's vibration request.");
            Check(Input.Instance.HasJoyVibration(device) && Input.Instance.HasJoyLight(device),
                "Raw joysticks report their native effect capabilities.");
            Input.Instance.StartJoyVibration(device, .25f, .5f, .1f);
            Input.Instance.SetJoyLight(device, Colors.Red);
            Check(rawRumble.Count == 1 && rawRumble[0].Low is >= 32767 and <= 32768 &&
                  rawRumble[0].High is >= 16383 and <= 16384 &&
                  rawLeds.Count == 1 && rawLeds[0] == (255, 0, 0),
                "Raw joystick rumble and LED effects reach their native callbacks.");
            Input.Instance.StopJoyVibration(device);
            joystick = SDL.OpenJoystick(instance);
            Check(joystick != 0 && SDL.SetJoystickVirtualButton(joystick, 0, true),
                "SDL accepted an unmapped button press.");
            SDL.UpdateJoysticks(); display.ProcessEvents();
            Check(Input.Instance.IsJoyButtonPressed(JoyButton.A, device), "Raw joystick button input is delivered.");
            Check(SDL.SetJoystickVirtualAxis(joystick, 0, short.MinValue), "SDL accepted an unmapped axis value.");
            SDL.UpdateJoysticks(); display.ProcessEvents();
            Check(Input.Instance.GetJoyAxis(JoyAxis.LeftX, device) == -1,
                "Raw joystick axis input retains the signed endpoint.");
            var guid = Input.Instance.GetJoyGUID(device);
            var mapping = $"{guid},Mapped Virtual,a:b0,b:b1,leftx:a0,lefty:a1,";
            Input.Instance.AddJoyMapping(mapping, updateExisting: true);
            display.ProcessEvents();
            Check(Input.Instance.IsJoyKnown(device) && Input.Instance.GetJoyName(device) == "Mapped Virtual" &&
                  Input.Instance.GetConnectedJoypads().SequenceEqual([device]),
                "Adding a mapping upgrades the connected joystick without changing its logical ID.");
            Input.Instance.RemoveJoyMapping(guid);
            display.ProcessEvents();
            Check(!Input.Instance.IsJoyKnown(device) && Input.Instance.GetConnectedJoypads().SequenceEqual([device]),
                "Removing the mapping restores raw joystick delivery without disconnecting it.");
            Input.Instance.AddJoyMapping(mapping);
            Check(!Input.Instance.IsJoyKnown(device),
                "A mapping added without updateExisting leaves the current device unchanged.");
            SDL.CloseJoystick(joystick); joystick = 0;
            Check(SDL.DetachVirtualJoystick(instance), "SDL detached the unmapped joystick.");
            instance = 0;
            display.ProcessEvents();
            Check(Input.Instance.GetConnectedJoypads().Length == 0 &&
                  !Input.Instance.IsJoyButtonPressed(JoyButton.A, device),
                "Unknown joystick removal clears raw input.");
            instance = SDL.AttachVirtualJoystick(in descriptor);
            Check(instance != 0, "SDL reattached the virtual joystick.");
            display.ProcessEvents();
            var reconnected = Input.Instance.GetConnectedJoypads();
            Check(reconnected.Length == 1 && Input.Instance.IsJoyKnown(reconnected[0]),
                "Stored mappings apply to the next connection.");
            Input.Instance.RemoveJoyMapping(guid);
            display.ProcessEvents();
            Check(SDL.DetachVirtualJoystick(instance), "SDL detached the mapped reconnect.");
            instance = 0;
            display.ProcessEvents();
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
