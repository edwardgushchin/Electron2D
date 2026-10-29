#ifndef ELECTRON2D_MEMORY_H
#define ELECTRON2D_MEMORY_H
#include <stddef.h>
#include <stdint.h>
#if defined(_WIN32)
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
#ifdef __cplusplus
}
#endif
#endif
