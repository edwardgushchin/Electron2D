#ifndef _POSIX_C_SOURCE
#define _POSIX_C_SOURCE 200112L
#endif
#include "memory.h"
#include <stdlib.h>
#include <string.h>
#if defined(_WIN32)
#include <windows.h>
#include <malloc.h>
static SRWLOCK gate = SRWLOCK_INIT;
void e2d_memory_lock(void) { AcquireSRWLockExclusive(&gate); }
void e2d_memory_unlock(void) { ReleaseSRWLockExclusive(&gate); }
#else
#include <pthread.h>
static pthread_mutex_t gate = PTHREAD_MUTEX_INITIALIZER;
void e2d_memory_lock(void) { if (pthread_mutex_lock(&gate)) abort(); }
void e2d_memory_unlock(void) { if (pthread_mutex_unlock(&gate)) abort(); }
#endif
typedef struct { void *allocation; size_t size, alignment; } Header;
_Static_assert(sizeof(Header) <= 32, "Private allocation prefix overflow");
_Static_assert(sizeof(E2DMemorySnapshot) == 96, "Memory snapshot ABI mismatch");
static E2DMemorySnapshot totals;
#if defined(_WIN32)
static __declspec(thread) uint64_t failure_epoch;
#else
static _Thread_local uint64_t failure_epoch;
#endif
uint64_t e2d_memory_failure_epoch(void) { return failure_epoch; }
uint32_t e2d_memory_abi(void) { return 1; }
static uint64_t add(uint64_t a, uint64_t b) { if (a > UINT64_MAX - b) abort(); return a + b; }
static void publish(void) {
    totals.current = totals.managedCurrent + totals.nativeCurrent;
    if (totals.current > totals.peak) totals.peak = totals.current;
}
int e2d_memory_managed_locked(uint64_t current) {
    if (current > INT64_MAX - totals.nativeCurrent) return -1;
    totals.managedCurrent = current;
    if (current > totals.managedPeak) totals.managedPeak = current;
    publish(); return 0;
}
void e2d_memory_snapshot_locked(E2DMemorySnapshot *result) {
    if (!result) abort();
    *result = totals;
}
void e2d_memory_snapshot(E2DMemorySnapshot *result) {
    e2d_memory_lock(); e2d_memory_snapshot_locked(result); e2d_memory_unlock();
}
static int request(size_t size, size_t prefix, size_t previous, size_t alignment, size_t *bytes) {
    if ((uint64_t)size > INT64_MAX - (totals.current - previous) || size > SIZE_MAX - prefix) return 0;
    *bytes = size + prefix;
    if (alignment > 1) {
        if (*bytes > SIZE_MAX - (alignment - 1)) return 0;
        *bytes = (*bytes + alignment - 1) & ~(alignment - 1);
    }
    return 1;
}
static void commit(size_t previous, size_t size) {
    if (size >= previous) totals.allocated = add(totals.allocated, size - previous);
    else totals.retired = add(totals.retired, previous - size);
    totals.nativeCurrent = totals.nativeCurrent - previous + size;
    if (totals.nativeCurrent > totals.nativePeak) totals.nativePeak = totals.nativeCurrent;
    publish();
}
static void failed(int32_t *error) { if (error) *error = 2; ++totals.failures; ++failure_epoch; }
void e2d_memory_failure_locked(void) { failed(NULL); }
int e2d_memory_ownership_locked(size_t previous, size_t size) {
    if (previous > totals.nativeCurrent) abort();
    if ((uint64_t)size > INT64_MAX - (totals.current - previous)) { failed(NULL); return -1; }
    commit(previous, size);
    if (!previous) ++totals.allocations;
    else if (!size) ++totals.frees;
    else ++totals.resizes;
    return 0;
}
void *e2d_memory_allocate(size_t size, size_t alignment, uint32_t zero, int32_t *error) {
    if (error) *error = 0;
    if ((alignment && (alignment & (alignment - 1))) || zero > 1) { if (error) *error = 1; return NULL; }
    e2d_memory_lock();
    size_t prefix = alignment > 32 ? alignment : 32, bytes;
    void *raw = NULL;
    if (request(size, prefix, 0, alignment, &bytes)) {
        if (alignment) {
            size_t actual = alignment < sizeof(void*) ? sizeof(void*) : alignment;
#if defined(_WIN32)
            raw = _aligned_malloc(bytes, actual);
#else
            if (posix_memalign(&raw, actual, bytes)) raw = NULL;
#endif
            if (raw && zero) memset(raw, 0, bytes);
        } else raw = zero ? calloc(1, bytes) : malloc(bytes);
    }
    if (!raw) { failed(error); e2d_memory_unlock(); return NULL; }
    char *value = (char*)raw + prefix;
    *(Header*)(value - 32) = (Header){raw, size, alignment};
    commit(0, size); ++totals.allocations;
    e2d_memory_unlock(); return value;
}
void *e2d_memory_reallocate(void *value, size_t size, int32_t *error) {
    if (!value) return e2d_memory_allocate(size, 0, 0, error);
    if (error) *error = 0;
    e2d_memory_lock();
    Header *header = (Header*)((char*)value - 32);
    if (header->alignment) { if (error) *error = 1; e2d_memory_unlock(); return NULL; }
    size_t previous = header->size, bytes;
    void *raw = request(size, 32, previous, 0, &bytes) ? realloc(header->allocation, bytes) : NULL;
    if (!raw) { failed(error); e2d_memory_unlock(); return NULL; }
    char *result = (char*)raw + 32;
    *(Header*)(result - 32) = (Header){raw, size, 0};
    commit(previous, size); ++totals.resizes;
    e2d_memory_unlock(); return result;
}
void e2d_memory_release(void *value) {
    if (!value) return;
    e2d_memory_lock();
    Header *header = (Header*)((char*)value - 32);
    size_t size = header->size;
#if defined(_WIN32)
    if (header->alignment) _aligned_free(header->allocation); else free(header->allocation);
#else
    free(header->allocation);
#endif
    commit(size, 0); ++totals.frees;
    e2d_memory_unlock();
}
void *e2d_memory_malloc(size_t size) { return e2d_memory_allocate(size, 0, 0, NULL); }
void *e2d_memory_calloc(size_t count, size_t size) {
    // Reject multiplication before forwarding it; failures still reach the common ledger.
    if (size && count > SIZE_MAX / size) return e2d_memory_allocate(SIZE_MAX, 0, 1, NULL);
    return e2d_memory_allocate(count * size, 0, 1, NULL);
}
void *e2d_memory_realloc(void *value, size_t size) { return e2d_memory_reallocate(value, size, NULL); }
void e2d_memory_free(void *value) { e2d_memory_release(value); }
void e2d_memory_functions(E2DMemoryFunctions *result) {
    if (!result) abort();
    *result = (E2DMemoryFunctions){e2d_memory_malloc, e2d_memory_calloc, e2d_memory_realloc, e2d_memory_free};
}
