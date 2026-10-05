# Electron2D.Native.Linux

Private native libraries used by Electron2D for text boundaries, audio mixing/output and networking. This package adds no managed assembly or public backend API.

The CI package contains Linux x64 and ARM64 assets under `runtimes/<RID>/native`. Its build targets preserve these paths in application outputs and copy the required notices to `licence/`. SDL3 comes from the pinned SDL3-CS.Linux dependency.

The package manifest records the native source fingerprint and SHA-256 of each binary. CI verifies architecture, SONAME, required engine exports and absence of build-machine library search paths before packing.

Ordinary Electron2D builds restore these binaries. Full native rebuilds use `-p:Electron2DBuildNativeFromSource=true` and require CMake, Ninja and C/C++ compilers. Native compilation fetches the matching SDL headers with a pinned archive hash; it does not compile SDL.
