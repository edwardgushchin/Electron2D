# Pinned FAudio dependency

Source: https://github.com/FNA-XNA/FAudio at tag `26.10`, commit `6839b88e304a046ae1a609ff14a085371e02a4e0`.

The native source closure retains the recorded Darwin-only deployment-default patch. The complete C# binding has integration-boundary changes recorded in manifest.json: internal top-level type, preserved-source warning policy and an engine-owned native-library constant selecting static executable symbols on iOS/tvOS. Upstream licenses remain under licence/ and in this source tree. Native builds use SDL3 and disable the unneeded XNA song layer, tests and utility applications. Runtime formats and mixing stay in the owned audio layer; dependency declarations are not public engine API.

The native sample slice updates the complete existing source closure to official 26.10. This includes upstream unity-SRC and tiny-ratio tap-bound fixes; it contains no Electron2D native code patches. The transient one-line 26.09 backport used to identify the crash was replaced by the complete official release.
