# Electron2D Native Packages

Private native libraries used by Electron2D for text boundaries, audio mixing/output and networking. This package adds no managed assembly or public backend API.

The seven platform packages cover all 18 declared RIDs with assets under `runtimes/<RID>/native`. Linux and Android use ELF libraries, Windows uses native PE DLLs, macOS uses Mach-O libraries, and iOS/tvOS/Web use target-specific static archives. Apple device and simulator payloads are distinct. SDL3 comes from the pinned SDL3-CS platform dependency except Web, whose archive is built from the same pinned SDL source release.

The package manifest records the native source fingerprint and SHA-256 of each binary. CI verifies architecture, loader or archive platform identity, required engine exports and absence of build-machine library search paths before packing. Android ELF segments must support 16 KB pages. Zstandard 1.5.7 and OpenSSL 3.6.4 retain their original notices.

Ordinary desktop Electron2D builds currently restore the Linux/macOS packages. The additional producer candidates require target execution and runtime loader/static-link integration before they establish engine support. Native production runs once per RID in shared CI and requires CMake, Ninja and the selected target compiler. Cross-target production uses `tools/native/build_cross.py`; Windows uses `tools/native/build_windows.py`. Linux/macOS full rebuilds retain `-p:Electron2DBuildNativeFromSource=true`.
