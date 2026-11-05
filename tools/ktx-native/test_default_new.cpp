#include <cstdio>
#include <cstdlib>
#include <new>
#ifdef _WIN32
#include <malloc.h>
#endif

namespace {
// The bridge executes codec work on the calling thread (job_pool(1)). Separate
// windows let concurrent tests allocate their own harness storage outside calls.
thread_local bool active = false, calibrated = false;
thread_local size_t calls = 0;
void *allocate(size_t size, size_t alignment = 0) {
    if (active) ++calls;
    if (!size) size = 1;
    for (;;) {
        void *result = nullptr;
        if (!alignment) result = std::malloc(size);
#ifdef _WIN32
        else result = _aligned_malloc(size, alignment);
#else
        else if (posix_memalign(&result, alignment, size) != 0) result = nullptr;
#endif
        if (result) return result;
        auto handler = std::get_new_handler();
        if (!handler) throw std::bad_alloc();
        handler();
    }
}
void aligned_free(void *pointer) noexcept {
#ifdef _WIN32
    _aligned_free(pointer);
#else
    std::free(pointer);
#endif
}
}
void *operator new(size_t size) { return allocate(size); }
void *operator new[](size_t size) { return allocate(size); }
void operator delete(void *pointer) noexcept { std::free(pointer); }
void operator delete[](void *pointer) noexcept { std::free(pointer); }
void operator delete(void *pointer, size_t) noexcept { std::free(pointer); }
void operator delete[](void *pointer, size_t) noexcept { std::free(pointer); }
void *operator new(size_t size, std::align_val_t alignment) { return allocate(size, static_cast<size_t>(alignment)); }
void *operator new[](size_t size, std::align_val_t alignment) { return allocate(size, static_cast<size_t>(alignment)); }
void operator delete(void *pointer, std::align_val_t) noexcept { aligned_free(pointer); }
void operator delete[](void *pointer, std::align_val_t) noexcept { aligned_free(pointer); }
void operator delete(void *pointer, size_t, std::align_val_t) noexcept { aligned_free(pointer); }
void operator delete[](void *pointer, size_t, std::align_val_t) noexcept { aligned_free(pointer); }
void *operator new(size_t size, const std::nothrow_t &) noexcept { try { return ::operator new(size); } catch (...) { return nullptr; } }
void *operator new[](size_t size, const std::nothrow_t &) noexcept { try { return ::operator new[](size); } catch (...) { return nullptr; } }
void operator delete(void *pointer, const std::nothrow_t &) noexcept { std::free(pointer); }
void operator delete[](void *pointer, const std::nothrow_t &) noexcept { std::free(pointer); }
void *operator new(size_t size, std::align_val_t alignment, const std::nothrow_t &) noexcept { try { return ::operator new(size, alignment); } catch (...) { return nullptr; } }
void *operator new[](size_t size, std::align_val_t alignment, const std::nothrow_t &) noexcept { try { return ::operator new[](size, alignment); } catch (...) { return nullptr; } }
void operator delete(void *pointer, std::align_val_t, const std::nothrow_t &) noexcept { aligned_free(pointer); }
void operator delete[](void *pointer, std::align_val_t, const std::nothrow_t &) noexcept { aligned_free(pointer); }

namespace e2d_test {
void begin() {
    if (active) std::_Exit(EXIT_FAILURE);
    active = true; calls = 0;
    if (!calibrated) {
        // Volatile function pointers prevent allocation elision in optimized builds.
        void *(*volatile scalar)(size_t) = ::operator new;
        void *(*volatile array)(size_t) = ::operator new[];
        void *(*volatile aligned)(size_t, std::align_val_t) = ::operator new;
        void *(*volatile aligned_array)(size_t, std::align_val_t) = ::operator new[];
        void *(*volatile quiet)(size_t, const std::nothrow_t &) noexcept = ::operator new;
        void *(*volatile quiet_array)(size_t, const std::nothrow_t &) noexcept = ::operator new[];
        void *(*volatile quiet_aligned)(size_t, std::align_val_t, const std::nothrow_t &) noexcept = ::operator new;
        void *(*volatile quiet_aligned_array)(size_t, std::align_val_t, const std::nothrow_t &) noexcept = ::operator new[];
        ::operator delete(scalar(0)); ::operator delete[](array(19));
        ::operator delete(aligned(128, std::align_val_t(128)), std::align_val_t(128));
        ::operator delete[](aligned_array(256, std::align_val_t(128)), std::align_val_t(128));
        ::operator delete(quiet(17, std::nothrow)); ::operator delete[](quiet_array(19, std::nothrow));
        ::operator delete(quiet_aligned(128, std::align_val_t(128), std::nothrow), std::align_val_t(128));
        ::operator delete[](quiet_aligned_array(256, std::align_val_t(128), std::nothrow), std::align_val_t(128));
        if (calls != 8) {
            active = false;
            std::fprintf(stderr, "C++ allocation calibration: %zu calls, expected 8\n", calls);
            std::fflush(stderr); std::_Exit(EXIT_FAILURE);
        }
        calls = 0; calibrated = true;
    }
}
void end(const char *operation) {
    active = false;
    if (calls) {
        std::fprintf(stderr, "%s: %zu untracked default C++ allocations\n", operation, calls);
        std::fflush(stderr); std::_Exit(EXIT_FAILURE);
    }
}
}
