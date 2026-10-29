#ifndef ELECTRON2D_TLS_MEMORY_H
#define ELECTRON2D_TLS_MEMORY_H
#include "../enet-native/memory.h"
#include <errno.h>
#include <stdlib.h>
#include <string.h>
#include <sys/mman.h>

static inline char *e2d_ssl_strdup(const char *text)
{
    size_t size = strlen(text) + 1;
    char *copy = e2d_memory_malloc(size);
    if (copy) memcpy(copy, text, size);
    return copy;
}

static inline int e2d_ssl_memalign(void **result, size_t alignment, size_t size)
{
    int32_t error;
    void *value = e2d_memory_allocate(size, alignment, 0, &error);
    if (!value) return error == 1 ? EINVAL : ENOMEM;
    *result = value;
    return 0;
}

static inline void *e2d_ssl_mmap(void *address, size_t size, int protection,
                               int flags, int descriptor, off_t offset)
{
    E2DMemorySnapshot state;
    e2d_memory_lock();
    e2d_memory_snapshot_locked(&state);
    if ((uint64_t)size > INT64_MAX - state.current) {
        if (e2d_memory_ownership_locked(0, size) != -1) abort();
        e2d_memory_unlock();
        errno = ENOMEM;
        return MAP_FAILED;
    }
    void *value = mmap(address, size, protection, flags, descriptor, offset);
    if (value == MAP_FAILED) e2d_memory_failure_locked();
    else if (e2d_memory_ownership_locked(0, size)) abort();
    e2d_memory_unlock();
    return value;
}

static inline int e2d_ssl_munmap(void *address, size_t size)
{
    e2d_memory_lock();
    int result = munmap(address, size);
    if (!result && e2d_memory_ownership_locked(size, 0)) abort();
    e2d_memory_unlock();
    return result;
}
#endif
