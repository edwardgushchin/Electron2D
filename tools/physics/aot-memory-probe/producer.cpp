// Test-only source-runtime callback ledger. It never calls managed code or triggers GC.
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <atomic>
#include <limits>
#include <new>
#include <pthread.h>
#include <sched.h>
#include <unistd.h>
#include "../../enet-native/memory.h"
struct Counts { uint64_t current, peak, allocated, retired, count, collections, fastObjects, fastArrays, slow, frozenCount, frozenBytes, failures; };
static_assert(sizeof(Counts) == 96 && sizeof(uintptr_t) == 8);
struct Watch { uintptr_t type; int64_t length; Counts totals; uint64_t survivors, survivorCount; };
struct FrozenCounts { uint64_t current, peak, allocated, retired, segments, registrations, updates, removals, failures, managedCurrent, managedPeak, dynamicCurrent, dynamicPeak; };
static_assert(sizeof(FrozenCounts) == 104);
struct Segment { void *handle; uintptr_t start; uint64_t bytes; Segment *next; };
using ProcessCounts = E2DMemorySnapshot;
static_assert(sizeof(ProcessCounts) == 96);
struct CommonCounts { uint64_t attempts, soh, freeList, segment, completed, failed, reverted, bytes, pending; };
static_assert(sizeof(CommonCounts) == 72);
struct CommonScope { CommonScope *previous; void *object; uint64_t bytes; uintptr_t type, elements; unsigned route; bool canceled, delayed; };
static_assert(sizeof(CommonScope) <= 64 && alignof(CommonScope) <= alignof(uint64_t));
struct DelayCounts { uint64_t expectedPeak, freedBeforeTyped, completed; };
static Watch watches[16];
static FrozenCounts frozen;
static Segment *segments;
static bool collecting;
static thread_local bool fastCommit;
static thread_local CommonScope *commonScope;
static CommonCounts common;
static uint32_t rejectFinalizerTag, delayTag;
static void *delayBlock;
static pthread_t delayThread;
static bool delayActive;
static std::atomic<unsigned> delayStage;
static DelayCounts delayCounts;
static uint32_t rejectTag;
static bool rejectFrozen;
[[noreturn]] static void fail(const char *message, int code = 135) {
    std::fprintf(stderr, "aot-memory-producer: %s\n", message); _exit(code);
}
static void enter() { e2d_memory_lock(); }
static void leave() { e2d_memory_unlock(); }
static uint64_t add(uint64_t a, uint64_t b) {
    if (b > std::numeric_limits<uint64_t>::max() - a) fail("counter overflow");
    return a + b;
}
static uint64_t aligned(size_t size) { return add(size, 7) & ~uint64_t{7}; }
static ProcessCounts processSnapshotLocked() {
    ProcessCounts result; e2d_memory_snapshot_locked(&result); return result;
}
static void publishManaged() {
    frozen.managedCurrent = add(watches[0].totals.current, frozen.current);
    if (frozen.managedCurrent > frozen.managedPeak) frozen.managedPeak = frozen.managedCurrent;
    if (e2d_memory_managed_locked(frozen.managedCurrent)) fail("process payload exceeds signed monitor range");
}
extern "C" void *e2d_aot_native_alloc(size_t size, size_t alignment, uint32_t zero) {
    int32_t error;
    void *result = e2d_memory_allocate(size, alignment, zero, &error);
    if (error == 1) fail("invalid native allocation", 132);
    return result;
}
extern "C" void *e2d_aot_native_resize(void *value, size_t size) {
    int32_t error;
    void *result = e2d_memory_reallocate(value, size, &error);
    if (error == 1) fail("invalid aligned native resize", 132);
    return result;
}
extern "C" void e2d_aot_native_free(void *value) { e2d_memory_release(value); }
extern "C" void e2d_aot_process_read(ProcessCounts *result) {
    if (!result) fail("invalid process snapshot", 132);
    e2d_memory_snapshot(result);
}
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
extern "C" void e2d_aot_common_prepare(void *storage, size_t size, void *type, uintptr_t elements, bool concurrent) {
    enter();
    if (concurrent) fail("heap walk requires stopped EE and non-concurrent GC");
    if (!storage || !size || !type || fastCommit) fail("invalid common allocation scope");
    auto scope = new (storage) CommonScope{commonScope, nullptr, aligned(size), reinterpret_cast<uintptr_t>(type), elements, 0, false, false};
    if (delayTag && watches[delayTag].type == scope->type &&
        (watches[delayTag].length < 0 || static_cast<uint64_t>(watches[delayTag].length) == elements)) {
        scope->delayed = true; delayTag = 0;
    }
    commonScope = scope; ++common.attempts;
    leave();
}
extern "C" void e2d_aot_common_begin(unsigned route) {
    enter();
    if (!commonScope || commonScope->object || commonScope->route || route < 1 || route > 3 || collecting)
        fail("invalid common reservation interval");
    commonScope->route = route;
}
extern "C" void e2d_aot_common_commit(void *object) {
    if (!commonScope || commonScope->object || !commonScope->route || !object) fail("invalid common reservation commit");
    commonScope->object = object;
    if (commonScope->route == 1) ++common.soh;
    else if (commonScope->route == 2) ++common.freeList;
    else ++common.segment;
    common.bytes = add(common.bytes, commonScope->bytes);
#ifndef E2D_TEST_DEFER_COMMON_COMMIT
    allocate(watches[0].totals, commonScope->bytes, 3);
    publishManaged();
#endif
    if (commonScope->delayed) delayCounts.expectedPeak = processSnapshotLocked().current;
    leave();
}
extern "C" void e2d_aot_common_abort() {
    enter();
    if (!commonScope || commonScope->object) fail("common failure retained reserved payload");
    ++watches[0].totals.failures;
    for (unsigned i = 1; i < 16; ++i)
        if (watches[i].type == commonScope->type &&
            (watches[i].length < 0 || static_cast<uint64_t>(watches[i].length) == commonScope->elements)) ++watches[i].totals.failures;
    commonScope = commonScope->previous; ++common.failed;
    leave();
}
extern "C" void e2d_aot_common_cancel_begin(void *object) {
    enter();
    if (!commonScope || commonScope->object != object || commonScope->canceled) fail("invalid common reservation cancellation");
}
extern "C" void e2d_aot_common_cancel_end() {
#ifndef E2D_TEST_DEFER_COMMON_COMMIT
    auto &c = watches[0].totals;
    if (c.current < commonScope->bytes || !c.count) fail("common cancellation underflow");
    c.current -= commonScope->bytes; --c.count; c.retired = add(c.retired, commonScope->bytes);
    publishManaged();
#endif
    commonScope->object = nullptr; commonScope->canceled = true; ++common.reverted;
    leave();
}
extern "C" bool e2d_aot_finalizer_reject() {
    enter();
    bool reject = rejectFinalizerTag && commonScope && watches[rejectFinalizerTag].type == commonScope->type;
    if (reject) rejectFinalizerTag = 0;
    leave(); return reject;
}
extern "C" void e2d_aot_finalizer_fail_next(uint32_t tag) {
    enter();
    if (!tag || tag >= 16 || !watches[tag].type || rejectFinalizerTag) fail("invalid finalizer failure arm", 132);
    rejectFinalizerTag = tag;
    leave();
}
extern "C" void e2d_aot_common_read(CommonCounts *result) {
    enter();
    if (!result) fail("invalid common snapshot", 132);
    *result = common; result->pending = common.attempts - common.completed - common.failed;
    leave();
}
static void *delayWorker(void*) {
    delayStage.store(10, std::memory_order_release);
    while (delayStage.load(std::memory_order_acquire) != 1) sched_yield();
    e2d_aot_native_free(delayBlock);
    delayStage.store(2, std::memory_order_release);
    return nullptr;
}
extern "C" void e2d_aot_delay_arm(uint32_t tag, void *block) {
    enter();
    if (!tag || tag >= 16 || !watches[tag].type || !block || delayActive) fail("invalid common delay arm", 132);
    delayTag = tag; delayBlock = block; delayActive = true; delayCounts = {}; delayStage.store(0);
    leave();
    if (pthread_create(&delayThread, nullptr, delayWorker, nullptr)) fail("native delay worker creation failed");
    while (delayStage.load(std::memory_order_acquire) != 10) sched_yield();
}
extern "C" void e2d_aot_delay_join(DelayCounts *result) {
    enter();
    if (!result || !delayActive || delayStage.load(std::memory_order_acquire) != 2) fail("common delay did not complete");
    leave();
    if (pthread_join(delayThread, nullptr)) fail("native delay worker join failed");
    enter();
    delayCounts.completed = 1; *result = delayCounts; delayActive = false;
    leave();
}
extern "C" void e2d_aot_begin_fast() {
    enter();
    if (collecting || fastCommit) fail("invalid fast allocation interval");
    fastCommit = true;
}
extern "C" void e2d_aot_allocated(void *object, size_t size, uint32_t route) {
    if (route == 3) {
        if (fastCommit) fail("common allocation inside fast commit");
        if (!commonScope || commonScope->object != object || commonScope->bytes != aligned(size))
            fail("common typed completion missed reservation");
        if (commonScope->delayed) {
            delayStage.store(1, std::memory_order_release);
            while (delayStage.load(std::memory_order_acquire) != 2) sched_yield();
        }
        enter();
    } else if ((route != 1 && route != 2) || !fastCommit) fail("fast allocation missed commit gate");
    if (collecting || !object || !size) fail("invalid allocation interval");
    auto bytes = aligned(size);
    if (route != 3) {
        allocate(watches[0].totals, bytes, route);
        publishManaged();
    } else {
#ifdef E2D_TEST_DEFER_COMMON_COMMIT
        allocate(watches[0].totals, bytes, 3);
        publishManaged();
#endif
        if (commonScope->delayed) delayCounts.freedBeforeTyped = 1;
        commonScope = commonScope->previous; ++common.completed;
    }
    for (unsigned i = 1; i < 16; ++i)
        if (matches(watches[i], object)) allocate(watches[i].totals, bytes, route);
    fastCommit = false;
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
    if (watches[0].totals.frozenBytes != frozen.current) {
        std::fprintf(stderr, "frozen lifetime=%lu census=%lu\n", frozen.current, watches[0].totals.frozenBytes);
        fail("frozen lifecycle/census mismatch");
    }
    publishManaged();
    collecting = false;
    leave();
}
extern "C" bool e2d_aot_frozen_reject() {
    enter();
    bool reject = rejectFrozen;
    if (reject) { rejectFrozen = false; ++frozen.failures; }
    leave();
    return reject;
}
extern "C" void e2d_aot_frozen_fail_next() {
    enter();
    if (rejectFrozen) fail("frozen failure already armed", 132);
    rejectFrozen = true;
    leave();
}
extern "C" void e2d_aot_frozen_change(unsigned event, void *handle, uintptr_t start,
    uintptr_t allocated, uintptr_t committed, uintptr_t reserved) {
    // The caller holds gc_lock. Neither this critical section nor snapshots enter the runtime.
    enter();
    if (collecting || !handle || allocated < start || committed < allocated || reserved < committed || (allocated - start) % 8)
        fail("invalid frozen registry transition");
    Segment **slot = &segments;
    while (*slot && (*slot)->handle != handle) slot = &(*slot)->next;
    if (event == 1) {
        if (*slot) fail("duplicate frozen registration");
        auto record = static_cast<Segment*>(std::malloc(sizeof(Segment)));
        if (!record) fail("frozen observer storage exhausted");
        *record = Segment{handle, start, 0, nullptr}; *slot = record;
        ++frozen.segments; ++frozen.registrations;
    } else if (!*slot || (*slot)->start != start) fail("unknown frozen registry handle");
    auto record = *slot;
    uint64_t bytes = event == 3 ? 0 : allocated - start;
    if (bytes >= record->bytes) {
        auto delta = bytes - record->bytes;
        frozen.current = add(frozen.current, delta); frozen.allocated = add(frozen.allocated, delta);
    } else {
        auto delta = record->bytes - bytes;
        if (frozen.current < delta) fail("frozen retirement underflow");
        frozen.current -= delta; frozen.retired = add(frozen.retired, delta);
    }
    record->bytes = bytes;
    if (frozen.current > frozen.peak) frozen.peak = frozen.current;
    if (event == 2) ++frozen.updates;
    else if (event == 3) {
        *slot = record->next; std::free(record); --frozen.segments; ++frozen.removals;
    } else if (event != 1) fail("invalid frozen event");
    publishManaged();
    leave();
}
extern "C" void e2d_aot_frozen_read(FrozenCounts *result) {
    enter();
    if (!result || !frozen.registrations) fail("frozen hooks are absent", 132);
    *result = frozen;
    result->dynamicCurrent = watches[0].totals.current; result->dynamicPeak = watches[0].totals.peak;
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
