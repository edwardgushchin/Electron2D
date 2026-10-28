#include <stdbool.h>
#include <stdint.h>
#include <string.h>

static bool timestamp_ns(uint64_t ticks, uint32_t bits, float period, uint64_t *result)
{
    _Static_assert(sizeof(float) == sizeof(uint32_t), "Vulkan timestamp period is binary32");
    uint32_t word; memcpy(&word, &period, sizeof(word));
    uint32_t exponent = (word >> 23) & 255, mantissa = word & 8388607;
    if (!bits || bits > 64 || (word >> 31) || exponent == 255 || (!exponent && !mantissa)) return false;
    if (bits != 64) ticks &= (UINT64_C(1) << bits) - 1;
    if (!ticks) { *result = 0; return true; }
    int shift = exponent ? (int)exponent - 150 : -149;
    if (exponent) mantissa |= 8388608;
    // Keep the complete 88-bit product; double loses low timestamp bits on some native targets.
    uint64_t low = (ticks & UINT32_MAX) * mantissa;
    uint64_t upper = (ticks >> 32) * mantissa + (low >> 32);
    low = (upper << 32) | (low & UINT32_MAX);
    uint64_t high = upper >> 32;
    if (shift >= 0) {
        if (high || shift >= 64 || low > (UINT64_MAX >> shift)) return false;
        *result = low << shift;
    } else {
        uint32_t right = (uint32_t)-shift;
        if (right < 64) {
            if ((high >> right) != 0) return false;
            *result = (low >> right) | (high << (64 - right));
        } else if (right == 64) *result = high;
        else *result = right >= 128 ? 0 : high >> (right - 64);
    }
    return true;
}
