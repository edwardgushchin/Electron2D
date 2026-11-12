# SMAA 2.8

[Release v2.8](https://github.com/iryoku/smaa/tree/c9ff6c03e8a2e2b7daa94bf6a92a8dfe08161746)
at commit `c9ff6c03e8a2e2b7daa94bf6a92a8dfe08161746`.
The [upstream license](../../../../../../licence/SMAA-LICENSE.txt) accompanies
the source and shipped assembly.

`SMAA.h` is the unmodified release header. `Area.bin` and `Search.bin` contain the
byte arrays from `Textures/AreaTex.h` and `Textures/SearchTex.h` in their original
row order and channel encoding.

| File | Layout | SHA-256 |
| --- | --- | --- |
| SMAA.h | Shader header | a6e396730c1ddc7074d657168641cc6729ff3706b4a1f8fb3ba97e8f0d683eea |
| Area.bin | 160 × 560 RG8 | 8d13bff24657cd862bcee94188abbeada0b96f1d64f1a7ab329fe9cc159d0c7e |
| Search.bin | 66 × 33 R8 | e5d85ae5a659337e6d6654b85d2741a0b08a2c7d2eda200151fb0b3bd41e6966 |

The fragment entry points in the parent shader directory use the High preset
with zero subsample indices. The engine's edge pass extends the color comparison
to alpha coverage and estimates perceptual contrast for half-float input.
