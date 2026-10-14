#define _GNU_SOURCE
#include <assert.h>
#include <dlfcn.h>
#include <errno.h>
#include <pthread.h>
#include <semaphore.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

typedef struct { uint64_t allocations, requested_bytes, frees, reallocations, failures; } allocation_stats;
static sem_t start, done;
static void *worker(void *unused)
{
    (void)unused; assert(!sem_wait(&start));
    void *p = malloc(31); assert(p); ((volatile unsigned char *)p)[0] = 1; free(p);
    assert(!sem_post(&done)); return NULL;
}
int main(void)
{
    int (*attached)(void) = dlsym(RTLD_DEFAULT, "electron2d_native_alloc_attached");
    int (*begin)(void) = dlsym(RTLD_DEFAULT, "electron2d_native_alloc_begin");
    int (*end)(allocation_stats *) = dlsym(RTLD_DEFAULT, "electron2d_native_alloc_end");
    void (*smoke)(void) = dlsym(RTLD_DEFAULT, "electron2d_native_alloc_smoke");
    assert(attached && begin && end && smoke && attached());
    smoke(); allocation_stats stats;
    assert(!begin()); assert(begin() == -1); smoke(); assert(!end(&stats));
    assert(stats.allocations == 4 && stats.requested_bytes == 131 && stats.frees == 3 && stats.reallocations == 1 && !stats.failures);
    assert(end(&stats) == -1);
    assert(!sem_init(&start, 0, 0) && !sem_init(&done, 0, 0));
    pthread_t thread; assert(!pthread_create(&thread, NULL, worker, NULL));
    assert(!begin()); assert(!sem_post(&start)); assert(!sem_wait(&done)); assert(!end(&stats));
    assert(stats.allocations == 1 && stats.requested_bytes == 31 && stats.frees == 1 && !stats.failures);
    assert(!pthread_join(thread, NULL));
    void *p = malloc(17); assert(p);
    assert(!begin()); errno = 0;
    volatile size_t impossible = SIZE_MAX;
    void *failed = realloc(p, impossible); assert(!failed && errno == ENOMEM);
    assert(!end(&stats)); assert(stats.failures == 1 && stats.reallocations == 1 && !stats.allocations && !stats.requested_bytes);
    free(p);
    p = malloc(17); assert(p);
    assert(!begin()); void *zero = realloc(p, 0); assert(!zero); assert(!end(&stats));
    assert(stats.reallocations == 1 && stats.frees == 1 && !stats.allocations && !stats.requested_bytes && !stats.failures);
    sem_destroy(&start); sem_destroy(&done);
    puts("Native allocator calibration: 4/131/3, worker 1/31/1, nested/stopped windows, failed and zero-size realloc passed.");
}
