# MovieRecording

Last updated: 2026-10-15

Internal sealed class in [MovieRecording.cs](../../src/Servers/Movie/MovieRecording.cs), implementing IDisposable. Owns one recording lease, initial dimensions, rational sample remainder and prepared Int32 PCM storage. Engine constructs it before SceneTree activation, calls Write after each completed render and disposes it before scene teardown. Captures are independent borrowed-to-hook Images; centered cover cropping/resize preserves the initial output dimensions. Preparation failure attempts partial writer cleanup; Dispose releases the lease even if End throws. [Movie writing](../components/movie-writing.md) owns its runtime/verification contract. There is no public constructor or independent game loop.
