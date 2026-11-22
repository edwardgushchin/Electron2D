# Pinned NLayer decoder

The [NLayer 3.0.1 release](https://github.com/naudio/NLayer/releases/tag/v3.0.1) resolves to commit `cdd16ea2a389b30f99d5797ad7b09224ab29e01a`. The previous pin, `v3.0.0`, resolves to `046c7ce422970f8f0f0bc205d6c6341bcb7debf1`.

The [3.0.1 source tarball](https://codeload.github.com/naudio/NLayer/tar.gz/refs/tags/v3.0.1) has SHA-256 `332d93b6de8d2d8021ee82b0cf9d2058a1c8e821cd5129bf8a67ce2e3da30c79`. The [3.0.0 source tarball](https://codeload.github.com/naudio/NLayer/tar.gz/refs/tags/v3.0.0) has SHA-256 `270828a7b451d30f93a2e3495cf4a54a37cf9587f0d4bdf091013c4a034e4a9d`. Both hashes cover the downloaded gzip bytes. The [manifest](manifest.json) records the raw archive member digests before local changes.

The 17 selected C# files compile as internal code in Electron2D.dll. Local adaptations keep the engine-private namespace, internal top-level visibility and nullable and XML diagnostic settings. The update changes only `Decoder/BitReservoir.cs` among those sources and preserves the upstream decoder logic. The vendor payload consists of the managed decoder and its original MIT license, also retained under `licence/`.
