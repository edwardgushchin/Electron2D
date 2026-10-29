#define _POSIX_C_SOURCE 200112L
#include <stdint.h>
#include <stdlib.h>
#include <stdatomic.h>
#include <errno.h>

static _Thread_local uint32_t remaining;
static void *fault_malloc(size_t size) {
    if (remaining && !--remaining) return NULL;
    return malloc(size);
}
#define malloc fault_malloc
#include "../enet-native/memory.c"
#undef malloc

static _Atomic uint64_t calls;
E2D_MEMORY_API void e2d_test_fail_after(uint32_t count) { remaining = count; }
E2D_MEMORY_API uint64_t e2d_test_calls(void) { return atomic_load(&calls); }
static uint64_t hash(const unsigned char *value, int length) {
    uint64_t result = UINT64_C(14695981039346656037);
    for (int i = 0; i < length; ++i) result = (result ^ value[i]) * UINT64_C(1099511628211);
    return result;
}
E2D_MEMORY_API int e2d_test_utf8(const unsigned char *a, int length_a, const unsigned char *b, int length_b,
    uint64_t expected, uint64_t *hash_a, uint64_t *hash_b) {
    atomic_fetch_add(&calls, 1);
    if ((a == NULL) != (length_a < 0) || (b == NULL) != (length_b < 0)) return -2;
    if ((a && a[length_a]) || (b && b[length_b])) return -3;
    *hash_a = a ? hash(a, length_a) : 0; *hash_b = b ? hash(b, length_b) : 0;
    if (expected == UINT64_MAX) return 1;
    E2DMemorySnapshot state; e2d_memory_snapshot(&state);
    return state.nativeCurrent == expected ? 1 : -1;
}
E2D_MEMORY_API int e2d_test_errno(void) { return ERANGE; }
E2D_MEMORY_API int e2d_test_error(const unsigned char *value) { (void)value; errno = ERANGE; return -1; }
