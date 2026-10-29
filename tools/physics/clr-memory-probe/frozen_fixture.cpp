// Reuse the same object/layout witnesses through the copied CLR's private test exports.
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <dlfcn.h>
template<class F> static F entry(const char *name) {
    static void *runtime = dlopen("libcoreclr.so", RTLD_NOW | RTLD_NOLOAD);
    void *value = runtime ? dlsym(runtime, name) : nullptr;
    if (!value) { std::fprintf(stderr, "clr-frozen-fixture: missing source runtime export %s\n", name); std::abort(); }
    return reinterpret_cast<F>(value);
}
extern "C" void *CLRRegisterFrozenSegment(void *memory, size_t allocated, size_t committed, size_t reserved) {
    return entry<void*(*)(void*, size_t, size_t, size_t)>("E2DRegisterFrozenSegment")(memory, allocated, committed, reserved);
}
extern "C" void CLRUpdateFrozenSegment(void *handle, uint8_t *allocated, uint8_t *committed) {
    entry<void(*)(void*, uint8_t*, uint8_t*)>("E2DUpdateFrozenSegment")(handle, allocated, committed);
}
extern "C" void CLRUnregisterFrozenSegment(void *handle) {
    entry<void(*)(void*)>("E2DUnregisterFrozenSegment")(handle);
}
#define RhRegisterFrozenSegment CLRRegisterFrozenSegment
#define RhUpdateFrozenSegment CLRUpdateFrozenSegment
#define RhUnregisterFrozenSegment CLRUnregisterFrozenSegment
#include "../aot-memory-probe/frozen_fixture.cpp"
