# Third-party notices

Electron2D-authored code is licensed under the [MIT license](Electron2D-LICENSE.txt). That license does not replace the licenses of vendored source, adapted algorithms, reference data, or native/runtime binaries. Keep this file and its referenced license texts with source and binary distributions.

## Source included in the repository or engine assembly

| Component | License | License text |
| --- | --- | --- |
| SDL3-CS managed bindings | zlib | [UPSTREAM-LICENSE](SDL3-CS-LICENSE.txt) |
| Box2D.NET managed backend | MIT | [LICENSE](Box2D.NET-LICENSE.txt) |
| Clipper2 managed geometry | BSL-1.0 | [LICENSE](Clipper2-LICENSE.txt) |
| FastNoiseLite managed algorithm | MIT | [LICENSE](FastNoiseLite-LICENSE.txt) |
| Adapted PCG32 algorithm | Apache-2.0 | [PCG32-LICENSE](PCG32-LICENSE.txt) |
| Adapted PolyPartition algorithm | MIT | [PolyPartition-LICENSE](PolyPartition-LICENSE.txt) |

The license texts above are published together in `licence/`. The comparison data under `docs/coverage` and its separate repository license are not included in a runtime publish.

## Native files in the current self-contained Linux publish

This inventory comes from a self-contained `linux-x64` HostExample publish on 2026-09-25. It contains 63 ELF files, including the application host and `createdump`; versioned and unversioned `.so` names are separate delivered files. Every ELF name is covered by exactly one row below. The runtime pack is .NET 10.0.1, with SDL3-CS.Linux 3.4.16, Image 3.4.6.9 and Shadercross 3.0.0.11. Other application names replace `HostExample` in the first row.

| Published native filenames, linux-x64 | Component and applicable license text |
| --- | --- |
| `HostExample`, `createdump`, `libSystem.*.so`, `libclrgc.so`, `libclrgcexp.so`, `libclrjit.so`, `libcoreclr.so`, `libcoreclrtraceptprovider.so`, `libhostfxr.so`, `libhostpolicy.so`, `libmscordaccore.so`, `libmscordbi.so` | .NET runtime 10.0.1: [MIT](dotnet-10.0.1-LICENSE.txt) and [runtime third-party notices](dotnet-10.0.1-ThirdPartyNotices.txt). An application has its own executable name in place of `HostExample`. |
| `libSDL3.so*` | SDL 3.4.16: [zlib](SDL-LICENSE.txt). |
| `libSDL3_image.so*` | SDL_image 3.4.6: [zlib](SDL_image-LICENSE.txt). |
| `libaom.so*` | libaom: [BSD-2-Clause](libaom-LICENSE.txt) and [Alliance for Open Media Patent License 1.0](libaom-PATENTS.txt). |
| `libavif.so*` | libavif: [upstream license and bundled notices](libavif-LICENSE.txt). |
| `libdav1d.so*` | dav1d: [BSD-2-Clause](dav1d-COPYING.txt) and [Alliance for Open Media patent terms](dav1d-PATENTS.txt). |
| `libpng16.so*` | libpng 1.6.58: [libpng license](libpng-LICENSE.txt). |
| `libtiff.so*` | libtiff: [libtiff license](libtiff-LICENSE.md). |
| `libwebp.so*`, `libwebpdemux.so*`, `libwebpmux.so*` | libwebp: [BSD-3-Clause](libwebp-COPYING.txt). |
| `libSDL3_shadercross.so*` | SDL_shadercross 3.0.0: [zlib](SDL_shadercross-LICENSE.txt). |
| `libspirv-cross-c-shared.so*` | SPIRV-Cross: [Apache-2.0 license text](SPIRV-Cross-LICENSE.txt); upstream source also permits MIT as an alternative. |
| `libdxcompiler.so`, `libdxil.so` | DirectXShaderCompiler: [license set](DXC-LICENSE.txt), [LLVM notice](DXC-LICENSE-LLVM.txt), [Microsoft notice](DXC-LICENSE-MS.txt) and [third-party notices](DXC-ThirdPartyNotices.txt). |
| `libvkd3d.so*`, `libvkd3d-shader.so*`, `libvkd3d-utils.so*` | Vkd3d: [upstream copyright notice](vkd3d-COPYING.txt) and [LGPL-2.1-or-later text](LGPL-2.1-or-later.txt). |

All listed license texts are copied into `licence/` by an Electron2D project publish. The referenced .NET and DirectXShaderCompiler texts came from the exact NuGet runtime packages listed above; libwebp's COPYING came from the Image package. The other texts were obtained from the corresponding upstream projects and retained without changing their terms.

## Release audit boundary

- **Not yet ready to claim complete LGPL compliance:** the referenced Shadercross package includes Vkd3d shared libraries, but does not include their corresponding source archive or an identified source offer. Before distributing these binaries, identify their exact build/source revisions, provide the corresponding source and required notices, and verify that recipients can use a compatible modified shared library under LGPL section 6. Review this with counsel for the intended package and platform.
- This audit covers the current self-contained `linux-x64` HostExample publish. Electron2D does not yet ship verified native packages for Windows, macOS, Android, iOS, Android TV, tvOS or Web; audit their actual publish outputs and add their notices before claiming support. The same applies when package versions or the .NET runtime pack change.
- A native package can contain statically linked code or further bundled notices that are not apparent from `.so` filenames. Reconcile the final release artifact and upstream build provenance before publication. `tools/licenses/check_publish.py` checks this inventory's filenames and included notice files for the audited Linux x64 RID; it does not replace the source-provenance and legal review above.
