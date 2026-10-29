#include "webp_memory.h"
#include <stdatomic.h>
#include <stdint.h>
#include <string.h>
#if defined(_WIN32)
#define API __declspec(dllexport)
#else
#define API __attribute__((visibility("default")))
#endif
static void *(*allocate_cb)(size_t);
static void (*release_cb)(void *);
static atomic_bool ready;
static atomic_flag registration = ATOMIC_FLAG_INIT;

// Concurrent codec calls must retain one allocation owner through final release.
API int e2d_webp_memory(void *(*allocate)(size_t), void (*release)(void *)) {
    if (!allocate || !release) return -1;
    while (atomic_flag_test_and_set_explicit(&registration, memory_order_acquire)) {}
    int result = 0;
    if (atomic_load_explicit(&ready, memory_order_relaxed)) {
        if (allocate_cb != allocate || release_cb != release) result = -1;
    } else {
        allocate_cb = allocate; release_cb = release;
        atomic_store_explicit(&ready, 1, memory_order_release);
    }
    atomic_flag_clear_explicit(&registration, memory_order_release);
    return result;
}
int e2d_webp_memory_ready(void) { return atomic_load_explicit(&ready, memory_order_acquire); }
void *e2d_webp_allocate(size_t size) { return e2d_webp_memory_ready() ? allocate_cb(size) : NULL; }
void *e2d_webp_zero(size_t count, size_t size) {
    if (count && size > SIZE_MAX / count) return NULL;
    size *= count;
    void *value = e2d_webp_allocate(size);
    if (value) memset(value, 0, size);
    return value;
}
void e2d_webp_release(void *value) { if (value) release_cb(value); }
