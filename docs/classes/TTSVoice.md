# TTSVoice

Last updated: 2026-11-06

**Declaration:** public readonly struct TTSVoice

**Source:** src/Servers/Display/TTSVoice.cs

Immutable native speech metadata returned in independent DisplayServer.TTSGetVoices arrays. ID is the exact installed identifier accepted by TTSSpeak; Name is native display text and Language is the ISO language/variant code. A default value returns empty strings. Descriptor values contain no native pointer, resource lifetime or mutable producer storage.

See [native speech](../components/text-to-speech.md) for filtering, configuration, queue behavior, actual Windows SDK/PCM verification and remaining platform limits.
