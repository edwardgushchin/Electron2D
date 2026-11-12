if (args is ["managed"]) MIDIInputTests.Managed();
else if (args is ["denied"]) MIDIInputTests.Unavailable(denied: true);
else if (args is ["unavailable"]) MIDIInputTests.Unavailable();
else if (args is ["windows-empty"]) MIDIInputTests.WindowsEmpty();
#if MIDI_WINMM_FIXTURE_HOST
else if (args is ["winmm-fixture", var library]) WinMMFixtureHost.Run(library);
else if (args is ["winmm-fixture-child", var resolvedLibrary]) MIDIInputTests.WinMMFixture(resolvedLibrary);
#endif
else if (args is [var fixture]) MIDIInputTests.Native(fixture);
else throw new ArgumentException("Supply a documented MIDI input test profile or the absolute ALSA fixture executable path.");
