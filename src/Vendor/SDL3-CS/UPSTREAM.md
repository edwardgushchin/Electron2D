# SDL3-CS core, shadercross and image binding source

Source: https://github.com/edwardgushchin/SDL3-CS
Release: v3.4.18.0
Commit: 8b89846448328879a15a4da7bd433a05001fa008

The complete upstream SDL3-CS/SDL, SDL3-CS/ShaderCross and SDL3-CS/Image trees are compiled into Electron2D.dll.
Local adaptation makes top-level binding types internal and suppresses CS0649 on
the native-initialized storage callback table. Core/Image/ShaderCross library
constants use the engine resolver's names, static executable symbols on iOS/tvOS
or generated archive module tables on Web.
Application-owned entry-point delegates are lazy so unrelated SDL calls do not
retain nonexistent host exports during static application linking.
Native SDL IO read/write results use pointer-sized size_t on both 32-bit and
64-bit hosts; the managed binding convenience result remains ulong.
Window flag imports use primitive ulong at the native boundary to preserve the
64-bit ABI in the WebAssembly interpreter. Managed wrappers keep WindowFlags.
The upstream license is retained.
Refresh with tools/update-sdl3-cs.sh and a release tag, then inspect the diff and
run the engine, test, coverage, and native example checks.
