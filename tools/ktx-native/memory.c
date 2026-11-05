#include <errno.h>
#include <string.h>
#include "memory.h"
#include "allocator.h"

static void *checked(void *value) { if (!value) errno = ENOMEM; return value; }
void *e2d_ktx_malloc(size_t size) { return checked(e2d_memory_malloc(size)); }
void *e2d_ktx_calloc(size_t count, size_t size) { return checked(e2d_memory_calloc(count, size)); }
void *e2d_ktx_realloc(void *value, size_t size) { return checked(e2d_memory_realloc(value, size)); }
void e2d_ktx_free(void *value) { e2d_memory_free(value); }
void *e2d_ktx_aligned_malloc(size_t size, size_t alignment) { return checked(e2d_memory_allocate(size, alignment, 0, NULL)); }
void e2d_ktx_aligned_free(void *value) { e2d_memory_free(value); }
char *e2d_ktx_strdup(const char *value) {
    size_t size = strlen(value) + 1;
    char *copy = e2d_ktx_malloc(size);
    if (copy) memcpy(copy, value, size);
    return copy;
}

int e2d_ktx_posix_memalign(void **result, size_t alignment, size_t size) {
    if (alignment < sizeof(void *) || (alignment & (alignment - 1))) return EINVAL;
    void *value = e2d_memory_allocate(size, alignment, 0, NULL);
    if (!value) return ENOMEM;
    *result = value;
    return 0;
}
