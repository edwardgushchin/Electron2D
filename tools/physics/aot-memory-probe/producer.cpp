// Test-only NativeAOT callback ledger. It never calls managed code or triggers GC.
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <limits>
#include <pthread.h>
#include <unistd.h>
struct Counts { uint64_t current, peak, allocated, retired, count, collections, fastObjects, fastArrays, slow, frozenCount, frozenBytes, failures; };
static_assert(sizeof(Counts) == 96 && sizeof(uintptr_t) == 8);
struct Watch { uintptr_t type; int64_t length; Counts totals; uint64_t survivors, survivorCount; };
static Watch watches[16];
static pthread_mutex_t gate = PTHREAD_MUTEX_INITIALIZER;
static bool collecting;
static uint32_t rejectTag;
[[noreturn]] static void fail(const char *message, int code = 135) {
    std::fprintf(stderr, "aot-memory-producer: %s\n", message); _exit(code);
}
static void enter() { if (pthread_mutex_lock(&gate)) fail("mutex lock failed"); }
static void leave() { if (pthread_mutex_unlock(&gate)) fail("mutex unlock failed"); }
static uint64_t add(uint64_t a, uint64_t b) {
    if (b > std::numeric_limits<uint64_t>::max() - a) fail("counter overflow");
    return a + b;
}
static uint64_t aligned(size_t size) { return add(size, 7) & ~uint64_t{7}; }
static bool matches(const Watch &watch, void *object) {
    return watch.type && watch.type == *static_cast<uintptr_t*>(object) &&
        (watch.length < 0 || watch.length == *reinterpret_cast<uint32_t*>(static_cast<char*>(object) + 8));
}
static void allocate(Counts &c, uint64_t bytes, uint32_t route) {
    c.current = add(c.current, bytes); c.allocated = add(c.allocated, bytes);
    if (c.current > c.peak) c.peak = c.current;
    ++c.count;
    if (route == 1) ++c.fastObjects;
    else if (route == 2) ++c.fastArrays;
    else if (route == 3) ++c.slow;
    else fail("invalid allocation route");
}
extern "C" void e2d_aot_allocated(void *object, size_t size, uint32_t route) {
    enter();
    if (collecting || !object || !size) fail("invalid allocation interval");
    auto bytes = aligned(size);
    allocate(watches[0].totals, bytes, route);
    for (unsigned i = 1; i < 16; ++i)
        if (matches(watches[i], object)) allocate(watches[i].totals, bytes, route);
    leave();
}
extern "C" bool e2d_aot_reject(void *type, uintptr_t elements) {
    enter();
    bool reject = rejectTag && watches[rejectTag].type == reinterpret_cast<uintptr_t>(type) &&
        (watches[rejectTag].length < 0 || static_cast<uint64_t>(watches[rejectTag].length) == elements);
    if (reject) {
        ++watches[0].totals.failures;
        ++watches[rejectTag].totals.failures;
        rejectTag = 0;
    }
    leave();
    return reject;
}
extern "C" void e2d_aot_fail_next(uint32_t tag) {
    enter();
    if (!tag || tag >= 16 || !watches[tag].type || rejectTag) fail("invalid failure arm", 132);
    rejectTag = tag;
    leave();
}
extern "C" void e2d_aot_gc_begin(bool stopped) {
    enter();
    if (!stopped || collecting) fail("heap walk requires stopped EE and non-concurrent GC");
    collecting = true;
    for (auto &w : watches) w.survivors = w.survivorCount = 0;
    watches[0].totals.frozenCount = watches[0].totals.frozenBytes = 0;
}
extern "C" void e2d_aot_gc_survivor(void *object, size_t size, bool frozen) {
    if (!collecting || !object || !size) fail("invalid survivor interval");
    auto bytes = aligned(size);
    if (frozen) {
        ++watches[0].totals.frozenCount;
        watches[0].totals.frozenBytes = add(watches[0].totals.frozenBytes, bytes);
        return;
    }
    watches[0].survivors = add(watches[0].survivors, bytes); ++watches[0].survivorCount;
    for (unsigned i = 1; i < 16; ++i) if (matches(watches[i], object)) {
        watches[i].survivors = add(watches[i].survivors, bytes); ++watches[i].survivorCount;
    }
}
extern "C" void e2d_aot_gc_end() {
    if (!collecting) fail("unbalanced GC finish");
    for (auto &w : watches) {
        auto &c = w.totals;
        if (w.survivors > c.current || w.survivorCount > c.count) {
            std::fprintf(stderr, "type=%zx current=%lu survivors=%lu count=%lu survivorsCount=%lu fast=%lu/%lu slow=%lu frozen=%lu\n",
                w.type, c.current, w.survivors, c.count, w.survivorCount, c.fastObjects, c.fastArrays, c.slow, c.frozenCount);
            fail("unobserved successful allocation");
        }
        c.retired = add(c.retired, c.current - w.survivors);
        c.current = w.survivors; c.count = w.survivorCount; ++c.collections;
    }
    collecting = false;
    leave();
}
extern "C" void e2d_aot_watch(uint32_t tag, uintptr_t type, int64_t length) {
    enter();
    if (!tag || tag >= 16 || !type || watches[tag].type || length < -1 || length > INT32_MAX)
        fail("invalid type watch", 132);
    watches[tag].type = type; watches[tag].length = length;
    leave();
}
extern "C" void e2d_aot_read(uint32_t tag, Counts *result) {
    enter();
    if (tag >= 16 || !result) fail("invalid snapshot", 132);
    if (!watches[0].totals.allocated) fail("runtime allocation hooks are absent", 132);
    *result = watches[tag].totals;
    leave();
}
