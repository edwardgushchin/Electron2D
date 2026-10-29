/* Test-only backing allocator. Refusals are scoped to the calling thread. */
#define _POSIX_C_SOURCE 200112L
#include <stdlib.h>
#include <errno.h>
#include <stdio.h>
#include "../enet-native/memory.h"
static _Thread_local size_t attempts, fail_at;
E2D_MEMORY_API void e2d_test_arm(size_t value) { attempts = 0; fail_at = value; }
E2D_MEMORY_API size_t e2d_test_attempts(void) { return attempts; }
static int fail(void) { return ++attempts == fail_at; }
static void *test_malloc(size_t size) { return fail() ? NULL : malloc(size); }
static void *test_calloc(size_t count, size_t size) { return fail() ? NULL : calloc(count, size); }
static void *test_realloc(void *value, size_t size) { return fail() ? NULL : realloc(value, size); }
static int test_memalign(void **value, size_t alignment, size_t size) {
    return fail() ? ENOMEM : posix_memalign(value, alignment, size);
}
#define malloc test_malloc
#define calloc test_calloc
#define realloc test_realloc
#define posix_memalign test_memalign
#include "../enet-native/memory.c"
/* libcrypto depends on this library, so its destructor must retire first. */
__attribute__((destructor)) static void check_exit(void) {
    E2DMemorySnapshot state;
    e2d_memory_snapshot(&state);
    if (state.current || state.allocated != state.retired) {
        fprintf(stderr, "TLS native exit leak: %lu bytes\n", state.current);
        abort();
    }
    fprintf(stderr, "TLS native exit: all owned bytes retired.\n");
}
