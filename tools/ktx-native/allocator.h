#ifndef E2D_KTX_ALLOCATOR_H
#define E2D_KTX_ALLOCATOR_H
#include <stddef.h>
#ifdef __cplusplus
extern "C" {
#endif
void *e2d_ktx_malloc(size_t size);
void *e2d_ktx_calloc(size_t count, size_t size);
void *e2d_ktx_realloc(void *value, size_t size);
void e2d_ktx_free(void *value);
int e2d_ktx_posix_memalign(void **result, size_t alignment, size_t size);
void *e2d_ktx_aligned_malloc(size_t size, size_t alignment);
void e2d_ktx_aligned_free(void *value);
char *e2d_ktx_strdup(const char *value);
#ifdef __cplusplus
}
#endif
#endif
