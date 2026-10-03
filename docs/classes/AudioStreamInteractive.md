# AudioStreamInteractive

Last updated: 2026-10-03

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AudioStreamInteractive` · **Source:** [AudioStreamInteractive.cs](../../src/Scene/Resources/AudioStreamInteractive.cs).

**Inherits:** [AudioStream](AudioStream.md).

## Description

Borrows up to 63 child streams and stores names, automatic progression and ordered transition rules. Configure slots 0–62 independently of ClipCount; only its active prefix participates in prepared playback/copies. Names are literal strings (duplicates choose the first active slot); ClipAny is -1 and GetClipName(-1) returns All Clips. InitialClip starts at zero and assigning requires an active index. Empty/null initial configuration starts safely inactive. Stream name is Transitioner, meta and inherited monophonic policy are true, and length/tempo/loop metadata retain zero/false defaults.

Exact queries/erase raise KeyNotFoundException for absent rules; runtime rule resolution separately chooses exact pair, source/Any, Any/destination, Any/Any, then NextBeat/Start/Automatic/one beat. Fade beats are finite and nonnegative, seconds without source BPM; zero is instantaneous. Literal next/filler indices may be unusable and are ignored during playback. Count shrink sanitizes initial/advance/filler/endpoints while retaining hidden assignments. Count always notifies property and parameter lists after commit (both attempt even if an observer fails); advance mode notifies the property list. Other setters do not emit Changed or implicit clip names.

Count/stream writes serialize with mixing and invalidate a version, including equal stream writes. Active playback stops at its next mix; owner restart prepares the new prefix, reuses unchanged resource/playback identities and closes removed ownership. Null-to-stream writes invalidate too. Authoring snapshots use the shared graph gate, and names/rules/progression affect later queue operations without reconfiguring an already queued transition. Factories and child callbacks cannot reenter authoring. Mixed interactive/randomizer/synchronized cycles and disposed assigned resources reject.

GetParameterList returns the typed SwitchToClipParameter. It uses exact strings and fits existing player/emitter typed parameter/scene storage. Stored active-prefix shallow/deep/local copies preserve rules and resource aliases through Resource graph policy; hidden assignments, devices and playback state are omitted. Disposal invalidates borrowed playbacks without disposing child resources. General usage/sample/editor/file-persistence dependencies remain separate. See [interactive streams](../components/audio-playback.md#interactive-streams) for execution and explicit lifecycle corrections.

## Example

This snippet requires the named live AudioStream resources and the usual engine/scene host.

```csharp
using var interactive = new AudioStreamInteractive { ClipCount = 2 };
interactive.SetClipName(0, "explore");
interactive.SetClipName(1, "combat");
interactive.SetClipStream(0, explorationMusic);
interactive.SetClipStream(1, combatMusic);
interactive.AddTransition(0, 1, AudioStreamInteractive.TransitionFromTime.NextBar,
    AudioStreamInteractive.TransitionToTime.Start, AudioStreamInteractive.FadeMode.Cross, 1);
var player = new AudioStreamPlayer { Stream = interactive };
// Attach to a Window/SceneTree before Play.
player.SetParameter(AudioStreamPlaybackInteractive.SwitchToClipParameter, "combat");
```

## Constructors

| Complete signature | Contract |
| --- | --- |
| `public AudioStreamInteractive()` | Creates an empty resource with initial index zero. |

## Constructors descriptions

<a id="member-981895aa2801"></a>
### .ctor

`public AudioStreamInteractive()`

Creates an empty resource with initial index zero.

## Constants

| Complete signature | Contract |
| --- | --- |
| `public const System.Int32 ClipAny = -1` | Matches every source or destination clip in a transition rule. |

## Constants descriptions

<a id="member-351c86e6fd2f"></a>
### ClipAny

`public const System.Int32 ClipAny = -1`

Matches every source or destination clip in a transition rule.

## Properties

| Complete signature | Contract |
| --- | --- |
| `public System.Int32 ClipCount { get; set; }` | Gets or sets the active clip prefix. |
| `public System.Int32 InitialClip { get; set; }` | Gets or sets the initial active clip index. |

## Properties descriptions

<a id="member-1dcdf299a124"></a>
### ClipCount

`public System.Int32 ClipCount { get; set; }`

Gets or sets the active clip prefix.

Value: Zero initially; zero through 63. Hidden clip configuration is retained on this resource.

Remarks: Changed count invalidates playback caches. Every write notifies property and parameter lists. Shrinking resets out-of-prefix initial/advance/filler targets and removes transition endpoints outside the prefix.

System.ArgumentOutOfRangeException: The count is outside capacity.

System.InvalidOperationException: A factory reenters editing.

<a id="member-3cd9ec875825"></a>
### InitialClip

`public System.Int32 InitialClip { get; set; }`

Gets or sets the initial active clip index.

Value: Zero initially; assigning requires an index in ClipCount.

System.ArgumentOutOfRangeException: The index is outside the active prefix.

## Methods and protected hooks

| Complete signature | Contract |
| --- | --- |
| `public System.Void AddTransition(System.Int32 fromClip, System.Int32 toClip, Electron2D.AudioStreamInteractive.TransitionFromTime fromTime, Electron2D.AudioStreamInteractive.TransitionToTime toTime, Electron2D.AudioStreamInteractive.FadeMode fadeMode, System.Single fadeBeats, System.Boolean useFillerClip = false, System.Int32 fillerClip = -1, System.Boolean holdPrevious = false)` | Adds or replaces a transition in insertion order. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Concrete inherited hook; see Description and the base-class contract. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Concrete inherited hook; see Description and the base-class contract. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Concrete inherited hook; see Description and the base-class contract. |
| `public System.Void EraseTransition(System.Int32 fromClip, System.Int32 toClip)` | Erases an exact rule. |
| `public Electron2D.AudioStreamInteractive.AutoAdvanceMode GetClipAutoAdvance(System.Int32 clipIndex)` | Gets a stored progression policy. |
| `public System.Int32 GetClipAutoAdvanceNextClip(System.Int32 clipIndex)` | Gets a literal next-clip target. |
| `public System.String GetClipName(System.Int32 clipIndex)` | Gets a clip identifier or the wildcard label. |
| `public Electron2D.AudioStream GetClipStream(System.Int32 clipIndex)` | Gets a borrowed clip stream. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Concrete inherited hook; see Description and the base-class contract. |
| `public System.Single GetTransitionFadeBeats(System.Int32 fromClip, System.Int32 toClip)` | Gets an exact rule's fade duration. |
| `public Electron2D.AudioStreamInteractive.FadeMode GetTransitionFadeMode(System.Int32 fromClip, System.Int32 toClip)` | Gets an exact rule's fade policy. |
| `public System.Int32 GetTransitionFillerClip(System.Int32 fromClip, System.Int32 toClip)` | Gets an exact rule's literal filler index. |
| `public Electron2D.AudioStreamInteractive.TransitionFromTime GetTransitionFromTime(System.Int32 fromClip, System.Int32 toClip)` | Gets an exact rule's source timing. |
| `public System.Int32[] GetTransitionList()` | Gets copied interleaved source/destination keys in insertion order. |
| `public Electron2D.AudioStreamInteractive.TransitionToTime GetTransitionToTime(System.Int32 fromClip, System.Int32 toClip)` | Gets an exact rule's destination timing. |
| `public System.Boolean HasTransition(System.Int32 fromClip, System.Int32 toClip)` | Tests exact authored endpoints, including wildcard values. |
| `public override System.Boolean IsMetaStream()` |  |
| `public System.Boolean IsTransitionHoldingPrevious(System.Int32 fromClip, System.Int32 toClip)` | Gets whether an exact rule remembers its source. |
| `public System.Boolean IsTransitionUsingFillerClip(System.Int32 fromClip, System.Int32 toClip)` | Gets whether an exact rule requests a filler. |
| `protected override Electron2D.PropertyDescriptor[] OnGetParameterList()` | Concrete inherited hook; see Description and the base-class contract. |
| `protected override System.String OnGetStreamName()` | Concrete inherited hook; see Description and the base-class contract. |
| `protected override Electron2D.AudioStreamPlayback OnInstantiatePlayback()` | Concrete inherited hook; see Description and the base-class contract. |
| `public System.Void SetClipAutoAdvance(System.Int32 clipIndex, Electron2D.AudioStreamInteractive.AutoAdvanceMode mode)` | Sets the automatic progression policy for future queues and notifies the property list. |
| `public System.Void SetClipAutoAdvanceNextClip(System.Int32 clipIndex, System.Int32 autoAdvanceNextClip)` | Stores a literal next-clip index; invalid or self targets are ignored during playback. |
| `public System.Void SetClipName(System.Int32 clipIndex, System.String name)` | Sets a clip's literal identifier. |
| `public System.Void SetClipStream(System.Int32 clipIndex, Electron2D.AudioStream stream)` | Sets a borrowed clip stream and invalidates the prepared version, including equal writes. |

## Methods and protected hooks descriptions

<a id="member-5b5789339721"></a>
### AddTransition

`public System.Void AddTransition(System.Int32 fromClip, System.Int32 toClip, Electron2D.AudioStreamInteractive.TransitionFromTime fromTime, Electron2D.AudioStreamInteractive.TransitionToTime toTime, Electron2D.AudioStreamInteractive.FadeMode fadeMode, System.Single fadeBeats, System.Boolean useFillerClip = false, System.Int32 fillerClip = -1, System.Boolean holdPrevious = false)`

Adds or replaces a transition in insertion order.

`fromClip`: Active index or ClipAny.

`toClip`: Active index or ClipAny.

`fromTime`: Defined source timing mode.

`toTime`: Defined destination timing mode.

`fadeMode`: Defined fade policy.

`fadeBeats`: Finite nonnegative beats, or seconds without source BPM; zero is instant.

`useFillerClip`: Whether a valid distinct filler precedes the destination.

`fillerClip`: Literal filler index; unusable targets are ignored.

`holdPrevious`: Remember the source for a later ReturnToHold.

System.ArgumentOutOfRangeException: An endpoint, selector or duration is invalid.

<a id="member-105ec8d4d28a"></a>
### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-3d86960e2fca"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-69054f1db40c"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-affa4c10350d"></a>
### EraseTransition

`public System.Void EraseTransition(System.Int32 fromClip, System.Int32 toClip)`

Erases an exact rule.

`fromClip`: Literal source key.

`toClip`: Literal destination key.

System.Collections.Generic.KeyNotFoundException: The rule is absent.

<a id="member-29831e71abd8"></a>
### GetClipAutoAdvance

`public Electron2D.AudioStreamInteractive.AutoAdvanceMode GetClipAutoAdvance(System.Int32 clipIndex)`

Gets a stored progression policy.

Returns: Disabled initially.

`clipIndex`: Zero through 62.

System.ArgumentOutOfRangeException: A slot index or enum selector is invalid.

System.ObjectDisposedException: This resource is disposed.

<a id="member-d3462226070e"></a>
### GetClipAutoAdvanceNextClip

`public System.Int32 GetClipAutoAdvanceNextClip(System.Int32 clipIndex)`

Gets a literal next-clip target.

Returns: The stored target, zero initially.

`clipIndex`: Zero through 62.

System.ArgumentOutOfRangeException: A slot index or enum selector is invalid.

System.ObjectDisposedException: This resource is disposed.

<a id="member-ae77ac5fb736"></a>
### GetClipName

`public System.String GetClipName(System.Int32 clipIndex)`

Gets a clip identifier or the wildcard label.

Returns: The stored name, empty initially, or All Clips.

`clipIndex`: Minus one for All Clips, otherwise zero through 62.

System.ArgumentOutOfRangeException: A slot index or enum selector is invalid.

System.ObjectDisposedException: This resource is disposed.

<a id="member-5f3dfe0c0d13"></a>
### GetClipStream

`public Electron2D.AudioStream GetClipStream(System.Int32 clipIndex)`

Gets a borrowed clip stream.

Returns: Null initially.

`clipIndex`: Zero through 62.

System.ArgumentOutOfRangeException: A slot index or enum selector is invalid.

System.ObjectDisposedException: This resource is disposed.

<a id="member-a0ac99677557"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-51096802b883"></a>
### GetTransitionFadeBeats

`public System.Single GetTransitionFadeBeats(System.Int32 fromClip, System.Int32 toClip)`

Gets an exact rule's fade duration.

Returns: Authored beats or seconds without BPM.

`fromClip`: Literal source key.

`toClip`: Literal destination key.

System.Collections.Generic.KeyNotFoundException: That exact rule is absent.

System.ObjectDisposedException: This resource is disposed.

<a id="member-3816daa0e4f6"></a>
### GetTransitionFadeMode

`public Electron2D.AudioStreamInteractive.FadeMode GetTransitionFadeMode(System.Int32 fromClip, System.Int32 toClip)`

Gets an exact rule's fade policy.

Returns: The authored policy.

`fromClip`: Literal source key.

`toClip`: Literal destination key.

System.Collections.Generic.KeyNotFoundException: That exact rule is absent.

System.ObjectDisposedException: This resource is disposed.

<a id="member-f1a480baf3ad"></a>
### GetTransitionFillerClip

`public System.Int32 GetTransitionFillerClip(System.Int32 fromClip, System.Int32 toClip)`

Gets an exact rule's literal filler index.

Returns: The authored index.

`fromClip`: Literal source key.

`toClip`: Literal destination key.

System.Collections.Generic.KeyNotFoundException: That exact rule is absent.

System.ObjectDisposedException: This resource is disposed.

<a id="member-f31fa75fa664"></a>
### GetTransitionFromTime

`public Electron2D.AudioStreamInteractive.TransitionFromTime GetTransitionFromTime(System.Int32 fromClip, System.Int32 toClip)`

Gets an exact rule's source timing.

Returns: The authored timing.

`fromClip`: Literal source key.

`toClip`: Literal destination key.

System.Collections.Generic.KeyNotFoundException: That exact rule is absent.

System.ObjectDisposedException: This resource is disposed.

<a id="member-8d186f7e79e3"></a>
### GetTransitionList

`public System.Int32[] GetTransitionList()`

Gets copied interleaved source/destination keys in insertion order.

Returns: Two signed integers per rule.

<a id="member-d9171d4742db"></a>
### GetTransitionToTime

`public Electron2D.AudioStreamInteractive.TransitionToTime GetTransitionToTime(System.Int32 fromClip, System.Int32 toClip)`

Gets an exact rule's destination timing.

Returns: The authored timing.

`fromClip`: Literal source key.

`toClip`: Literal destination key.

System.Collections.Generic.KeyNotFoundException: That exact rule is absent.

System.ObjectDisposedException: This resource is disposed.

<a id="member-8c63c9094245"></a>
### HasTransition

`public System.Boolean HasTransition(System.Int32 fromClip, System.Int32 toClip)`

Tests exact authored endpoints, including wildcard values.

Returns: True when that exact rule exists.

`fromClip`: Literal source key.

`toClip`: Literal destination key.

<a id="member-616b35dd4ca7"></a>
### IsMetaStream

`public override System.Boolean IsMetaStream()`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-516372d8cfa6"></a>
### IsTransitionHoldingPrevious

`public System.Boolean IsTransitionHoldingPrevious(System.Int32 fromClip, System.Int32 toClip)`

Gets whether an exact rule remembers its source.

Returns: The authored flag.

`fromClip`: Literal source key.

`toClip`: Literal destination key.

System.Collections.Generic.KeyNotFoundException: That exact rule is absent.

System.ObjectDisposedException: This resource is disposed.

<a id="member-d4f24b664e6e"></a>
### IsTransitionUsingFillerClip

`public System.Boolean IsTransitionUsingFillerClip(System.Int32 fromClip, System.Int32 toClip)`

Gets whether an exact rule requests a filler.

Returns: The authored flag.

`fromClip`: Literal source key.

`toClip`: Literal destination key.

System.Collections.Generic.KeyNotFoundException: That exact rule is absent.

System.ObjectDisposedException: This resource is disposed.

<a id="member-b653cbb517be"></a>
### OnGetParameterList

`protected override Electron2D.PropertyDescriptor[] OnGetParameterList()`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-3e0c776b0eb7"></a>
### OnGetStreamName

`protected override System.String OnGetStreamName()`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-fcbde262a82f"></a>
### OnInstantiatePlayback

`protected override Electron2D.AudioStreamPlayback OnInstantiatePlayback()`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-6b13052529bc"></a>
### SetClipAutoAdvance

`public System.Void SetClipAutoAdvance(System.Int32 clipIndex, Electron2D.AudioStreamInteractive.AutoAdvanceMode mode)`

Sets the automatic progression policy for future queues and notifies the property list.

`clipIndex`: Zero through 62.

`mode`: A defined auto-advance mode.

System.ArgumentOutOfRangeException: A slot index or enum selector is invalid.

System.ObjectDisposedException: This resource is disposed.

<a id="member-3fa92c065579"></a>
### SetClipAutoAdvanceNextClip

`public System.Void SetClipAutoAdvanceNextClip(System.Int32 clipIndex, System.Int32 autoAdvanceNextClip)`

Stores a literal next-clip index; invalid or self targets are ignored during playback.

`clipIndex`: Zero through 62.

`autoAdvanceNextClip`: Literal signed target, zero initially.

System.ArgumentOutOfRangeException: A slot index or enum selector is invalid.

System.ObjectDisposedException: This resource is disposed.

<a id="member-3eb90d2582d0"></a>
### SetClipName

`public System.Void SetClipName(System.Int32 clipIndex, System.String name)`

Sets a clip's literal identifier.

`clipIndex`: Zero through 62, including hidden slots.

`name`: Non-null name; duplicates resolve to the first active clip.

System.ArgumentNullException: The name is null.

System.ArgumentOutOfRangeException: The index is outside capacity.

<a id="member-e3fc7c58db7a"></a>
### SetClipStream

`public System.Void SetClipStream(System.Int32 clipIndex, Electron2D.AudioStream stream)`

Sets a borrowed clip stream and invalidates the prepared version, including equal writes.

`clipIndex`: Zero through 62.

`stream`: Borrowed resource or null.

System.InvalidOperationException: A mixed composite cycle or callback reentry is attempted.

System.ObjectDisposedException: This or the assigned stream is disposed.

## Enumerations

| Complete signature | Contract |
| --- | --- |
| `public enum Electron2D.AudioStreamInteractive.TransitionFromTime` | [Source immediate, beat, bar or end timing](AudioStreamInteractive.TransitionFromTime.md) |
| `public enum Electron2D.AudioStreamInteractive.TransitionToTime` | [Destination same, start or remembered cursor](AudioStreamInteractive.TransitionToTime.md) |
| `public enum Electron2D.AudioStreamInteractive.FadeMode` | [Five incoming/outgoing gain policies](AudioStreamInteractive.FadeMode.md) |
| `public enum Electron2D.AudioStreamInteractive.AutoAdvanceMode` | [Disabled, next target or held return](AudioStreamInteractive.AutoAdvanceMode.md) |

## Related enums and lifecycle

- [TransitionFromTime](AudioStreamInteractive.TransitionFromTime.md)
- [TransitionToTime](AudioStreamInteractive.TransitionToTime.md)
- [FadeMode](AudioStreamInteractive.FadeMode.md)
- [AutoAdvanceMode](AudioStreamInteractive.AutoAdvanceMode.md)

The Description specifies state changes, errors, ownership and threading. [AudioInteractiveTests](../../tests/Electron2D.Tests/AudioInteractiveTests.cs) exercises deterministic PCM, lifetime/copies/typed parameters and warmed CPU/native controls. [ADR 0047](../decisions/audio.md#adr-0047) and [the component](../components/audio-playback.md#interactive-streams) record execution and physical/platform limits.
