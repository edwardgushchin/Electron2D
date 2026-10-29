# Animation event endpoint oracle

Pinned source: Godot 4.7.2-stable, commit `ed1daf0bf001b61586d9930840f2f1394092c079`.
Exact input paths and SHA-256 identities are in `sources.json`.

`oracle.cpp` retains the original MIT header and extracts `Animation::_track_get_key_indices_in_range`, the applicable method/audio/animation cases of `track_get_key_indices_in_range`, and the double approximate-equality/ping-pong helpers. Unused 3D/value/Bezier cases are mechanically removed; vector/track storage and the exact-key lookup are test adapters. Endpoint range control flow and key ordering remain the pinned implementation, independently compiled as C++. The emitter tests 3564 combinations of clip lengths, all three loop modes/flags, forward/reverse/zero/large deltas, folded/outside positions and epsilon-adjacent key times.

Run `python3 -B tests/Electron2D.Tests/Fixtures/AnimationEndpoints/check.py` to recompile the oracle and compare `reference.tsv` byte-for-byte. To regenerate from the pinned files, put the three source inputs in one directory and run `prepare.py DIRECTORY`; it checks all input digests first. The runtime uses typed C# event ranges; it does not compile or load this oracle. Native/source-generated fixture binaries are temporary and stay outside Git.

Runtime adaptation attribution is recorded in `licence/THIRD_PARTY_NOTICES.md` and covered by the existing shipped CanvasStyleGeometry MIT notice. Fixture-only copied sources retain their own full header; no extra runtime dependency is introduced.
