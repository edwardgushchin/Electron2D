# Pinned NVorbis decoder

Source: https://github.com/NVorbis/NVorbis at `v0.10.5`, commit `519d4e2aae7d6a4d5bab552ec5c1e517e9c78855`.

Selected managed decoder sources compile internally into Electron2D.dll. Only namespace, top-level visibility and diagnostic boundaries change; decoder logic is preserved. Native/NAudio adapters and test projects are not included. Source digests and archive pin are retained in manifest.json; the original MIT license is retained here and under licence/.
