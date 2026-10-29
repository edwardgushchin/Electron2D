#pragma once
#include "../../enet-native/memory.h"
#include <stdint.h>

static void *e2d_compression_allocate(size_t size) {
    int32_t error;
    if (e2d_memory_abi() != 1) return NULL;
    return e2d_memory_allocate(size, 0, 0, &error);
}
static void e2d_compression_release(void *value) { e2d_memory_release(value); }
static void *e2d_compression_zero(size_t count, size_t size) {
    int32_t error;
    if (e2d_memory_abi() != 1 || (count && size > SIZE_MAX / count)) return NULL;
#ifdef _WIN64
    const size_t alignment = 64;
#else
    const size_t alignment = 0;
#endif
    return e2d_memory_allocate(count * size, alignment, 1, &error);
}
static void *e2d_compression_zalloc(void *opaque, unsigned count, unsigned size) {
    (void)opaque; return e2d_compression_zero(count, size);
}
static void e2d_compression_zfree(void *opaque, void *value) {
    (void)opaque; e2d_compression_release(value);
}
