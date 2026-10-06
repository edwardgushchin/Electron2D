# AudioBusLayout

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.AudioBusLayout`. **Source:** [AudioBusLayout.cs](../../src/Scene/Resources/AudioBusLayout.cs).

**Inherits:** [Resource](Resource.md). **Component:** [Audio playback](../components/audio-playback.md#saved-bus-layouts).

## Description

Stores independent bus/effect configuration containers with borrowed effect resources. A new resource contains one Master bus at unity gain, with solo/mute/bypass false and no effects. GenerateBusLayout captures the live graph; SetBusLayout applies it. Resource archives retain ordered names/sends, gain, flags and effect/enabled pairs. No native voices, processing history or active playback enters a layout.

Ordinary generated layouts borrow caller-owned effect resources. Shallow copies share those identities; deep copies follow Resource graph policy and preserve aliases. Loaded file roots own their internal dependency graph, while server application and generated/shallow snapshots retain its lifetime independently. Disposing the file root therefore cannot invalidate the applied graph. External dependencies remain borrowed. Explicit disposal of an individual borrowed effect still invalidates its use.

Typed stored descriptors represent BusCount, per-bus Name/Solo/Mute/Bypass/VolumeDB/Send/EffectCount and per-effect Resource/Enabled. Count fields precede their dependent schemas during archive loading. Authoring/copy/archive work allocates and uses independent configuration storage; resource operations serialize state. AudioServer application requires its owner and rejects audio callback reentrancy. Changed is not raised by descriptor writes, matching this storage resource's silent configuration behavior.

## Example

Run on the audio owner with an existing directory. The complete producer/fresh-process consumer and public Window host are executable in AudioBusLayoutTests.

```csharp
using var layout = AudioServer.GenerateBusLayout();
ResourceSaver.Save(layout, "res://default_bus_layout.e2dres");
using var loaded = ResourceLoader.Load<AudioBusLayout>("res://default_bus_layout.e2dres");
AudioServer.SetBusLayout(loaded);
```

The AudioBusesDefaultBusLayout project setting defaults to that optional path. Engine.Start and Engine.Run load it before main-loop initialization/autoplay. Empty or absent files preserve configuration; wrong-type files are ignored, while corrupt or invalid layouts report failure. Active feature overrides apply.

## API summary

| Complete C# signature | Contract |
| --- | --- |
| `public AudioBusLayout()` | Creates the one-bus default. |
| `protected override Resource CreateDuplicateInstance()` | Creates independent empty configuration storage. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies bus containers and applies Resource graph copying to effects. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds typed stored configuration fields after base metadata. |
| `protected override void Dispose(bool disposing)` | Releases configuration references without disposing borrowed effects. |

## Constructor Descriptions

### AudioBusLayout

Creates one Master bus with zero dB, false flags, an empty send name and no effects. Applying normalizes its Master send to the server's Master convention. Inherited Resource metadata starts at its normal defaults.

## Method Descriptions

### CreateDuplicateInstance

Returns a caller-owned AudioBusLayout for inherited copy operations. No native device or playback is created.

### CopyCustomStateTo

The target is the duplicate layout. The deep/subresourceMode and duplicateSubresource/forceDuplicateSubresource callbacks are supplied by Resource's copy session. Independent bus arrays and effect slots retain configuration; duplicateSubresource handles each borrowed AudioEffect according to that session and preserves repeated identities. Native processing state is absent. Source/target disposal and callback failures follow the inherited Resource contract.

### GetPropertyDescriptors

Returns inherited metadata and writable typed stored fields in count-before-dependent order. BusCount accepts zero through 255; each EffectCount accepts zero through 65,536. Empty layouts can be stored but SetBusLayout rejects them. String fields are nonnull, and VolumeDB accepts a finite multiplier or negative infinity. Indexed null effect slots have false Enabled initially and are skipped during application. Layout application separately validates names and resource lifetimes. Discovery is cold allocating work.

### Dispose

Clears borrowed references under the resource gate, then releases inherited file ownership. Effect resources created outside the file graph are never disposed. The final internal graph owner attempts every dependency cleanup and reports collected errors through Resource's normal lifecycle.

## Verification and limits

AudioBusLayoutTests exercises all 27 concrete effect archive schemas, endian/compression/bundling, aliases/copies, cache replacement/corruption, fresh-process default load before initialization, file lifetime, maximum count, invalid/disposed values, owner/reentrancy and factory/cleanup/notification failure. Native FAudio replacement checks active/paused and Sample PCM, playback retention and 64 warmed post-edit passes without measured managed bytes/native allocator calls. Two public Window lifecycles execute on each current Linux Wayland GPU/compatibility renderer. Other targets, physical listening, native graph failure injection and editor bus authoring remain separate verification gates.

Application details and exceptions are documented on [AudioServer](AudioServer.md#bus-layout-methods). See ADRs [0013](../decisions/resources.md#adr-0013), [0047](../decisions/audio.md#adr-0047) and [0090](../decisions/agent-native.md#adr-0090).
