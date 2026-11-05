#pragma once
#include "../enet-native/memory.h"
#include <cstddef>
#include <deque>
#include <limits>
#include <memory>
#include <new>
#include <string>
#include <utility>
#include <vector>

namespace e2d_rtc {
template<class T> struct Allocator {
    using value_type = T;
    Allocator() = default;
    template<class U> Allocator(const Allocator<U>&) noexcept {}
    T* allocate(size_t count) {
        if (count > std::numeric_limits<size_t>::max() / sizeof(T)) throw std::bad_array_new_length();
        auto value = e2d_memory_allocate(count * sizeof(T), alignof(T), 0, nullptr);
        if (!value) throw std::bad_alloc();
        return static_cast<T*>(value);
    }
    void deallocate(T* value, size_t) noexcept { e2d_memory_free(value); }
};
template<class T, class U> bool operator==(Allocator<T>, Allocator<U>) noexcept { return true; }
template<class T, class U> bool operator!=(Allocator<T>, Allocator<U>) noexcept { return false; }
template<class T> using Vector = std::vector<T, Allocator<T>>;
template<class T> using Queue = std::deque<T, Allocator<T>>;
using String = std::basic_string<char, std::char_traits<char>, Allocator<char>>;
template<class T, class... Args> std::shared_ptr<T> shared(Args&&... args) {
    return std::allocate_shared<T>(Allocator<T>{}, std::forward<Args>(args)...);
}
struct Owned {
    static void* operator new(size_t size) {
        auto value = e2d_memory_allocate(size, alignof(std::max_align_t), 0, nullptr);
        if (!value) throw std::bad_alloc();
        return value;
    }
    static void operator delete(void* value) noexcept { e2d_memory_free(value); }
};
}
