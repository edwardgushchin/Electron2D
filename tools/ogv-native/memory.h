#ifndef E2D_OGV_MEMORY_H
#define E2D_OGV_MEMORY_H
#include "../enet-native/memory-functions.h"
#include <stdint.h>
#include <setjmp.h>
#include <fenv.h>
typedef struct {
    void **slots;
    size_t count, capacity;
    int failed, active;
} E2DOGVOwner;
/* Only pure-C codec frames may jump; caller callbacks suspend this frame. */
typedef struct E2DOGVScope {
    E2DOGVOwner *owner;
    struct E2DOGVScope *previous;
    jmp_buf jump;
    fenv_t environment;
} E2DOGVScope;
int e2d_ogv_scope_enter(E2DOGVScope *scope, E2DOGVOwner *owner);
void e2d_ogv_scope_leave(E2DOGVScope *scope);
E2DOGVScope *e2d_ogv_scope_suspend(void);
void e2d_ogv_scope_resume(E2DOGVScope *scope);
void e2d_ogv_owner_clear(E2DOGVOwner *owner);
void *e2d_ogv_context_zero(size_t size);
int e2d_ogv_memory_ready(void);
void *e2d_ogv_allocate(size_t size);
void *e2d_ogv_zero(size_t count, size_t size);
void *e2d_ogv_resize(void *value, size_t size);
void e2d_ogv_release(void *value);
uint64_t e2d_ogv_failure_epoch(void);
#endif
