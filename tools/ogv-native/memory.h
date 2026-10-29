#ifndef E2D_OGV_MEMORY_H
#define E2D_OGV_MEMORY_H
#include "../enet-native/memory-functions.h"
int e2d_ogv_memory_ready(void);
void *e2d_ogv_allocate(size_t size);
void *e2d_ogv_zero(size_t count, size_t size);
void *e2d_ogv_resize(void *value, size_t size);
void e2d_ogv_release(void *value);
#endif
