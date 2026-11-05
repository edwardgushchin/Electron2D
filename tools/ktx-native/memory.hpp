#ifndef E2D_KTX_CPP_MEMORY_H
#define E2D_KTX_CPP_MEMORY_H
#include "allocator.h"
#include <limits>
#include <memory>
#include <new>
#include <type_traits>
#include <utility>
#include <vector>

namespace e2d_ktx {
template <typename T> T *storage(size_t count) {
    if (count > std::numeric_limits<size_t>::max() / sizeof(T)) throw std::bad_array_new_length();
    auto value = e2d_ktx_aligned_malloc(count * sizeof(T), alignof(T));
    if (!value) throw std::bad_alloc();
    return static_cast<T *>(value);
}

template <typename T> struct Allocator {
    using value_type = T;
    Allocator() = default;
    template <typename U> Allocator(const Allocator<U> &) noexcept { }
    T *allocate(size_t count) { return storage<T>(count); }
    void deallocate(T *value, size_t) noexcept { e2d_ktx_free(value); }
};
template <typename T, typename U> bool operator==(Allocator<T>, Allocator<U>) noexcept { return true; }
template <typename T, typename U> bool operator!=(Allocator<T>, Allocator<U>) noexcept { return false; }
template <typename T> using vector = std::vector<T, Allocator<T>>;

template <typename T> struct ObjectFree {
    void operator()(T *value) const noexcept {
        if (value) { value->~T(); e2d_ktx_free(value); }
    }
};
template <typename T, typename... Args> std::unique_ptr<T, ObjectFree<T>> make(Args &&...args) {
    auto value = storage<T>(1);
    try { ::new (static_cast<void *>(value)) T(std::forward<Args>(args)...); }
    catch (...) { e2d_ktx_free(value); throw; }
    return std::unique_ptr<T, ObjectFree<T>>(value);
}

template <typename T> struct ArrayFree {
    void operator()(T *value) const noexcept {
        static_assert(std::is_trivially_destructible<T>::value, "Array storage requires trivial destruction");
        e2d_ktx_free(const_cast<typename std::remove_const<T>::type *>(value));
    }
};
template <typename T> using Array = std::unique_ptr<T[], ArrayFree<T>>;
template <typename T> Array<T> make_array(size_t count) {
    Array<T> value(storage<T>(count));
    for (size_t i = 0; i < count; ++i) ::new (static_cast<void *>(value.get() + i)) T;
    return value;
}
}
#endif
