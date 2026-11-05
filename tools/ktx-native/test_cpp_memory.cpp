#include "memory.hpp"
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
static uint64_t current() { E2DMemorySnapshot value{}; e2d_memory_snapshot(&value); return value.nativeCurrent; }

int main() {
    auto baseline = current();
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
    std::puts("Codec C++ ownership: aligned object/array, nested constructor and partial vector-copy failure drain and retry passed.");
}
