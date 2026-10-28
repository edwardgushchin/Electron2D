#include <atomic>
#include <cstddef>
#include <cstring>
#include <limits>

#if defined(_WIN32)
#define API __declspec(dllexport)
#else
#define API __attribute__((visibility("default")))
#endif

using Allocate = void *(*)(size_t);
using Resize = void *(*)(void *, size_t);
using Release = void (*)(void *);
static Allocate allocate_cb;
static Resize resize_cb;
static Release release_cb;
static std::atomic<bool> ready{false};
static std::atomic_flag registration = ATOMIC_FLAG_INIT;

// Handles and process caches must retain the same allocator through final teardown.
extern "C" API int e2d_hb_memory(Allocate allocate, Resize resize, Release release) {
    if (!allocate || !resize || !release) return -1;
    while (registration.test_and_set(std::memory_order_acquire)) {}
    int result = 0;
    if (ready.load(std::memory_order_relaxed)) {
        if (allocate_cb != allocate || resize_cb != resize || release_cb != release) result = -1;
    } else {
        allocate_cb = allocate;
        resize_cb = resize;
        release_cb = release;
        ready.store(true, std::memory_order_release);
    }
    registration.clear(std::memory_order_release);
    return result;
}

extern "C" API void *e2d_ft_hb_malloc(size_t size) {
    return ready.load(std::memory_order_acquire) ? allocate_cb(size) : nullptr;
}
extern "C" API void *e2d_ft_hb_calloc(size_t count, size_t size) {
    if (count && size > std::numeric_limits<size_t>::max() / count) return nullptr;
    size *= count;
    void *value = e2d_ft_hb_malloc(size);
    if (value) std::memset(value, 0, size);
    return value;
}
extern "C" API void e2d_ft_hb_free(void *value) {
    if (value) release_cb(value);
}
extern "C" API void *e2d_ft_hb_realloc(void *value, size_t size) {
    if (!value) return e2d_ft_hb_malloc(size);
    if (!size) { e2d_ft_hb_free(value); return nullptr; }
    return resize_cb(value, size);
}
