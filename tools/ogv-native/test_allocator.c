/* Test-only native backing allocator; one refusal position per worker thread. */
#include <stddef.h>
#include <stdlib.h>
#include "../enet-native/memory.h"
static _Thread_local size_t attempts, fail_at;
E2D_MEMORY_API void e2d_test_arm(size_t value) { attempts = 0; fail_at = value; }
E2D_MEMORY_API size_t e2d_test_attempts(void) { return attempts; }
static int fail(void) { return ++attempts == fail_at; }
static void *test_malloc(size_t size) { return fail() ? NULL : malloc(size); }
static void *test_calloc(size_t count, size_t size) { return fail() ? NULL : calloc(count, size); }
static void *test_realloc(void *value, size_t size) { return fail() ? NULL : realloc(value, size); }
#define malloc test_malloc
#define calloc test_calloc
#define realloc test_realloc
#include "../enet-native/memory.c"
