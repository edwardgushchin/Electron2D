# AudioEffectRecordInstance

Last updated: 2026-10-03

**Declaration:** `internal sealed class Electron2D.AudioEffectRecordInstance` · **Source:** [AudioEffectRecord.cs](../../src/Scene/Resources/AudioEffectRecord.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectInstance](AudioEffectInstance.md).

## Description

The resource's internal stereo processor passes source frames through unchanged, including silent blocks. Only an attached front-pair instance becomes the bus recorder. A prepared capture ring receives whole callback blocks; a worker drains them to accumulated interleaved float PCM, and resource queries encode copied snapshots on the caller thread. Start clears data and prior failure; stop joins the worker and drains pending frames. Ring overrun latches failure without interrupting pass-through output. Native attachment, failed-chain rollback and output teardown control which instance is current. The instance is not public application API.

## Verification and limits

[AudioRecordTests](../../tests/Electron2D.Tests/AudioRecordTests.cs) exercises standalone processing, native front-pair selection, output closure, rollback and allocation. Worker memory grows with recording duration; only the callback ring is fixed-size. See [AudioEffectRecord](AudioEffectRecord.md) and [ADR 0047](../decisions/audio.md#adr-0047).
