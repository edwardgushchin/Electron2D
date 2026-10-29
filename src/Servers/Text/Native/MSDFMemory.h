#pragma once
#include "TextMemory.h"
#include <limits>
#include <new>
#include <queue>
#include <type_traits>
#include <vector>

namespace e2d_msdf {
inline void *allocate(size_t size) {
    auto value = e2d_text_allocate(size, nullptr);
    if (!value) throw std::bad_alloc();
    return value;
}
inline void release(void *value) noexcept { e2d_text_release(value, nullptr); }
template <typename T> T *storage(size_t count) {
    static_assert(alignof(T) <= alignof(std::max_align_t), "MSDF storage exceeds native allocator alignment");
    if (count > std::numeric_limits<size_t>::max() / sizeof(T)) throw std::bad_array_new_length();
    return static_cast<T *>(allocate(count * sizeof(T)));
}
template <typename T> T *array(size_t count) {
    static_assert(std::is_trivial<T>::value, "MSDF pixel storage must be trivial");
    return storage<T>(count);
}
template <typename T> struct Allocator {
    using value_type = T;
    Allocator() = default;
    template <typename U> Allocator(const Allocator<U> &) noexcept { }
    T *allocate(size_t count) { return storage<T>(count); }
    void deallocate(T *value, size_t) noexcept { release(value); }
};
template <typename T, typename U> bool operator==(Allocator<T>, Allocator<U>) noexcept { return true; }
template <typename T, typename U> bool operator!=(Allocator<T>, Allocator<U>) noexcept { return false; }
template <typename T> using vector = std::vector<T, Allocator<T>>;
template <typename T> using queue = std::queue<T, std::deque<T, Allocator<T>>>;
struct EdgeMemory {
    static void *operator new(size_t size) { return allocate(size); }
    static void operator delete(void *value) noexcept { release(value); }
};
// Split functions publish raw edges incrementally; keep each part owned until adoption.
template <typename T, size_t N> struct Parts {
    T *values[N]{};
    Parts() = default;
    Parts(const Parts &) = delete;
    Parts &operator=(const Parts &) = delete;
    ~Parts() { for (auto value : values) delete value; }
    T *&operator[](size_t index) { return values[index]; }
    T *take(size_t index) { auto value = values[index]; values[index] = nullptr; return value; }
};
}
