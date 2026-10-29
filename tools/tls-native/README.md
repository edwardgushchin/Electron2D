# Owned TLS allocation source profile

The Linux source profile uses the same pinned OpenSSL 4.1.0 archive as the ordinary
TLS, DTLS, WebRTC and configured BCL cryptography consumers. It changes allocation
routing in a generated source copy; it does not introduce another upstream version.

Build the common ledger first, then the selected TLS pair:

```bash
cmake -S tools/enet-native -B BUILD/core -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build BUILD/core --target Electron2DENet Electron2DMemoryCheck
BUILD/core/Electron2DMemoryCheck
python3 -B tools/native/build_tls.py linux-x64 BUILD --memory-library BUILD/core/libElectron2DENet.so
python3 -B tools/tls-native/check.py BUILD BUILD/checks
```

The producer requires the existing Perl/compiler/make/patchelf tools. Five pinned
source preimages and exact allocation-site counts guard preparation. Repeating
preparation preserves identical generated files. Ordinary malloc/realloc/free,
aligned allocation, pre-initialization configuration strings and directory context
storage use the existing pure-C ledger before managed execution. Explicit custom
allocators retain their original ownership.

The secure heap keeps its original mmap, guard pages, locking and cleansing. The
same native gate covers the actual mapping/unmapping and ownership publication.
Its entire requested mapping, including guard pages, contributes once; secure
suballocations borrow that retained arena. Mapping refusal contributes no storage
and records a native failure epoch. Only three redundant aborting metadata-null
assertions are removed, allowing their existing failure/cleanup paths to execute.
No jump frame crosses cryptographic code or resource destructors.

Select the resulting TLS directory and the matching core through
`Electron2DTLSNativeDirectory` and `Electron2DENetNativeLibrary`. Follow
[unified TLS qualification](../../docs/maintaining.md#unified-native-tls-qualification)
for the WebRTC bridge and Linux PAL startup. `LD_LIBRARY_PATH` must name the
consumer's actual `runtimes/linux-x64/native` directory, with
`DOTNET_OPENSSL_VERSION_OVERRIDE=4` set before BCL use. One loaded TLS pair and one
actual common core are required.

`check.py` exercises original-source preservation, all five drift rejections,
repeatable preparation, explicit custom allocators, early configuration strings,
ordinary/zero/aligned/failed resize, secure mapping budgets, secure suballocation,
context/BIO lifetime, SHA-256, four native workers and every observed refused
request with healthy retry. The test-only fault core verifies full retirement
in its native destructor, after libcrypto's destructor. OpenSSL 4.1 retains global
caches until native destruction after `OPENSSL_cleanup` requests cleanup; a
snapshot immediately after that request is not a leak oracle. The test core must
never be shipped or substituted in ordinary consumer output.

For a full ASan/UBSan check, build both TLS and the common core with
`-fsanitize=address,undefined -fno-omit-frame-pointer`, then pass `--sanitize` to
the same checker. Keep the pinned pristine source available at
`BUILD/openssl-4.1.0` and its generated headers in `BUILD/build/include`.
`ELECTRON2D_TEST_TLS_ALLOCATION=1` runs the real BCL RSA/ECDSA ownership check with
this selected profile; an uninstrumented TLS pair is a required negative control.
The existing TLS/DTLS/WebRTC/crypto root groups and public CLR/full-trim NativeAOT
consumers remain functional gates.

This qualifies selected original heap and secure-mapping ownership. Libc/OS
service internals, external providers, transport C++ storage, stock runtime/package
selection, foreign targets and exact managed/native composition remain separate.
It does not implement the public total-memory monitors.
