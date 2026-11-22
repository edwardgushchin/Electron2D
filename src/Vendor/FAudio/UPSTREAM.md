# Pinned FAudio dependency

Source: https://github.com/FNA-XNA/FAudio at tag `26.11`, commit `0c423758079863fc3044ba26dd00fc89c41f469e`. [manifest.json](manifest.json) records the GitHub tag archive and the SHA-256 of the archive and its `csharp/FAudio.cs` file.

The native source closure retains the recorded Darwin-only deployment-default patch. The complete C# binding has integration-boundary changes recorded in manifest.json: internal top-level type, preserved-source warning policy and an engine-owned native-library constant selecting static executable symbols on iOS/tvOS. Upstream licenses remain under licence/ and in this source tree. Native builds use SDL3 and disable the unneeded XNA song layer, tests and utility applications. Runtime formats and mixing stay in the owned audio layer; dependency declarations are not public engine API.

Within the embedded source set, 26.11 changes the version constants in CMake, the C header and FAudio#. ABI 0, the SDL3 mixer and the upstream unity-SRC and tiny-ratio tap-bound fixes carry forward from 26.10. The engine output bridge is compiled separately from [output.c](../../../tools/audio-native/output.c).
