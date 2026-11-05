/* Test-only backing refusal, including the ledger's aligned allocation route. */
#define _POSIX_C_SOURCE 200112L
#include <errno.h>
#include <stdlib.h>
static int test_aligned(void **result, size_t alignment, size_t size);
#define posix_memalign test_aligned
#include "../runtime-native/compression/fault.c"
#undef posix_memalign
static int test_aligned(void **result, size_t alignment, size_t size) {
    return fail() ? ENOMEM : posix_memalign(result, alignment, size);
}
