# Third-party notices

Electron2D-authored code is licensed under the root [MIT license](LICENSE). That license does not replace the licenses of vendored source, adapted algorithms, reference data, or native/runtime binaries. Keep this file and its referenced license texts with source and binary distributions.

## Source included in the repository or engine assembly

| Component | License | License text |
| --- | --- | --- |
| SDL3-CS managed bindings | zlib | [UPSTREAM-LICENSE](src/Vendor/SDL3-CS/UPSTREAM-LICENSE.txt) |
| Box2D.NET managed backend | MIT | [LICENSE](src/Vendor/Box2D.NET/LICENSE) |
| Clipper2 managed geometry | BSL-1.0 | [LICENSE](src/Vendor/Clipper2/LICENSE) |
| FastNoiseLite managed algorithm | MIT | [LICENSE](src/Vendor/FastNoiseLite/LICENSE) |
| Adapted PCG32 algorithm | Apache-2.0 | [PCG32-LICENSE](docs/licenses/PCG32-LICENSE.txt) |
| Adapted PolyPartition algorithm | MIT | [PolyPartition-LICENSE](docs/licenses/PolyPartition-LICENSE.txt) |
| Pinned comparison data under `docs/coverage` (not in the runtime publish) | MIT | [GODOT-LICENSE](docs/coverage/GODOT-LICENSE.txt) |

The license texts above are copied into an Electron2D project publish at the same relative paths. The comparison data itself is not in the runtime publish.

## Native files in the current self-contained Linux publish

This inventory comes from `dotnet publish examples/HostExample/HostExample.csproj -c Release -r <rid> --self-contained true` on 2026-09-25. Before notices were added, both `linux-x64` and `linux-arm64` outputs contained 237 files. Each now contains 266 files, including 29 license/notice texts, 60 `.so` filenames and two other ELF files (`HostExample` and `createdump`). Versioned and unversioned `.so` names are separate files in the output; every native name is covered by exactly one row below. The .NET runtime pack was 8.0.22. The native package versions were SDL3-CS.Linux 3.4.16, Image 3.4.6.9 and Shadercross 3.0.0.11. The table records delivered filenames, not every library that may be supplied by the operating system.

| Published native filenames, both RIDs | Component and applicable license text |
| --- | --- |
| `HostExample`, `createdump`, `libSystem.*.so`, `libclrgc.so`, `libclrjit.so`, `libcoreclr.so`, `libcoreclrtraceptprovider.so`, `libhostfxr.so`, `libhostpolicy.so`, `libmscordaccore.so`, `libmscordbi.so` | .NET runtime 8.0.22: [MIT](docs/licenses/native/dotnet-8.0.22-LICENSE.txt) and [runtime third-party notices](docs/licenses/native/dotnet-8.0.22-ThirdPartyNotices.txt). An application has its own executable name in place of `HostExample`. |
| `libSDL3.so*` | SDL 3.4.16: [zlib](docs/licenses/native/SDL-LICENSE.txt). |
| `libSDL3_image.so*` | SDL_image 3.4.6: [zlib](docs/licenses/native/SDL_image-LICENSE.txt). |
| `libaom.so*` | libaom: [BSD-2-Clause](docs/licenses/native/libaom-LICENSE.txt) and [Alliance for Open Media Patent License 1.0](docs/licenses/native/libaom-PATENTS.txt). |
| `libavif.so*` | libavif: [upstream license and bundled notices](docs/licenses/native/libavif-LICENSE.txt). |
| `libdav1d.so*` | dav1d: [BSD-2-Clause](docs/licenses/native/dav1d-COPYING.txt) and [Alliance for Open Media patent terms](docs/licenses/native/dav1d-PATENTS.txt). |
| `libpng16.so*` | libpng 1.6.58: [libpng license](docs/licenses/native/libpng-LICENSE.txt). |
| `libtiff.so*` | libtiff: [libtiff license](docs/licenses/native/libtiff-LICENSE.md). |
| `libwebp.so*`, `libwebpdemux.so*`, `libwebpmux.so*` | libwebp: [BSD-3-Clause](docs/licenses/native/libwebp-COPYING.txt). |
| `libSDL3_shadercross.so*` | SDL_shadercross 3.0.0: [zlib](docs/licenses/native/SDL_shadercross-LICENSE.txt). |
| `libspirv-cross-c-shared.so*` | SPIRV-Cross: [Apache-2.0 license text](docs/licenses/native/SPIRV-Cross-LICENSE.txt); upstream source also permits MIT as an alternative. |
| `libdxcompiler.so`, `libdxil.so` | DirectXShaderCompiler: [license set](docs/licenses/native/DXC-LICENSE.txt), [LLVM notice](docs/licenses/native/DXC-LICENSE-LLVM.txt), [Microsoft notice](docs/licenses/native/DXC-LICENSE-MS.txt) and [third-party notices](docs/licenses/native/DXC-ThirdPartyNotices.txt). |
| `libvkd3d.so*`, `libvkd3d-shader.so*`, `libvkd3d-utils.so*` | Vkd3d: [upstream copyright notice](docs/licenses/native/vkd3d-COPYING.txt) and [LGPL-2.1-or-later text](docs/licenses/native/LGPL-2.1-or-later.txt). |

The native license texts above are copied into `docs/licenses/native/` by an Electron2D project publish. The referenced .NET and DirectXShaderCompiler texts came from the exact NuGet runtime packages listed above; libwebp's COPYING came from the Image package. The other texts were obtained from the corresponding upstream projects and retained without changing their terms.

## Release audit boundary

- **Not yet ready to claim complete LGPL compliance:** the referenced Shadercross package includes Vkd3d shared libraries, but does not include their corresponding source archive or an identified source offer. Before distributing these binaries, identify their exact build/source revisions, provide the corresponding source and required notices, and verify that recipients can use a compatible modified shared library under LGPL section 6. Review this with counsel for the intended package and platform.
- This audit covers the current self-contained `linux-x64` and `linux-arm64` HostExample publishes. Electron2D does not yet ship verified native packages for Windows, macOS, Android, iOS or Web; audit their actual publish outputs and add their notices before claiming support. The same applies when package versions or the .NET runtime pack change.
- A native package can contain statically linked code or further bundled notices that are not apparent from `.so` filenames. Reconcile the final release artifact and upstream build provenance before publication. `tools/licenses/check_publish.py` checks this inventory's filenames and included notice files for the two audited Linux RIDs; it does not replace the source-provenance and legal review above.
