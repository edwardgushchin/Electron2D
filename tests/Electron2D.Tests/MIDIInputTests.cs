using System.Diagnostics;
using Electron2D;

internal static class MIDIInputTests
{
    private static int _checks;
    private static void Check(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException(reason);
        _checks++;
    }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { _checks++; return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    internal static void Managed()
    {
        using var value = new InputEventMIDI();
        var names = new[] { "Device", "Channel", "ControllerNumber", "ControllerValue", "Instrument", "Message", "Pitch", "Pressure", "Velocity" };
        var properties = value.GetPropertyList().Where(p => names.Contains(p.Name)).ToArray();
        Check(properties.Length == 9 && properties.All(p => p.IsStored && !value.PropertyCanRevert(p)), "All MIDI values have stored descriptors and zero defaults.");
        var changes = 0; value.Changed += _ => changes++;
        value.Device = 3; value.Channel = 15; value.Message = MIDIMessage.PitchBend; value.Pitch = 16383;
        value.ControllerNumber = 127; value.ControllerValue = 126; value.Instrument = 125; value.Pressure = 124; value.Velocity = 123;
        Check(changes == 9 && value.AsText() == "MIDI Input on Channel=15 Message=14", "Assignments emit changes and text uses numeric MIDI identities.");
        using var copy = (InputEventMIDI)value.Duplicate();
        Check(copy.Device == 3 && copy.Channel == 15 && copy.Message == MIDIMessage.PitchBend && copy.Pitch == 16383 && copy.ControllerNumber == 127 && copy.ControllerValue == 126 && copy.Instrument == 125 && copy.Pressure == 124 && copy.Velocity == 123, "Duplicate retains every event value.");
        Check(!value.IsActionType() && !value.IsPressed() && value.IsReleased() && !value.IsMatch(copy), "MIDI does not acquire press or binding semantics.");
        const string action = "tests.midi.unbound";
        InputMap.AddAction(action);
        try { Reject<ArgumentException>(() => InputMap.ActionAddEvent(action, value)); Input.ParseInputEvent(copy); Check(!Input.IsActionPressed(action) && !copy.IsAction(action), "MIDI events do not automatically bind actions."); }
        finally { InputMap.EraseAction(action); }
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-midi-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(value, path);
            using var loaded = ResourceLoader.Load<InputEventMIDI>(path, cacheMode: ResourceLoader.CacheMode.Ignore)!;
            Check(loaded.Device == 3 && loaded.Channel == 15 && loaded.Message == MIDIMessage.PitchBend && loaded.Pitch == 16383 && loaded.ControllerNumber == 127 && loaded.ControllerValue == 126 && loaded.Instrument == 125 && loaded.Pressure == 124 && loaded.Velocity == 123, "Portable storage restores the concrete registered event and enum.");
        }
        finally { File.Delete(path); }
        value.Channel = -1; value.Message = (MIDIMessage)17;
        Check(value.Channel == -1 && (int)value.Message == 17, "Authored event values retain the unrestricted integer contract.");
        value.Dispose(); Reject<ObjectDisposedException>(() => _ = value.Pitch); Reject<ObjectDisposedException>(() => value.Velocity = 0);
        Console.WriteLine($"MIDI managed PASS {_checks}");
    }

    internal static void Unavailable(bool denied = false)
    {
        if (denied) Reject<UnauthorizedAccessException>(OS.OpenMIDIInputs);
        else Reject<IOException>(OS.OpenMIDIInputs);
        OS.CloseMIDIInputs(); OS.CloseMIDIInputs();
        Check(OS.GetConnectedMIDIInputs().Length == 0, "Failed native acquisition rolls back ownership.");
        Console.WriteLine($"MIDI unavailable PASS {_checks}");
    }

    internal static void Native(string fixture)
    {
        Managed();
        using var first = new Source(fixture, "E2D MIDI A " + Environment.ProcessId);
        using var second = new Source(fixture, "E2D MIDI B " + Environment.ProcessId);
        OS.CloseMIDIInputs(); OS.OpenMIDIInputs(); OS.OpenMIDIInputs();
        var names = OS.GetConnectedMIDIInputs();
        var a = Array.FindIndex(names, n => n.Contains(first.Name, StringComparison.Ordinal));
        var b = Array.FindIndex(names, n => n.Contains(second.Name, StringComparison.Ordinal));
        Check(a >= 0 && b >= 0 && a != b, "Two real ALSA sources are subscribed with distinct Device indices.");
        names[a] = "mutated"; Check(OS.GetConnectedMIDIInputs()[a] != "mutated", "The names query returns a defensive copy.");
        using var root = new Node();
        var probe = new Probe { InputEnabled = true, UnhandledInputEnabled = true, ProcessEnabled = true, PhysicsProcessEnabled = true }; root.AddChild(probe);
        using var tree = new SceneTree(root);
        Engine.Start(tree);
        try
        {
            first.Send("9f 00 7f 7f 00 8f 7f 7f af 7f 7f bf 7f 7f cf 7f df 7f ef 00 00 7f 7f");
            second.Send("90 3c 64");
            probe.Callback = () => { probe.Callback = null; Reject<InvalidOperationException>(Input.FlushBufferedEvents); };
            Engine.AdvanceFrame(1d / 30);
            Check(probe.ProcessEventCount == 10 && probe.PhysicsEventCount == 10, "MIDI is committed before both frame callback lanes.");
            Check(probe.Events.Count == 10 && probe.Unhandled == 10, "Native messages reach both ordinary and unhandled scene input before processing.");
            var events = probe.Events;
            Check(events[0].Device == a && events[0].Channel == 15 && events[0].Pitch == 0 && events[0].Velocity == 127, "Channel and note lower/upper bounds survive native conversion.");
            Check(events[1].Message == MIDIMessage.NoteOn && events[1].Velocity == 0, "Zero-velocity note-on retains its status.");
            Check(events[2].Message == MIDIMessage.NoteOff && events[2].Velocity == 127 && events[3].Pressure == 127, "Release velocity and key pressure are retained.");
            Check(events[4].ControllerNumber == 127 && events[4].ControllerValue == 127 && events[5].Instrument == 127 && events[6].Pressure == 127, "Controller, program and channel pressure use their dedicated fields.");
            Check(events[7].Pitch == 0 && events[8].Pitch == 16383 && events[9].Device == b && events[9].Channel == 0, "Unsigned fourteen-bit pitch and independent source identity are preserved.");
            Clear(probe);
            first.Send("91 3c"); first.Send("f8"); first.Send("40 3d 41 f1 7f f2 7f 7f f3 7f f6 fa fb fc fe ff f0 01 f8 02 f7");
            Input.FlushBufferedEvents();
            Check(probe.Events.Select(e => e.Message).SequenceEqual(new[] { MIDIMessage.TimingClock, MIDIMessage.NoteOn, MIDIMessage.NoteOn, MIDIMessage.QuarterFrame, MIDIMessage.SongPositionPointer, MIDIMessage.SongSelect, MIDIMessage.TuneRequest, MIDIMessage.Start, MIDIMessage.Continue, MIDIMessage.Stop, MIDIMessage.ActiveSensing, MIDIMessage.SystemReset, MIDIMessage.TimingClock }), "Fragmented running status and interleaved realtime/system messages preserve packet order; SysEx payload is skipped.");
            Check(probe.Events[1].Pitch == 60 && probe.Events[1].Velocity == 64 && probe.Events[2].Pitch == 61, "Partial data persists in the native source parser across writes.");
            Clear(probe);
            first.Send("90 3c 64 3d 65");
            probe.Callback = () => { OS.CloseMIDIInputs(); OS.OpenMIDIInputs(); probe.Callback = null; };
            Input.FlushBufferedEvents();
            Check(probe.Events.Count == 1, "Closing and reopening from a callback discards old-session pending events.");
            Clear(probe); first.Send("90 3e 66"); Engine.AdvanceFrame(0);
            Check(probe.Events.Count == 1 && probe.Events[0].Pitch == 62, "The new session accepts fresh messages.");
            Reject<InvalidOperationException>(() => Task.Run(Input.FlushBufferedEvents).GetAwaiter().GetResult());
            Clear(probe); second.Dispose();
            Reject<IOException>(Input.FlushBufferedEvents); Reject<IOException>(() => OS.GetConnectedMIDIInputs());
            OS.OpenMIDIInputs(); Check(!OS.GetConnectedMIDIInputs().Any(n => n.Contains(second.Name, StringComparison.Ordinal)), "Disconnect fails the previous session and reopen enumerates current sources.");
            first.Send("invalid"); Reject<IOException>(Input.FlushBufferedEvents);
            OS.OpenMIDIInputs();
            first.Send("flood"); Reject<IOException>(Input.FlushBufferedEvents);
            OS.OpenMIDIInputs(); Clear(probe);
            first.Send("90 40 7f"); probe.Callback = () => throw new ApplicationException("MIDI callback");
            Reject<AggregateException>(() => Engine.AdvanceFrame(0)); probe.Callback = null;
        }
        finally { Engine.Stop(); Clear(probe); OS.CloseMIDIInputs(); }
        Check(OS.GetConnectedMIDIInputs().Length == 0, "Engine.Stop retires MIDI after a throwing user input callback.");
        using var failure = new FailingLoop(); Engine.Start(failure); OS.OpenMIDIInputs();
        Reject<ApplicationException>(Engine.Stop);
        Check(OS.GetConnectedMIDIInputs().Length == 0, "A failing finalization callback still closes the native session.");
        using var startupFailure = new FailingStart();
        Reject<ApplicationException>(() => Engine.Start(startupFailure));
        Check(OS.GetConnectedMIDIInputs().Length == 0, "Failed engine initialization retires an input session opened by the callback.");
        Console.WriteLine($"MIDI ALSA native PASS {_checks}");
    }

    private static void Clear(Probe probe) { foreach (var e in probe.Events) e.Dispose(); probe.Events.Clear(); probe.Unhandled = 0; }
    private sealed class Probe : Node
    {
        internal readonly List<InputEventMIDI> Events = [];
        internal int Unhandled;
        internal int ProcessEventCount, PhysicsEventCount;
        protected override void OnProcess(double delta) => ProcessEventCount = Events.Count;
        protected override void OnPhysicsProcess(double delta) => PhysicsEventCount = Events.Count;
        internal Action? Callback;
        protected override void OnInput(InputEvent value)
        {
            if (value is not InputEventMIDI midi) return;
            Events.Add((InputEventMIDI)midi.Duplicate()); Callback?.Invoke();
        }
        protected override void OnUnhandledInput(InputEvent value) { if (value is InputEventMIDI) Unhandled++; }
    }
    private sealed class FailingStart : MainLoop { protected override void OnInitialize() { OS.OpenMIDIInputs(); throw new ApplicationException("initialize"); } }
    private sealed class FailingLoop : MainLoop { protected override void OnFinalize() => throw new ApplicationException("finalize"); }
    private sealed class Source : IDisposable
    {
        private readonly Process _process;
        private bool _closed;
        internal readonly string Name;
        internal Source(string fixture, string name)
        {
            Name = name;
            var start = new ProcessStartInfo(fixture) { RedirectStandardInput = true, RedirectStandardOutput = true, UseShellExecute = false }; start.ArgumentList.Add(name);
            _process = Process.Start(start)!;
            if (_process.StandardOutput.ReadLine() != "READY") throw new InvalidOperationException("Native MIDI source did not open.");
        }
        internal void Send(string bytes)
        {
            _process.StandardInput.WriteLine(bytes); _process.StandardInput.Flush();
            if (_process.StandardOutput.ReadLine() is not { } response || !response.StartsWith("OK ", StringComparison.Ordinal)) throw new InvalidOperationException("Native source failed.");
        }
        public void Dispose()
        {
            if (_closed) return;
            _closed = true;
            _process.StandardInput.WriteLine("quit"); _process.StandardInput.Flush();
            if (!_process.WaitForExit(5000)) { _process.Kill(); throw new InvalidOperationException("Native source failed to exit."); }
            Check(_process.ExitCode == 0, "Native source closed its client and parser.");
            _process.Dispose();
        }
    }
}
