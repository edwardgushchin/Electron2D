// Diagnostic Linux x64 CoreCLR producer. Slot numbers follow v10.0.1 corprof.idl.
#include <algorithm>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <limits>
#include <map>
#include <mutex>
#include <unistd.h>

struct GUID { uint32_t a; uint16_t b, c; uint8_t d[8]; };
constexpr GUID unknown{0, 0, 0, {0xc0, 0, 0, 0, 0, 0, 0, 0x46}};
constexpr GUID factoryID{1, 0, 0, {0xc0, 0, 0, 0, 0, 0, 0, 0x46}};
constexpr GUID callbackIDs[]{
    {0x176fbed1, 0xa55c, 0x4796, {0x98, 0xca, 0xa9, 0xda, 0x0e, 0xf8, 0x83, 0xe7}},
    {0x8a8cc829, 0xccf2, 0x49fe, {0xbb, 0xae, 0x0f, 0x02, 0x22, 0x28, 0x07, 0x1a}},
    {0x4fd2ed52, 0x7731, 0x4b8d, {0x94, 0x69, 0x03, 0xd2, 0xcc, 0x30, 0x86, 0xc5}},
    {0x7b63b2e3, 0x107d, 0x4d48, {0xb2, 0xf6, 0xf6, 0x1e, 0x22, 0x94, 0x70, 0xd2}}};
constexpr GUID info4ID{0x0d8fdcaa, 0x6257, 0x47bf, {0xb1, 0xbf, 0x94, 0xda, 0xc8, 0x84, 0x66, 0xee}};
struct Snapshot { uint64_t current, peak, allocated, retired, count, moved, collections, rawAllocated, frozenExcluded; };
struct Record { uint64_t size; uint32_t tag; uintptr_t destination; bool candidate, survived; };
struct Generation { int32_t generation; uintptr_t start, length, reserved; };
static_assert(sizeof(uintptr_t) == 8 && sizeof(Generation) == 32 && sizeof(Snapshot) == 72);
static void *info;
static std::mutex gate;
static bool ready, collecting;
static std::map<uintptr_t, Record> objects;
static Snapshot totals[32];
static thread_local uint32_t tag;
static void *callbacks[89], *factoryMethods[5];
struct Object { void **table; };
static Object profiler{callbacks}, factory{factoryMethods};
[[noreturn]] static void fail(const char *reason, int code = 135) {
    std::fprintf(stderr, "live-memory-producer: %s\n", reason); _exit(code);
}
static bool equal(const GUID *a, const GUID &b) { return std::memcmp(a, &b, sizeof(GUID)) == 0; }
template<class F> static F method(unsigned slot) { return reinterpret_cast<F>((*static_cast<void***>(info))[slot]); }
static uint64_t add(uint64_t a, uint64_t b) {
    if (b > std::numeric_limits<uint64_t>::max() - a) fail("counter overflow");
    return a + b;
}
static void allocatedBytes(Snapshot &s, uint64_t size, uint64_t raw) {
    s.current = add(s.current, size); s.peak = std::max(s.peak, s.current);
    s.allocated = add(s.allocated, size); s.rawAllocated = add(s.rawAllocated, raw); ++s.count;
}
static void retiredBytes(Snapshot &s, uint64_t size) {
    if (s.current < size || !s.count) fail("retirement underflow");
    s.current -= size; s.retired = add(s.retired, size); --s.count;
}
static int32_t noOperation(...) { return 0; }
static uint32_t addRef(void*) { return 2; }
static uint32_t release(void*) { return 1; }
static int32_t query(void*, const GUID *id, void **target) {
    if (equal(id, unknown)) { *target = &profiler; return 0; }
    for (const auto &supported : callbackIDs)
        if (equal(id, supported)) { *target = &profiler; return 0; }
    *target = nullptr; return static_cast<int32_t>(0x80004002);
}
static int32_t initialize(void*, void *source) {
    using Q = int32_t(*)(void*, const GUID*, void**);
    int32_t hr = reinterpret_cast<Q>((*static_cast<void***>(source))[0])(source, &info4ID, &info);
    if (hr < 0) return hr;
    // MONITOR_GC deliberately disables concurrent GC in this CLR profiling mode.
    hr = method<int32_t(*)(void*, uint32_t)>(16)(info, 0x00800000 | 0x00000100 | 0x00000080);
    ready = hr >= 0;
    return hr;
}
static int32_t shutdown(void*) {
    std::lock_guard<std::mutex> hold(gate);
    ready = false;
    if (info) { method<uint32_t(*)(void*)>(2)(info); info = nullptr; }
    return 0;
}
static int32_t allocated(void*, uintptr_t object, uintptr_t) {
    // These Info4/Info2 queries cannot trigger GC. Never enter managed code while holding gate.
    std::lock_guard<std::mutex> hold(gate);
    if (!ready || collecting) fail("allocation outside a ready mutator interval");
    size_t raw = 0;
    if (method<int32_t(*)(void*, uintptr_t, size_t*)>(80)(info, object, &raw) < 0 || !raw)
        fail("GetObjectSize2 failed");
    Generation generation{};
    int32_t generationHR = method<int32_t(*)(void*, uintptr_t, Generation*)>(55)(info, object, &generation);
    if (generationHR == static_cast<int32_t>(0x80131389) && generation.generation == INT32_MAX) {
        if (tag) fail("tagged witness is outside the dynamic GC heap");
        ++totals[0].frozenExcluded;
        return 0;
    }
    if (generationHR < 0) fail("allocation generation query failed");
    uint64_t size = add(raw, 7) & ~uint64_t{7};
    try {
        if (!objects.emplace(object, Record{size, tag, object, false, false}).second)
            fail("object address reused before retirement");
    } catch (...) { fail("object table allocation failed"); }
    allocatedBytes(totals[0], size, raw);
    if (tag) allocatedBytes(totals[tag], size, raw);
    return 0;
}
static int32_t gcStarted(void*, int32_t count, int32_t *generations, int32_t) {
    std::lock_guard<std::mutex> hold(gate);
    if (collecting || count < 3 || count > 5) fail("unexpected collection nesting or generation set");
    collecting = true;
    for (auto &[address, object] : objects) {
        Generation generation{};
        int32_t hr = method<int32_t(*)(void*, uintptr_t, Generation*)>(55)(info, address, &generation);
        if (hr < 0 || generation.generation < 0 || generation.generation > 4) {
            std::fprintf(stderr, "object=%zx tag=%u hr=%x generation=%d\n", address, object.tag,
                static_cast<uint32_t>(hr), generation.generation);
            fail("GetObjectGeneration failed");
        }
        // LOH/POH are collected with generation 2; older CLR callback arrays may omit their entries.
        int32_t index = generation.generation < count ? generation.generation : 2;
        object.candidate = generations[index] != 0;
        object.survived = !object.candidate;
        object.destination = address;
    }
    return 0;
}
static int32_t ranges(uint32_t count, uintptr_t *oldStart, uintptr_t *newStart, size_t *lengths) {
    std::lock_guard<std::mutex> hold(gate);
    if (!collecting) fail("survivor range outside collection");
    for (uint32_t i = 0; i < count; ++i) {
        if (lengths[i] > std::numeric_limits<uintptr_t>::max() - oldStart[i]) fail("invalid survivor range");
        auto end = oldStart[i] + lengths[i];
        for (auto it = objects.lower_bound(oldStart[i]); it != objects.end() && it->first < end; ++it) {
            auto &[address, object] = *it;
            uintptr_t destination = newStart ? add(newStart[i], address - oldStart[i]) : address;
            if (object.candidate && object.survived && object.destination != destination) fail("conflicting relocation");
            object.survived = true; object.destination = destination;
        }
    }
    return 0;
}
static int32_t moved(void*, uint32_t count, uintptr_t *oldStart, uintptr_t *newStart, size_t *lengths) {
    return ranges(count, oldStart, newStart, lengths);
}
static int32_t survived(void*, uint32_t count, uintptr_t *start, size_t *lengths) {
    return ranges(count, start, nullptr, lengths);
}
static int32_t gcFinished(void*) {
    std::lock_guard<std::mutex> hold(gate);
    if (!collecting) fail("unbalanced collection finish");
    std::map<uintptr_t, Record> survivors;
    try {
        for (const auto &[address, object] : objects) {
            if (!object.survived) {
                retiredBytes(totals[0], object.size);
                if (object.tag) retiredBytes(totals[object.tag], object.size);
            } else {
                if (address != object.destination) {
                    ++totals[0].moved;
                    if (object.tag) ++totals[object.tag].moved;
                }
                // Keep old keys until every range has been applied: moved ranges can overlap.
                if (!survivors.emplace(object.destination, object).second) fail("duplicate relocated address");
            }
        }
    } catch (...) { fail("survivor table allocation failed"); }
    objects.swap(survivors);
    for (auto &s : totals) ++s.collections;
    collecting = false;
    return 0;
}
static int32_t factoryQuery(void*, const GUID *id, void **target) {
    if (equal(id, unknown) || equal(id, factoryID)) { *target = &factory; return 0; }
    *target = nullptr; return static_cast<int32_t>(0x80004002);
}
static int32_t create(void*, void*, const GUID *id, void **target) {
    for (auto &slot : callbacks) slot = reinterpret_cast<void*>(noOperation);
    callbacks[0] = reinterpret_cast<void*>(query); callbacks[1] = reinterpret_cast<void*>(addRef);
    callbacks[2] = reinterpret_cast<void*>(release); callbacks[3] = reinterpret_cast<void*>(initialize);
    callbacks[4] = reinterpret_cast<void*>(shutdown); callbacks[50] = reinterpret_cast<void*>(allocated);
    callbacks[73] = reinterpret_cast<void*>(gcStarted); callbacks[75] = reinterpret_cast<void*>(gcFinished);
    callbacks[87] = reinterpret_cast<void*>(moved); callbacks[88] = reinterpret_cast<void*>(survived);
    return query(nullptr, id, target);
}
#define EXPORT extern "C" __attribute__((visibility("default")))
EXPORT int32_t DllGetClassObject(const GUID*, const GUID *id, void **target) {
    factoryMethods[0] = reinterpret_cast<void*>(factoryQuery); factoryMethods[1] = reinterpret_cast<void*>(addRef);
    factoryMethods[2] = reinterpret_cast<void*>(release); factoryMethods[3] = reinterpret_cast<void*>(create);
    factoryMethods[4] = reinterpret_cast<void*>(noOperation);
    return factoryQuery(nullptr, id, target);
}
EXPORT int32_t DllCanUnloadNow() { return 1; }
EXPORT void e2d_live_tag(uint32_t value) {
    std::lock_guard<std::mutex> hold(gate);
    if (!ready || value >= 32) fail("uninitialized producer or invalid tag", 132);
    tag = value;
}
EXPORT void e2d_live_snapshot(uint32_t value, Snapshot *result) {
    std::lock_guard<std::mutex> hold(gate);
    if (!ready || value >= 32 || !result) fail("uninitialized producer or invalid snapshot", 132);
    *result = totals[value];
}
