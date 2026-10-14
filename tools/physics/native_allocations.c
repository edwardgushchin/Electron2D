#define _GNU_SOURCE
#include <dlfcn.h>
#include <errno.h>
#include <stdint.h>
#include <stdatomic.h>
#include <stdlib.h>

#ifndef __GLIBC__
#error This test-only allocator probe requires Linux glibc.
#endif

extern void *__libc_malloc(size_t);
extern void *__libc_calloc(size_t, size_t);
extern void *__libc_realloc(void *, size_t);
extern void __libc_free(void *);
extern void *__libc_memalign(size_t, size_t);

typedef struct
{
    uint64_t allocations, requested_bytes, frees, reallocations, failures;
} allocation_stats;

static atomic_flag gate = ATOMIC_FLAG_INIT;
static atomic_bool recording;
static allocation_stats totals;

static void lock(void) { while (atomic_flag_test_and_set_explicit(&gate, memory_order_acquire)) {} }
static void unlock(void) { atomic_flag_clear_explicit(&gate, memory_order_release); }
static void allocation(void *pointer, size_t bytes, int resized)
{
    if (!atomic_load_explicit(&recording, memory_order_relaxed)) return;
    lock();
    if (atomic_load_explicit(&recording, memory_order_relaxed))
    {
        totals.reallocations += resized;
        if (pointer) { totals.allocations++; totals.requested_bytes += bytes; }
        else if (!resized || bytes) totals.failures++;
    }
    unlock();
}
static void released(void *pointer)
{
    if (!pointer || !atomic_load_explicit(&recording, memory_order_relaxed)) return;
    lock();
    if (atomic_load_explicit(&recording, memory_order_relaxed)) totals.frees++;
    unlock();
}

void *malloc(size_t size) { void *p = __libc_malloc(size); allocation(p, size, 0); return p; }
void *calloc(size_t count, size_t size)
{
    void *p = __libc_calloc(count, size);
    allocation(p, p ? count * size : 0, 0); return p;
}
void *realloc(void *pointer, size_t size)
{
    void *p = __libc_realloc(pointer, size);
    if (!size && pointer) released(pointer);
    allocation(p, size, 1);
    return p;
}
void free(void *pointer) { released(pointer); __libc_free(pointer); }
void *memalign(size_t alignment, size_t size) { void *p = __libc_memalign(alignment, size); allocation(p, size, 0); return p; }
void *aligned_alloc(size_t alignment, size_t size)
{
    if (!alignment || (alignment & (alignment - 1))) { errno = EINVAL; allocation(NULL, 0, 0); return NULL; }
    return memalign(alignment, size);
}
int posix_memalign(void **result, size_t alignment, size_t size)
{
    if (alignment < sizeof(void *) || (alignment & (alignment - 1))) { allocation(NULL, 0, 0); return EINVAL; }
    int saved = errno;
    void *p = __libc_memalign(alignment, size);
    errno = saved; allocation(p, size, 0);
    if (!p) return ENOMEM;
    *result = p; return 0;
}

int electron2d_native_alloc_attached(void)
{
    Dl_info allocator, probe;
    return dladdr(dlsym(RTLD_DEFAULT, "malloc"), &allocator) &&
        dladdr((void *)&electron2d_native_alloc_attached, &probe) && allocator.dli_fbase == probe.dli_fbase;
}
size_t electron2d_native_alloc_size(void) { return sizeof(allocation_stats); }
int electron2d_native_alloc_begin(void)
{
    lock();
    if (atomic_load_explicit(&recording, memory_order_relaxed)) { unlock(); return -1; }
    totals = (allocation_stats){0}; atomic_store_explicit(&recording, 1, memory_order_relaxed);
    unlock(); return 0;
}
int electron2d_native_alloc_end(allocation_stats *result)
{
    lock();
    if (!atomic_load_explicit(&recording, memory_order_relaxed)) { unlock(); return -1; }
    atomic_store_explicit(&recording, 0, memory_order_relaxed); *result = totals;
    unlock(); return 0;
}
void electron2d_native_alloc_smoke(void)
{
    void *p = malloc(17), *q = calloc(3, 7), *aligned = NULL;
    p = realloc(p, 29);
    if (!p || !q || posix_memalign(&aligned, 64, 64)) abort();
    ((volatile unsigned char *)p)[0] = 1;
    free(p); free(q); free(aligned);
}
