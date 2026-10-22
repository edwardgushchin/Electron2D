#include <cstdint>

// Deliberately invalid samples exercise calibration failure and helper retirement.
extern "C" int context_initialize(const void*) { return 0; }
extern "C" void context_snapshot(uint64_t *output) {
    for (unsigned i = 0; i < 5; i++) output[i] = 0;
}
