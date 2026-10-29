if (args is ["managed"]) MIDIInputTests.Managed();
else if (args is ["denied"]) MIDIInputTests.Unavailable(denied: true);
else if (args is ["unavailable"]) MIDIInputTests.Unavailable();
else if (args is [var fixture]) MIDIInputTests.Native(fixture);
else throw new ArgumentException("Supply managed, unavailable, or an absolute native MIDI source executable path.");
