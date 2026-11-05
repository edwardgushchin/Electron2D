#include "basisu.h"
#include "basisu_containers.h"
#include <cassert>
#include <cstdio>
#include <new>

struct element {
    static inline int live = 0, attempts = 0, fail_at = 0;
    int value = 0;
    static void construct() { if (++attempts == fail_at) throw std::bad_alloc(); }
    element() { construct(); ++live; }
    element(const element &other) : value(other.value) { construct(); ++live; }
    element(element &&other) noexcept : value(other.value) { ++live; }
    element &operator=(const element &other) { construct(); value = other.value; return *this; }
    ~element() { --live; }
};

int main() {
    int refusals = 0;
    {
        basisu::vector<element> source(3);
        for (int i = 0; i < 3; ++i) source[i].value = 100 + i;
        std::initializer_list<element> values{source[0], source[1], source[2]};
        const int baseline = element::live;
        for (int mode = 0; mode < 8; ++mode) {
            auto call = [&] {
                switch (mode) {
                case 0: { basisu::vector<element> value(3); break; }
                case 1: { basisu::vector<element> value(3, source[0]); break; }
                case 2: { basisu::vector<element> value(source); break; }
                case 3: { basisu::vector<element> value(values); break; }
                case 4: { basisu::vector<element> value(1); value = source; break; }
                case 5: {
                    basisu::vector<element> value(source);
                    assert(value.set(basisu::readable_span<element>(source.data(), 1)));
                    assert(value.size() == 1 && value[0].value == 100); break;
                }
                case 6: { basisu::vector<element> value(basisu::readable_span<element>(source.data(), 3)); break; }
                case 7: { basisu::vector<element> value(basisu::writable_span<element>(source.data(), 3)); break; }
                }
            };
            element::attempts = 0;
            call();
            const int requests = element::attempts;
            assert(element::live == baseline);
            for (int failure = 1; failure <= requests; ++failure) {
                element::attempts = 0; element::fail_at = failure;
                bool rejected = false;
                try { call(); } catch (const std::bad_alloc &) { rejected = true; }
                element::fail_at = 0;
                assert(rejected && element::live == baseline);
                for (int i = 0; i < 3; ++i) assert(source[i].value == 100 + i);
                call();
                assert(element::live == baseline);
                ++refusals;
            }
        }
    }
    assert(element::live == 0);
    std::printf("Basis containers: eight constructor/copy/shrink roles, %d refusals, complete element retirement and retry passed.\n", refusals);
}
