# Electron2D Native Packages

Private native libraries used by Electron2D for text boundaries, audio mixing/output and networking. This package adds no managed assembly or public backend API.

The platform packages contain matching assets under `runtimes/<RID>/native`. Their build targets preserve these paths in application outputs and copy required notices to `licence/`. SDL3 comes from the corresponding pinned SDL3-CS platform dependency. Linux supplies x64/ARM64 ELF libraries. The MacOS producer supplies x64/ARM64 Mach-O libraries and private OpenSSL; runtime integration is verified separately from native production.

The package manifest records the native source fingerprint and SHA-256 of each binary. CI verifies architecture, loader identity, required engine exports and absence of build-machine library search paths before packing. macOS ENet contains pinned Zstandard 1.5.7 under its BSD license; macOS TLS uses private, relocatable OpenSSL 3.6.4 libraries.

Ordinary Electron2D builds restore these binaries. Full native rebuilds use `-p:Electron2DBuildNativeFromSource=true` and require CMake, Ninja and C/C++ compilers. Native compilation fetches the matching SDL headers with a pinned archive hash; it does not compile SDL.
