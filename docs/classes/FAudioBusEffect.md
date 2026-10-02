# FAudioBusEffect

Last updated: 2026-10-03

**Declaration:** internal sealed unsafe SafeHandle · **Source:** [FAudioBusEffect.cs](../../src/Servers/Audio/FAudioBusEffect.cs).

Owns one native FAPOBase block, inline persistent registration properties and a GCHandle retaining private managed state. The pinned native base supplies format validation, registration copying and reference lifetime; engine-owned Cdecl Process/Destructor callbacks bridge bounded stereo processing. Voice descriptors retain a native reference until detached. SafeHandle release drops the managed native reference; final native destruction frees the GCHandle and allocated block. Native callbacks never invoke managed disposal hooks.

Public-effect state borrows its resource, owns per-pair instances and prepares two quantum-sized stereo buffers. Each instance is told its pair index when attached, allowing the recorder resource to select pair zero without promoting rear-pair state. Processing deinterleaves each native pair, handles activity/silence policy, invokes finite span processing, interleaves output and contains errors. Disabled processing passes PCM through. Failed effects silence their complete block and report once on the owner until recreation. Gain state needs no stereo scratch and runs after public effects; it validates finite output, updates shared activity and precedes the native meter.

Nested Activity owns per-stereo-pair active/used flags, last-used/mixed frame stamps and the prepared timeout/threshold. Each actual quantum resets usage, sources mark their routed pairs, upstream active sends mark downstream pairs, and final gain updates the post-volume threshold. Unused silent pairs expire only after the timeout; silent-source usage and effects producing tails remain active. Routing edits preserve the shared activity object. Output closure recreates it from typed project settings.

Native voice destruction precedes SafeHandle and instance cleanup. Every managed instance is detached and disposed on the audio owner, collecting custom cleanup errors. The server prevents configuration/Lock/Unlock reentrancy from factories, processing and disposal hooks. This helper and its ABI never enter public signatures.

AudioEffectTests checks pinned Linux x64 C/managed FAPOBase size/offsets, interleaved 2/4/6/8-channel callback pairs, actual FAudio ordering, gain/capture/meters, source/tail activity, errors and warmed allocations. Actual multichannel devices, physical listening and foreign ABIs remain unverified. See [audio component](../components/audio-playback.md#bus-effects).
