#include "meshoptimizer.h"
#include <cmath>
#include <limits>
#include <new>
#include <atomic>
#if defined(_WIN32)
#define E2D_EXPORT extern "C" __declspec(dllexport)
#else
#define E2D_EXPORT extern "C" __attribute__((visibility("default")))
#endif
using Allocate = void *(*)(size_t);
using Release = void (*)(void *);
static Allocate allocate_cb;
static Release release_cb;
static std::atomic<bool> ready{false};
static std::atomic_flag registration = ATOMIC_FLAG_INIT;
static void *allocate(size_t size) {
    void *value = allocate_cb(size);
    // The simplifier expects the allocator's ordinary throwing-new contract.
    if (!value) throw std::bad_alloc();
    return value;
}
static void release(void *value) { if (value) release_cb(value); }
E2D_EXPORT int e2d_mesh_memory(Allocate alloc, Release free) {
    if (!alloc || !free) return -1;
    while (registration.test_and_set(std::memory_order_acquire)) {}
    int result = 0;
    if (ready.load(std::memory_order_relaxed)) {
        if (allocate_cb != alloc || release_cb != free) result = -1;
    } else {
        allocate_cb = alloc; release_cb = free;
        meshopt_setAllocator(allocate, release);
        ready.store(true, std::memory_order_release);
    }
    registration.clear(std::memory_order_release);
    return result;
}
E2D_EXPORT float e2d_mesh_scale(const float* positions, size_t count) {
    return meshopt_simplifyScale(positions, count, 3 * sizeof(float));
}
E2D_EXPORT size_t e2d_mesh_simplify(unsigned int* output, const unsigned int* indices, size_t count,
    const float* positions, size_t vertices, const float* attributes, const float* weights,
    size_t attribute_count, size_t target, unsigned int options, float* error) {
    if (!ready.load(std::memory_order_acquire)) {
        *error = std::numeric_limits<float>::quiet_NaN();
        return size_t(-1);
    }
    try {
        return meshopt_simplifyWithAttributes(output, indices, count, positions, vertices, 3 * sizeof(float),
            attributes, attribute_count * sizeof(float), weights, attribute_count, nullptr, target, 1.0f, options, error);
    } catch (const std::bad_alloc&) {
        *error = std::numeric_limits<float>::quiet_NaN();
        return size_t(-1);
    }
}
