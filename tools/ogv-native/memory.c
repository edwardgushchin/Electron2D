#include "bridge.h"
#include "memory.h"
#include <stdatomic.h>
static E2DMemoryFunctions functions;
static atomic_bool ready;
static atomic_flag registration = ATOMIC_FLAG_INIT;
#if defined(_WIN32)
static __declspec(thread) uint64_t failure_epoch;
#else
static _Thread_local uint64_t failure_epoch;
#endif

int e2d_ogv_memory(const E2DMemoryFunctions *value) {
    if (!value || !value->allocate || !value->zero || !value->resize || !value->release) return -1;
    while (atomic_flag_test_and_set_explicit(&registration, memory_order_acquire)) {}
    int result = 0;
    if (atomic_load_explicit(&ready, memory_order_relaxed)) {
        if (functions.allocate != value->allocate || functions.zero != value->zero ||
            functions.resize != value->resize || functions.release != value->release) result = -1;
    } else {
        functions = *value;
        atomic_store_explicit(&ready, 1, memory_order_release);
    }
    atomic_flag_clear_explicit(&registration, memory_order_release);
    return result;
}
int e2d_ogv_memory_ready(void) { return atomic_load_explicit(&ready, memory_order_acquire); }
static void *record(void *value) { if (!value) ++failure_epoch; return value; }
uint64_t e2d_ogv_failure_epoch(void) { return failure_epoch; }
void *e2d_ogv_allocate(size_t size) { return e2d_ogv_memory_ready() ? record(functions.allocate(size)) : NULL; }
void *e2d_ogv_zero(size_t count, size_t size) { return e2d_ogv_memory_ready() ? record(functions.zero(count, size)) : NULL; }
void *e2d_ogv_resize(void *value, size_t size) { return e2d_ogv_memory_ready() ? record(functions.resize(value, size)) : NULL; }
void e2d_ogv_release(void *value) { if (value) functions.release(value); }
