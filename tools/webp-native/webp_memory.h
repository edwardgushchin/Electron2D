#ifndef E2D_WEBP_MEMORY_H
#define E2D_WEBP_MEMORY_H
#include <stddef.h>
void *e2d_webp_allocate(size_t size);
void *e2d_webp_zero(size_t count, size_t size);
void e2d_webp_release(void *value);
int e2d_webp_memory_ready(void);
#endif
