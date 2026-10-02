# AudioEffectInstance

Last updated: 2026-10-03

**Declaration:** `public abstract class Electron2D.AudioEffectInstance` · **Source:** [AudioEffect.cs](../../src/Scene/Resources/AudioEffect.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [ElectronObject](ElectronObject.md). Internal concrete state includes [AudioEffectAmplifyInstance](AudioEffectAmplifyInstance.md), [AudioEffectPannerInstance](AudioEffectPannerInstance.md), [AudioEffectEQInstance](AudioEffectEQInstance.md), [AudioEffectCaptureInstance](AudioEffectCaptureInstance.md), [AudioEffectFilterInstance](AudioEffectFilterInstance.md) and [AudioEffectHardLimiterInstance](AudioEffectHardLimiterInstance.md).

## Description

Independent stereo DSP state. A standalone instance belongs to its caller; a bus instance returned by AudioServer is borrowed. Native attachment identifies the stereo pair internally; [AudioEffectRecord](AudioEffectRecord.md) uses this to make only the front pair current. Structural effect edits and output closure dispose those borrowed handles. Source resources remain borrowed. Per-instance processing serializes through a separate gate and rejects recursive processing/disposal. Bus callbacks also reject server configuration and Lock/Unlock. User hooks must use prepared storage, synchronize custom fields and produce finite PCM; amplitudes beyond unity are preserved until downstream output processing.

## Example

See the complete custom extension in [AudioEffect](AudioEffect.md#example). Standalone processing uses caller-provided spans:

```csharp
using var capture = new AudioEffectCapture();
using var instance = capture.Instantiate();
Vector2[] source = [new(.2f, -.3f)];
Vector2[] destination = new Vector2[source.Length];
instance.Process(source, destination);
```

## API summary

| Signature | Contract |
| --- | --- |
| `protected AudioEffectInstance()` | Initializes independent state. |
| `public void Process(ReadOnlySpan<Vector2> source, Span<Vector2> destination)` | Processes a standalone complete block. |
| `public bool ProcessSilence()` | Queries the concrete silence policy. |
| `protected abstract void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)` | Writes every output frame. |
| `protected virtual bool OnProcessSilence()` | False by default. |
| `protected override void ValidateDisposal()` | Rejects active/attached disposal before logical state changes. |

## Method Descriptions

<a id="process"></a>
### Process and OnProcess

Input and output have identical frame counts and may alias. Nonfinite input, mismatched lengths or nonfinite hook output throw ArgumentException. Empty blocks are valid. OnProcess must overwrite every destination frame; do not retain spans. Processing a bus-owned instance directly or recursively throws InvalidOperationException. Disposed instances throw ObjectDisposedException. Standalone hook failures propagate and release the processing gate.

The bus path deinterleaves each output stereo pair into prepared buffers, invokes the same contract and interleaves the result. It contains hook exceptions at the native boundary, clears the entire failed effect block and continues to output silence through that effect until structural recreation. The next owner SceneTree frame reports collected errors once while still running ordinary frame callbacks. Later downstream effects continue processing the silence.

<a id="processsilence"></a>
### ProcessSilence and OnProcessSilence

OnProcessSilence returns false initially. True opts into processing inactive silent buses, including muted buses before their final gain. Active buses still process silent blocks to preserve effect tails. Prepared bus activity follows source usage, upstream sends and post-gain threshold/timeout settings. Capture returns true. Hook exceptions use the same native containment/reporting behavior.

<a id="validatedisposal"></a>
### ValidateDisposal

Attached or currently processing instances reject public Dispose with InvalidOperationException. Native voices detach before server-owned disposal. Cleanup attempts every instance even when custom disposal hooks throw; disposed old handles remain invalid and configuration commits are retained. Ordinary inherited identity/notifications follow ElectronObject.

## Verification and limits

AudioEffectTests verifies finite PCM, aliased spans, reentrancy, custom callback errors, independent 2/4/6/8-channel FAPO pair projection, real native ordering and shared ring concurrency. Native Linux x64 execution and warmed allocation checks are recorded in the [component](../components/audio-playback.md#bus-effects). Actual multichannel devices, physical listening and other platforms remain unverified.
