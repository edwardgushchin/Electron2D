# Native speech consumer

The public-only consumer shares TTSChecks with the root runner. Enable ProjectSettings.AudioGeneralTextToSpeech before opening a display. Build both independent inspectors with the Windows SDK in a Developer PowerShell:

```powershell
$receipts = 'C:/absolute/speech-receipts'
New-Item -ItemType Directory -Force -Path $receipts
cl /nologo /EHsc /O2 /MT /std:c++17 /utf-8 tools/text-to-speech/voices.cpp /Fe:"$receipts/voices.exe" /Fo:"$receipts/voices.obj" sapi.lib ole32.lib
cl /nologo /EHsc /O2 /MT /std:c++17 /utf-8 tools/text-to-speech/loopback.cpp /Fe:"$receipts/loopback.exe" /Fo:"$receipts/loopback.obj" mmdevapi.lib ole32.lib
dotnet run --project tests/TTS.Consumer/TTS.Consumer.csproj -c Release -- $receipts "$receipts/run"
```

The SDK voice inspector compares native IDs, names and locale metadata. Process loopback requires Windows build 20348 or later and captures only the tested consumer process tree, never other applications' audio. Outputs are raw `.pcm` (44.1 kHz, stereo, signed little-endian PCM16), native `.txt` measurements and private ready/stop markers. Each capture uses a fresh stem. The producer speaks through its real system speech output. Tests measure signal, silence within one conversion LSB, buffered pause/resume, real duration and voiced frequency; callbacks alone cannot establish output. Other targets need their own equivalent native capture.

Repeat against a fresh candidate package and strict full-trim NativeAOT with the matching native bundle from [native packaging](../../docs/native-packaging.md). All outputs belong outside the source tree. The `disabled` argument exercises restart/absence controls; Windows SDL_VIDEODRIVER=dummy still permits real speech, testing a different COM initialization path. The root runner accepts ELECTRON2D_TEST_TTS=INSPECTOR_DIRECTORY and its first app argument is the output directory; `disabled` selects only that control. See [the component](../../docs/components/text-to-speech.md) for actual semantics and remaining platform/acoustic limits.
