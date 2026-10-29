// Test-only refusal at this shaping domain's boundary; other ledger producers keep working.
#include "../enet-native/memory.h"
#include <atomic>
#include <cstddef>
static std::atomic<size_t> attempts{0}, position{0}, failures{0};
static bool refuse() {
    if (attempts.fetch_add(1) + 1 != position.load()) return false;
    failures.fetch_add(1);
    // Use the real range-failure path so consumers observe this same native-thread refusal.
    (void)e2d_memory_malloc(SIZE_MAX);
    return true;
}
static void *test_malloc(size_t size) { return refuse() ? nullptr : e2d_memory_malloc(size); }
static void *test_calloc(size_t count, size_t size) { return refuse() ? nullptr : e2d_memory_calloc(count, size); }
static void *test_realloc(void *value, size_t size) { return refuse() ? nullptr : e2d_memory_realloc(value, size); }
#define e2d_memory_malloc test_malloc
#define e2d_memory_calloc test_calloc
#define e2d_memory_realloc test_realloc
#include "allocator.cc"
extern "C" E2D_SHAPING_API void e2d_shaping_test_arm(size_t value) { attempts = 0; failures = 0; position = value; }
extern "C" E2D_SHAPING_API size_t e2d_shaping_test_attempts() { return attempts.load(); }
extern "C" E2D_SHAPING_API size_t e2d_shaping_test_failures() { return failures.load(); }
