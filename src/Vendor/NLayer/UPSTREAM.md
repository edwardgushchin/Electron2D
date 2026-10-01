# Pinned NLayer decoder

Source: https://github.com/naudio/NLayer at `v3.0.0`, commit `046c7ce422970f8f0f0bc205d6c6341bcb7debf1`.

Selected managed decoder sources compile internally into Electron2D.dll. Only namespace, top-level visibility and diagnostic boundaries change; decoder logic is preserved. Native/NAudio adapters and test projects are not included. Source digests and archive pin are retained in manifest.json; the original MIT license is retained here and under licence/.
