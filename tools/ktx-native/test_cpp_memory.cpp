#include "basis-memory.hpp"
#include "encoder/basisu_comp.h"
#include "memory.h"
#include <cassert>
#include <cstdint>
#include <cstdio>

struct failure { };
struct alignas(128) element {
    static int live, fail;
    int value;
    explicit element(int value = 0) : value(value) { if (fail-- == 1) throw failure{}; ++live; }
    element(const element &other) : element(other.value) { }
    ~element() { --live; }
};
int element::live, element::fail;
struct owner {
    e2d_ktx::vector<element> values;
    owner() { values.emplace_back(1); throw failure{}; }
};
struct alignas(128) array_element {
    static int remaining;
    array_element() { if (--remaining == 0) throw failure{}; }
};
int array_element::remaining;
struct callback {
    static int live;
    static bool fail;
    int calls = 0;
    int *result;
    explicit callback(int &result) : result(&result) { ++live; }
    callback(const callback &other) : calls(other.calls), result(other.result) { if (fail) throw failure{}; ++live; }
    ~callback() { --live; }
    void operator()() { *result = ++calls; }
};
int callback::live;
bool callback::fail;
static uint64_t current() { E2DMemorySnapshot value{}; e2d_memory_snapshot(&value); return value.nativeCurrent; }

int main() {
    { basisu::basis_compressor_params warm; }
    auto baseline = current();
    {
        basisu::basis_compressor_params params;
        auto &options = params.m_astc_hdr_6x6_options;
        assert(current() >= baseline + options.m_debug_image_prefix.capacity() + options.m_output_image_prefix.capacity() + 2);
    }
    assert(current() == baseline);
    {
        auto value = e2d_ktx::make<element>(7);
        assert(value->value == 7 && reinterpret_cast<uintptr_t>(value.get()) % 128 == 0);
        assert(current() == baseline + sizeof(element));
    }
    assert(element::live == 0 && current() == baseline);
    try { auto value = e2d_ktx::make<owner>(); assert(false); }
    catch (const failure &) { }
    assert(element::live == 0 && current() == baseline);
    {
        e2d_ktx::vector<element> values;
        values.emplace_back(11); values.emplace_back(19);
        auto live = current(); element::fail = 2;
        try { auto copy = values; assert(false); }
        catch (const failure &) { }
        assert(element::live == 2 && current() == live && values[0].value == 11 && values[1].value == 19);
    }
    assert(element::live == 0 && current() == baseline);
    array_element::remaining = 3;
    try { auto values = e2d_ktx::make_array<array_element>(8); assert(false); }
    catch (const failure &) { }
    assert(current() == baseline);
    array_element::remaining = 9;
    {
        auto values = e2d_ktx::make_array<array_element>(8);
        assert(reinterpret_cast<uintptr_t>(values.get()) % 128 == 0 && current() == baseline + 8 * sizeof(array_element));
    }
    assert(current() == baseline);
    {
        e2d_ktx::string text(4096, 'x');
        e2d_ktx::map<int, e2d_ktx::string> ordered;
        e2d_ktx::unordered_map<int, e2d_ktx::string> hashed;
        e2d_ktx::unordered_set<int> unique;
        e2d_ktx::priority_queue<int> queue;
        e2d_ktx::stringstream stream;
        for (int i = 0; i < 32; ++i) { ordered[i] = text; hashed[i] = text; unique.insert(i); queue.push(i); }
        stream << text;
        assert(ordered[7] == text && hashed[7] == text && unique.size() == 32 && stream.str() == text && queue.top() == 31);
        assert(current() >= baseline + 66 * text.size());
    }
    assert(current() == baseline);
    {
        int result = 0;
        callback original(result);
        e2d_ktx::Function first(original);
        first(); assert(result == 1);
        e2d_ktx::Function second(first);
        second(); second(); assert(result == 3);
        first(); assert(result == 2);
        auto live = current(); callback::fail = true;
        try { second = first; assert(false); }
        catch (const failure &) { }
        assert(current() == live && callback::live == 3);
        second(); assert(result == 4);
        callback::fail = false; second = first; second(); assert(result == 3);
        e2d_ktx::Function moved(std::move(second)); moved(); assert(result == 4);
        try { second(); assert(false); }
        catch (const std::bad_function_call &) { }
    }
    assert(current() == baseline && callback::live == 0);
    std::puts("Codec C++ ownership: aligned object/array, nested constructor and partial vector-copy failure drain and retry passed.");
    std::puts("Basis standard storage: string/stream/map/hash/queue accounting, callable value copies, failed assignment and move retirement passed.");
}
