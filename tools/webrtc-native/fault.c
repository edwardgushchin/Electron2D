/* Calling-thread refusal leaves asynchronous protocol workers independent. */
#define _POSIX_C_SOURCE 200112L
#include <errno.h>
#include <stdlib.h>
static _Thread_local size_t attempts, fail_at;
void e2d_test_arm(size_t value) { attempts=0; fail_at=value; }
size_t e2d_test_attempts(void) { return attempts; }
static int fail(void) { return ++attempts==fail_at; }
static void* test_malloc(size_t size) { return fail()?NULL:malloc(size); }
static void* test_calloc(size_t count,size_t size) { return fail()?NULL:calloc(count,size); }
static void* test_realloc(void* value,size_t size) { return fail()?NULL:realloc(value,size); }
static int test_aligned(void** value,size_t alignment,size_t size) { return fail()?ENOMEM:posix_memalign(value,alignment,size); }
#define malloc test_malloc
#define calloc test_calloc
#define realloc test_realloc
#define posix_memalign test_aligned
#include "../enet-native/memory.c"
