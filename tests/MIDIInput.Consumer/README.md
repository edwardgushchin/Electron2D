# MIDI input consumer

This public-only consumer shares the focused assertions in `../Electron2D.Tests/MIDIInputTests.cs`.
Build the independent native producer on desktop Linux with system ALSA development headers:

```sh
cc -std=gnu11 -Wall -Wextra -Werror ../Electron2D.Tests/Fixtures/MIDI/source.c -lasound -o /tmp/e2d-midi-source
dotnet run -c Release -- /tmp/e2d-midi-source
dotnet publish -c Release -r linux-x64 -p:PublishAot=true -p:IlcTreatWarningsAsErrors=true -p:TrimmerTreatWarningsAsErrors=true -o /tmp/e2d-midi-aot
/tmp/e2d-midi-aot/MIDIInput.Consumer /tmp/e2d-midi-source
```

`managed` checks event values/resources without opening native input. `unavailable` expects native acquisition to fail, so run it in a child with `ALSA_CONFIG_PATH` set to an empty file. `denied` expects the isolated `Fixtures/MIDI/denied.c` shared-library fault, selected only in that child through `LD_LIBRARY_PATH`. The fixture exports an acquisition failure and never intercepts user input. Source mode requires `/dev/snd/seq` access; it creates two virtual source ports and removes them on exit.

The ordinary native-consumer properties also support `Electron2DUsePackage=true` for validation against a locally qualified package. Source-to-scene checks include callback failure diagnostics by design. See [the component contract](../../docs/components/midi-input.md) for source identity, failure semantics, platform and physical-device limits.
