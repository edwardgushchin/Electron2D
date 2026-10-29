#ifndef ELECTRON2D_MEMORY_H
#define ELECTRON2D_MEMORY_H
#include <stddef.h>
#include <stdint.h>
#include "memory-functions.h"
#if defined(_WIN32) && defined(E2D_MEMORY_IMPORT)
#define E2D_MEMORY_API __declspec(dllimport)
#elif defined(_WIN32)
#define E2D_MEMORY_API __declspec(dllexport)
#else
#define E2D_MEMORY_API __attribute__((visibility("default")))
#endif
#ifdef __cplusplus
extern "C" {
#endif
typedef struct {
    uint64_t managedCurrent, managedPeak, nativeCurrent, nativePeak, current, peak;
    uint64_t allocated, retired, allocations, resizes, frees, failures;
} E2DMemorySnapshot;
E2D_MEMORY_API uint32_t e2d_memory_abi(void);
E2D_MEMORY_API void *e2d_memory_allocate(size_t size, size_t alignment, uint32_t zero, int32_t *error);
E2D_MEMORY_API void *e2d_memory_reallocate(void *value, size_t size, int32_t *error);
E2D_MEMORY_API void e2d_memory_release(void *value);
E2D_MEMORY_API void e2d_memory_snapshot(E2DMemorySnapshot *result);
E2D_MEMORY_API void e2d_memory_lock(void);
E2D_MEMORY_API void e2d_memory_unlock(void);
// Runtime producers already hold the gate across their actual ownership commit.
E2D_MEMORY_API int e2d_memory_managed_locked(uint64_t current);
E2D_MEMORY_API void e2d_memory_snapshot_locked(E2DMemorySnapshot *result);
// Native library defaults must outlive managed execution and keep the same owner.
E2D_MEMORY_API void *e2d_memory_malloc(size_t size);
E2D_MEMORY_API void *e2d_memory_calloc(size_t count, size_t size);
E2D_MEMORY_API void *e2d_memory_realloc(void *value, size_t size);
E2D_MEMORY_API void e2d_memory_free(void *value);
E2D_MEMORY_API void e2d_memory_functions(E2DMemoryFunctions *result);
E2D_MEMORY_API uint64_t e2d_memory_failure_epoch(void);
#ifdef __cplusplus
}
#endif
#endif
