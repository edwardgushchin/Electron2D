using System.Runtime.InteropServices;
using Electron2D;

internal static partial class MIDIInputTests
{
    internal static void WindowsEmpty()
    {
        OS.CloseMIDIInputs();
        Check(OS.GetConnectedMIDIInputs().Length == 0, "No names before opening.");
        OS.OpenMIDIInputs();
        try
        {
            Check(OS.GetConnectedMIDIInputs().Length == 0, "The WinMM host has no input endpoints.");
            OS.OpenMIDIInputs();
            Check(OS.GetConnectedMIDIInputs().Length == 0, "Opening an active empty session is repeat-safe.");
        }
        finally { OS.CloseMIDIInputs(); }
        OS.CloseMIDIInputs();
        Check(OS.GetConnectedMIDIInputs().Length == 0, "Closing an empty WinMM session is repeat-safe.");
        Console.WriteLine($"MIDI WinMM system empty PASS {_checks}");
    }

    internal static void WinMMFixture(string fixturePath)
    {
        const uint data = 0x3C3;
        const uint moreData = 0x3CC;
        const uint error = 0x3C5;
        const uint closed = 0x3C2;

        var fixture = NativeLibrary.Load(fixturePath);
        var send = Export<Send>("fixture_send");
        var sources = Export<SetValue>("fixture_sources");
        var failStart = Export<SetValue>("fixture_fail_start");
        var setInterface = Export<SetValue>("fixture_interface");
        var openCount = Export<GetValue>("fixture_open_count");

        failStart(1);
        Reject<IOException>(OS.OpenMIDIInputs);
        Check(openCount() == 0 && OS.GetConnectedMIDIInputs().Length == 0, "Failed startup of the second port retires both acquired handles.");
        failStart(0);
        setInterface(0);
        Reject<IOException>(OS.OpenMIDIInputs);
        Check(openCount() == 0, "A source already disabled during opening invalidates the acquisition.");
        setInterface(1);
        OS.OpenMIDIInputs();
        OS.OpenMIDIInputs();
        var names = OS.GetConnectedMIDIInputs();
        Check(openCount() == 2 && names.SequenceEqual(["Fixture A", "Fixture B Ω"]), "Opening snapshots two WinMM sources with Unicode names.");
        names[0] = "changed";
        Check(OS.GetConnectedMIDIInputs()[0] == "Fixture A", "Names are copied.");

        using var root = new Node();
        var probe = new Probe { InputEnabled = true, UnhandledInputEnabled = true, ProcessEnabled = true, PhysicsProcessEnabled = true };
        root.AddChild(probe);
        using var tree = new SceneTree(root);
        Engine.Start(tree);
        try
        {
            Task.Run(() =>
            {
                send(0, data, 0x7F009F);
                send(0, moreData, 0x007F9F);
                send(0, data, 0x7F7F8F);
                send(0, data, 0x7F7FAF);
                send(0, data, 0x7F7FBF);
                send(0, data, 0x007FCF);
                send(0, data, 0x007FDF);
                send(0, data, 0x0000EF);
                send(0, data, 0x7F7FEF);
                send(1, data, 0x643C90);
            }).GetAwaiter().GetResult();

            Check(probe.Events.Count == 0, "Native callbacks queue input without invoking scene code.");
            probe.Callback = () => { probe.Callback = null; Reject<InvalidOperationException>(Input.FlushBufferedEvents); };
            Engine.AdvanceFrame(1d / 30);
            var events = probe.Events;
            Check(events.Count == 10 && probe.Unhandled == 10 && probe.ProcessEventCount == 10 && probe.PhysicsEventCount == 10,
                "Both input stages receive every queued event before frame callbacks.");
            Check(events[0].Device == 0 && events[0].Channel == 15 && events[0].Pitch == 0 && events[0].Velocity == 127, "Channel and note boundaries survive packing.");
            Check(events[1].Message == MIDIMessage.NoteOn && events[1].Pitch == 127 && events[1].Velocity == 0, "Driver backlog retains a zero-velocity note-on.");
            Check(events[2].Message == MIDIMessage.NoteOff && events[2].Velocity == 127 && events[3].Pressure == 127, "Release velocity and key pressure survive packing.");
            Check(events[4].ControllerNumber == 127 && events[4].ControllerValue == 127 && events[5].Instrument == 127 && events[6].Pressure == 127,
                "Controller, program and channel-pressure values use the corresponding event properties.");
            Check(events[7].Pitch == 0 && events[8].Pitch == 16383 && events[9].Device == 1 && events[9].Channel == 0, "Pitch bend and distinct device identity survive packing.");

            Clear(probe);
            foreach (var status in new uint[] { 0xF0, 0xF1, 0xF2, 0xF3, 0xF6, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFE, 0xFF })
                send(0, data, status);
            Input.FlushBufferedEvents();
            Check(events.Select(e => e.Message).SequenceEqual([MIDIMessage.QuarterFrame, MIDIMessage.SongPositionPointer, MIDIMessage.SongSelect,
                MIDIMessage.TuneRequest, MIDIMessage.TimingClock, MIDIMessage.Start, MIDIMessage.Continue, MIDIMessage.Stop,
                MIDIMessage.ActiveSensing, MIDIMessage.SystemReset]) && events.All(e => e.Channel == 0),
                "System statuses retain packet order and use channel zero.");
            Clear(probe);
            send(0, data, 0x643C90);
            send(1, data, 0x643D90);
            probe.Callback = () => { probe.Callback = null; OS.CloseMIDIInputs(); OS.OpenMIDIInputs(); };
            Input.FlushBufferedEvents();
            Check(events.Count == 1 && openCount() == 2, "Close and reopen during delivery drops all remaining old-session data.");
            Clear(probe);
            send(2, data, 0x643D90);
            send(0, data, 0x643E90);
            Reject<InvalidOperationException>(() => Task.Run(Input.FlushBufferedEvents).GetAwaiter().GetResult());
            Input.FlushBufferedEvents();
            Check(events.Count == 1 && events[0].Pitch == 62, "A retired driver callback cannot send into the new session.");

            Clear(probe);
            send(1, data, 0x643C90);
            setInterface(0);
            Reject<IOException>(Input.FlushBufferedEvents);
            Check(events.Count == 0 && openCount() == 0, "PnP interface removal retires every source before queued input is delivered.");
            Reject<IOException>(() => OS.GetConnectedMIDIInputs());
            sources(1);
            OS.OpenMIDIInputs();
            Check(OS.GetConnectedMIDIInputs().SequenceEqual(["Fixture A"]), "Explicit reopen enumerates remaining devices.");

            send(0, closed, 0);
            Reject<IOException>(Input.FlushBufferedEvents);
            OS.OpenMIDIInputs();
            send(0, error, 0);
            Reject<IOException>(Input.FlushBufferedEvents);
            OS.OpenMIDIInputs();
            send(0, data, 0x648090);
            Reject<IOException>(Input.FlushBufferedEvents);
            OS.OpenMIDIInputs();
            for (var i = 0; i < 4097; i++) send(0, data, 0x643C90);
            Reject<IOException>(Input.FlushBufferedEvents);
            Check(events.Count == 0 && openCount() == 0, "Input loss invalidates the session before a partial batch reaches the scene.");

            OS.OpenMIDIInputs();
            send(0, data, 0x643C90);
            probe.Callback = () => throw new ApplicationException("MIDI callback");
            Reject<AggregateException>(() => Engine.AdvanceFrame(0));
        }
        finally
        {
            try { Engine.Stop(); }
            finally { Clear(probe); OS.CloseMIDIInputs(); }
        }

        Check(openCount() == 0 && OS.GetConnectedMIDIInputs().Length == 0, "Engine.Stop retires native input after a throwing callback.");
        using var failure = new FailingLoop();
        Engine.Start(failure);
        OS.OpenMIDIInputs();
        Reject<ApplicationException>(Engine.Stop);
        Check(openCount() == 0, "A failing finalization callback still closes native input.");
        using var failedStart = new FailingStart();
        Reject<ApplicationException>(() => Engine.Start(failedStart));
        Check(openCount() == 0, "Failed engine initialization closes native input opened by the callback.");
        Console.WriteLine($"MIDI WinMM fixture PASS {_checks}");

        T Export<T>(string name) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(fixture, name));
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Send(uint device, uint message, uint packed);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetValue(uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint GetValue();
}
