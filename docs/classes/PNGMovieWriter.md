# PNGMovieWriter

Last updated: 2026-10-15

Internal sealed [MovieWriter](MovieWriter.md) implementation in [PNGMovieWriter.cs](../../src/Servers/Movie/PNGMovieWriter.cs). Acquired per recording only after custom handlers decline a .png path, and disposed by the recording lease. Borrows frame Images, owns one WAV FileAccess and writes numbered RGB PNGs with little-endian 32-bit PCM WAV. Built-in project settings select rate/channel mode. RIFF lengths finalize during End after normal or failed writing; absent partial-file state is safe. Output directory creation is the caller's responsibility. See [movie writing](../components/movie-writing.md) for filenames, lifetime, actual checks and unimplemented encoder dependencies.
