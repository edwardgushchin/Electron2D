# MovieWriter

Last updated: 2026-10-15

Source: [MovieWriter.cs](../../src/Servers/Movie/MovieWriter.cs). Declaration: `public abstract class MovieWriter : ElectronObject`. Inherits [ElectronObject](ElectronObject.md). Internal implementation: [PNGMovieWriter](PNGMovieWriter.md).

## Description

Encodes completed non-real-time frames and interleaved full-range signed 32-bit PCM. Register a borrowed custom instance before runtime startup. Later matching registrations override earlier ones. The creating thread registers/disposes the instance; selection and recording hooks execute serially on the runtime owner. Registered writers cannot be disposed while a runtime is reserved. The registry retains at most eight live custom instances and reclaims disposed entries on the next AddWriter.

Frame Images and audio spans live only through OnWriteFrame; copy any retained output. Hook exceptions fail the recording. End is attempted even after partial Begin failure, and does not prevent other cleanup. Readback/encoding may allocate and block. See [movie writing](../components/movie-writing.md) for clocks, native graph ownership and supported built-in output.

## Example

The built-in writer needs no custom subclass. In a normal main-thread application entry point:

```csharp
var window = new Window { Size = new Vector2i(640, 480) };
window.AddChild(new RecordingScene()); // caller's Node; quits through Tree.Quit()
int exitCode = Engine.Run(window, "user://capture.png", fps: 60);
```

The destination directory must exist. To encode a custom format, derive from MovieWriter and implement all five abstract hooks below, then call MovieWriter.AddWriter before Engine.Run. Mix-rate and speaker-mode hooks have defaults.

## Constructors and methods

| Signature | Contract |
| --- | --- |
| `protected MovieWriter()` | Creates an unregistered instance. |
| `public static void AddWriter(MovieWriter writer)` | Borrows a live creating-thread instance before startup; newest matching handler wins. |
| `protected virtual int OnGetAudioMixRate()` | Defaults to 48000 Hz; valid range 1000..200000. |
| `protected virtual AudioServer.SpeakerMode OnGetAudioSpeakerMode()` | Defaults to Stereo; all four enum values execute. |
| `protected abstract string[] OnGetSupportedExtensions()` | Returns nonnull bare extensions during registration. |
| `protected abstract bool OnHandlesFile(string path)` | Tests the unchanged requested path. |
| `protected abstract void OnWriteBegin(Vector2i movieSize, int fps, string basePath)` | Prepares the output before scene activation. |
| `protected abstract void OnWriteFrame(Image frameImage, ReadOnlySpan<int> audioFrameBlock)` | Writes a borrowed RGB8 frame and interleaved PCM. |
| `protected abstract void OnWriteEnd()` | Finalizes normal or partial output before scene teardown. |
| `protected override void ValidateDisposal()` | Rejects active/off-owner disposal before terminal state. |

## Method descriptions

<a id="addwriter"></a>
### AddWriter

Rejects null/disposed, off-creator-thread, duplicate/full registrations, active runtime and hook reentry. Calls extension discovery before publication; a callback or invalid extension preserves registration state. Extensions must be unique case-insensitive nonempty bare suffixes containing ASCII letters/digits/underscore/hyphen. Empty lists are allowed because OnHandlesFile owns actual selection. Disposed registrations are ignored during selection and reclaimed at later registration. Caller retains disposal ownership.

<a id="ongetaudiomixrate"></a>
<a id="ongetaudiospeakermode"></a>
<a id="audiometadata"></a>
### OnGetAudioMixRate / OnGetAudioSpeakerMode

Sampled once for a selected recording, before Begin. Invalid rate/enum fails before Begin/End. The native output graph uses exactly this rate and 2/4/6/8 channels in AudioServer order. The built-in writer samples MovieWriterMixRate and MovieWriterSpeakerMode project settings instead of custom defaults.

<a id="ongetsupportedextensions"></a>
<a id="onhandlesfile"></a>
<a id="selection"></a>
### OnGetSupportedExtensions / OnHandlesFile

Extension discovery executes on the registration thread. Selection executes on the runtime owner after reservation; exceptions abort instead of silently choosing another handler. Test case/extension rules in OnHandlesFile as appropriate. Registration and active disposal cannot reenter either callback.

<a id="onwritebegin"></a>
<a id="begin"></a>
### OnWriteBegin

Receives fixed positive pixel dimensions, positive FPS and the unchanged path. Native render/audio preparation precedes the hook; scene activation follows it. Throw to abort. End is still attempted, so partial output state must tolerate finalization. A selection/metadata failure never invokes this pair.

<a id="onwriteframe"></a>
<a id="frame"></a>
### OnWriteFrame

Runs after actual rendering/readback and synchronous native audio mixing. Images are opaque RGB8 at the initial size; later viewport resizes are centered-cover cropped/resized. The audio length is channels times the difference between consecutive cumulative floor(frameCount*rate/FPS) values, preventing non-divisible-rate drift. Values span Int32.MinValue through Int32.MaxValue with finite output clamped to PCM range. Data is borrowed; callbacks may perform cold encoding/file work. No fake headless image or realtime game-recording promise is provided.

<a id="onwriteend"></a>
<a id="validatedisposal"></a>
<a id="end"></a>
### OnWriteEnd / ValidateDisposal

End runs once after Begin was attempted, including startup/callback failure, before scene and native cleanup. Its exception is reported alongside any other failure and the lease still releases. ValidateDisposal rejects callbacks, selected recordings, any reserved runtime for a registered instance and creating-thread violations; inherited idempotent disposal remains applicable. A completed run does not dispose a borrowed custom writer.

## Verification and remaining work

MovieWriterTests exercises portable registration and actual Linux GPU/Compatibility recording. [The component](../components/movie-writing.md) records the exact checks and package/foreign limits. Built-in AVI/MJPEG and OGV encoders remain in coverage; they are not provided by the generic hooks or the PNG/WAV backend. [ADR 0109](../decisions/movie-writing.md#adr-0109).
