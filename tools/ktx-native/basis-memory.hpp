#ifndef E2D_BASIS_CPP_MEMORY_H
#define E2D_BASIS_CPP_MEMORY_H
#include "memory.hpp"
#include <functional>
#include <map>
#include <queue>
#include <set>
#include <sstream>
#include <string>
#include <unordered_map>
#include <unordered_set>

namespace e2d_ktx {
using string = std::basic_string<char, std::char_traits<char>, Allocator<char>>;
using wstring = std::basic_string<wchar_t, std::char_traits<wchar_t>, Allocator<wchar_t>>;
using stringstream = std::basic_stringstream<char, std::char_traits<char>, Allocator<char>>;
using ostringstream = std::basic_ostringstream<char, std::char_traits<char>, Allocator<char>>;
template <typename K, typename V, typename Compare = std::less<K>>
using map = std::map<K, V, Compare, Allocator<std::pair<const K, V>>>;
template <typename K, typename V, typename Hash = std::hash<K>, typename Equal = std::equal_to<K>>
using unordered_map = std::unordered_map<K, V, Hash, Equal, Allocator<std::pair<const K, V>>>;
template <typename K, typename Compare = std::less<K>>
using set = std::set<K, Compare, Allocator<K>>;
template <typename K, typename Hash = std::hash<K>, typename Equal = std::equal_to<K>>
using unordered_set = std::unordered_set<K, Hash, Equal, Allocator<K>>;
template <typename T, typename Container = vector<T>, typename Compare = std::less<T>>
using priority_queue = std::priority_queue<T, Container, Compare>;

// The standard callable has no allocator-aware constructor in the selected language profile.
class Function {
    struct Operations {
        void (*invoke)(void *);
        void *(*copy)(const void *);
        void (*destroy)(void *);
    };
    const Operations *operations = nullptr;
    void *value = nullptr;
public:
    Function() = default;
    template <typename F, typename = typename std::enable_if<
        !std::is_same<typename std::decay<F>::type, Function>::value>::type>
    Function(F &&function) {
        using T = typename std::decay<F>::type;
        static const Operations table{
            [](void *p) { (*static_cast<T *>(p))(); },
            [](const void *p) -> void * { return make<T>(*static_cast<const T *>(p)).release(); },
            [](void *p) { ObjectFree<T>{}(static_cast<T *>(p)); }
        };
        value = make<T>(std::forward<F>(function)).release();
        operations = &table;
    }
    Function(const Function &other) : operations(other.operations),
        value(other.value ? other.operations->copy(other.value) : nullptr) { }
    Function(Function &&other) noexcept { swap(other); }
    ~Function() { if (value) operations->destroy(value); }
    Function &operator=(const Function &other) { Function copy(other); swap(copy); return *this; }
    Function &operator=(Function &&other) noexcept { Function moved(std::move(other)); swap(moved); return *this; }
    void swap(Function &other) noexcept { std::swap(value, other.value); std::swap(operations, other.operations); }
    void operator()() const { if (!value) throw std::bad_function_call(); operations->invoke(value); }
};
}
#endif
