# Pinned FAudio dependency

Source: https://github.com/FNA-XNA/FAudio at tag `26.09`, commit `667942a9097991f5cb6d1532a7ac3142e0581102`.

The native source closure and CMake metadata are unchanged. The complete C# binding has only integration-boundary changes recorded in manifest.json: internal top-level type and preserved-source warning policy. Upstream licenses remain under licence/ and in this source tree. Native builds use SDL3 and disable the unneeded XNA song layer, tests and utility applications. Runtime formats and mixing stay in the owned audio layer; dependency declarations are not public engine API.
