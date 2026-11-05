# Private WebRTC bridge

The bridge uses the unchanged vendored libdatachannel 0.24.6 closure and the
same pinned OpenSSL 4.1.0 pair as the other TLS consumers. Media and the independent
WebSocket backend remain disabled. See the [component contract](../../docs/components/webrtc.md).

`memory.hpp` supplies the original C++ allocator for bridge metadata. Unified builds
use the existing engine core; standalone CMake consumers link `Electron2DRTCMemory`
once. The [qualified ownership and error boundary](../../docs/components/webrtc.md#bridge-owned-native-storage-2026-11-05)
defines counted storage, actual weak/thread lifetime and the remaining packet,
backend/provider and runtime domains. It does not implement a total-memory monitor.

```bash
cmake -S tools/webrtc-native -B BUILD -G Ninja -DCMAKE_BUILD_TYPE=Release \
  -DELECTRON2D_STATIC_NATIVE=ON -DOPENSSL_ROOT_DIR=OPENSSL_4_PREFIX
cmake --build BUILD --target Electron2DWebRTC Electron2DRTCMemoryCheck \
  Electron2DRTCConsumerCheck Electron2DRTCCloseFailureCheck --parallel 1
LD_LIBRARY_PATH=OPENSSL_4_PREFIX/lib BUILD/Electron2DRTCMemoryCheck
```

The Linux refusal fixture exercises actual bridge types and ABI event/read
operations: calibrated allocation refusals, temporary drain and retry, exact
handle/event bytes, weak lifetime, failed queue insertion, allocation-free close,
diagnostic fallback and thread-local retirement. The common core is replaced only
in that test executable; refusal is scoped to its calling thread.

`consumer/libElectron2D.so` and `close-failure/libElectron2D.so` are RTC/core-only
test modules. Never package them as the complete native product. Use the first
with FastTests `--webrtc`, and the second with `--webrtc-close-error`. The latter
injects an error after real native teardown. Publish CLR and full-trim NativeAOT
against the matching module. Before any BCL cryptography, set
`DOTNET_OPENSSL_VERSION_OVERRIDE=4` and `LD_LIBRARY_PATH` to the consumer's selected
native directory; retain one copy of the SSL/crypto pair. Inspect process maps.
The [TLS instructions](../tls-native/README.md) own that process setup.

Local receipts and both failing preceding-profile controls are retained in
`bin/webrtc-memory/2026-11-05/`. Full bundle/package, foreign-host, browser/routed
and complete allocator acceptance remain separate.
