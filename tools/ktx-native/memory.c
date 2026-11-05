#include <errno.h>
#include "memory.h"

static void *checked(void *value) { if (!value) errno = ENOMEM; return value; }
void *e2d_ktx_malloc(size_t size) { return checked(e2d_memory_malloc(size)); }
void *e2d_ktx_calloc(size_t count, size_t size) { return checked(e2d_memory_calloc(count, size)); }
void *e2d_ktx_realloc(void *value, size_t size) { return checked(e2d_memory_realloc(value, size)); }
void e2d_ktx_free(void *value) { e2d_memory_free(value); }

int e2d_ktx_posix_memalign(void **result, size_t alignment, size_t size) {
    if (alignment < sizeof(void *) || (alignment & (alignment - 1))) return EINVAL;
    void *value = e2d_memory_allocate(size, alignment, 0, NULL);
    if (!value) return ENOMEM;
    *result = value;
    return 0;
}
