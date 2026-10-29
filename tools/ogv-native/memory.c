#include "bridge.h"
#include "memory.h"
#include <stdatomic.h>
#include <string.h>
static E2DMemoryFunctions functions;
static atomic_bool ready;
static atomic_flag registration = ATOMIC_FLAG_INIT;
#if defined(_WIN32)
static __declspec(thread) uint64_t failure_epoch;
static __declspec(thread) E2DOGVScope *current_scope;
#else
static _Thread_local uint64_t failure_epoch;
static _Thread_local E2DOGVScope *current_scope;
#endif

int e2d_ogv_memory(const E2DMemoryFunctions *value) {
    if (!value || !value->allocate || !value->zero || !value->resize || !value->release) return -1;
    while (atomic_flag_test_and_set_explicit(&registration, memory_order_acquire)) {}
    int result = 0;
    if (atomic_load_explicit(&ready, memory_order_relaxed)) {
        if (functions.allocate != value->allocate || functions.zero != value->zero ||
            functions.resize != value->resize || functions.release != value->release) result = -1;
    } else {
        functions = *value;
        atomic_store_explicit(&ready, 1, memory_order_release);
    }
    atomic_flag_clear_explicit(&registration, memory_order_release);
    return result;
}
int e2d_ogv_memory_ready(void) { return atomic_load_explicit(&ready, memory_order_acquire); }
static void *record(void *value) {
    if (!value) {
        ++failure_epoch;
        if (current_scope) {
            current_scope->owner->failed = 1;
            longjmp(current_scope->jump, 1);
        }
    }
    return value;
}
int e2d_ogv_scope_enter(E2DOGVScope *scope, E2DOGVOwner *owner) {
    if (owner->failed) return -2;
    if (owner->active) return -1;
    scope->owner = owner; scope->previous = current_scope;
    if (fegetenv(&scope->environment)) return -1;
    owner->active = 1; current_scope = scope;
    return 0;
}
void e2d_ogv_scope_leave(E2DOGVScope *scope) {
    if (scope->owner->failed) fesetenv(&scope->environment);
    scope->owner->active = 0; current_scope = scope->previous;
}
E2DOGVScope *e2d_ogv_scope_suspend(void) {
    E2DOGVScope *saved = current_scope; current_scope = NULL; return saved;
}
void e2d_ogv_scope_resume(E2DOGVScope *scope) { current_scope = scope; }
static size_t hash(void *value) {
    uintptr_t key = (uintptr_t)value >> 3;
    key *= (uintptr_t)UINT64_C(0x9e3779b97f4a7c15);
    return (size_t)(key ^ (key >> (sizeof(key) * 4)));
}
static size_t slot(E2DOGVOwner *owner, void *value) {
    size_t index = hash(value) & (owner->capacity - 1);
    while (owner->slots[index] && owner->slots[index] != value) index = (index + 1) & (owner->capacity - 1);
    return index;
}
static void reserve(E2DOGVOwner *owner) {
    if (owner->count < owner->capacity / 2) return;
    if (owner->capacity > SIZE_MAX / 2 / sizeof(void *)) {
        record(functions.zero(SIZE_MAX, 2)); return;
    }
    size_t capacity = owner->capacity ? owner->capacity * 2 : 16;
    void **slots = record(functions.zero(capacity, sizeof(void *)));
    void **old = owner->slots; size_t old_capacity = owner->capacity;
    owner->slots = slots; owner->capacity = capacity;
    for (size_t i = 0; i < old_capacity; ++i) if (old[i]) owner->slots[slot(owner, old[i])] = old[i];
    if (old) functions.release(old);
}
static int remove_value(E2DOGVOwner *owner, void *value) {
    if (!owner->capacity) return 0;
    size_t gap = slot(owner, value), mask = owner->capacity - 1;
    if (!owner->slots[gap]) return 0;
    size_t next = (gap + 1) & mask;
    while (owner->slots[next]) {
        size_t home = hash(owner->slots[next]) & mask;
        if (((next - home) & mask) >= ((next - gap) & mask)) {
            owner->slots[gap] = owner->slots[next]; gap = next;
        }
        next = (next + 1) & mask;
    }
    owner->slots[gap] = NULL; --owner->count; return 1;
}
void e2d_ogv_owner_clear(E2DOGVOwner *owner) {
    /* A failed initializer can leave unreachable or non-destructible codec objects. */
    for (size_t i = 0; i < owner->capacity; ++i) if (owner->slots[i]) functions.release(owner->slots[i]);
    if (owner->slots) functions.release(owner->slots);
    memset(owner, 0, sizeof(*owner));
}
void *e2d_ogv_context_zero(size_t size) {
    if (!e2d_ogv_memory_ready()) return NULL;
    void *value = functions.zero(1, size);
    if (!value) ++failure_epoch;
    return value;
}
uint64_t e2d_ogv_failure_epoch(void) { return failure_epoch; }
void *e2d_ogv_allocate(size_t size) {
    if (!e2d_ogv_memory_ready()) return NULL;
    E2DOGVOwner *owner = current_scope ? current_scope->owner : NULL;
    if (owner) reserve(owner);
    void *value = record(functions.allocate(size));
    if (owner) { owner->slots[slot(owner, value)] = value; ++owner->count; }
    return value;
}
void *e2d_ogv_zero(size_t count, size_t size) {
    if (!e2d_ogv_memory_ready()) return NULL;
    E2DOGVOwner *owner = current_scope ? current_scope->owner : NULL;
    if (owner) reserve(owner);
    void *value = record(functions.zero(count, size));
    if (owner) { owner->slots[slot(owner, value)] = value; ++owner->count; }
    return value;
}
void *e2d_ogv_resize(void *value, size_t size) {
    if (!e2d_ogv_memory_ready()) return NULL;
    E2DOGVOwner *owner = current_scope ? current_scope->owner : NULL;
    if (owner) reserve(owner);
    int tracked = owner && value ? remove_value(owner, value) : 0;
    void *replacement = functions.resize(value, size);
    if (!replacement && tracked) { owner->slots[slot(owner, value)] = value; ++owner->count; }
    record(replacement);
    if (owner) {
        owner->slots[slot(owner, replacement)] = replacement; ++owner->count;
    }
    return replacement;
}
void e2d_ogv_release(void *value) {
    if (!value) return;
    if (current_scope) remove_value(current_scope->owner, value);
    functions.release(value);
}
