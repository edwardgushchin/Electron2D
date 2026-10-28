#include <stdint.h>
#include <stdatomic.h>

_Atomic uint64_t e2d_fast_entries, e2d_slow_entries;
void e2d_route_begin(void) { e2d_fast_entries = 0; e2d_slow_entries = 0; }
void e2d_route_read(uint64_t *fast, uint64_t *slow) { *fast = e2d_fast_entries; *slow = e2d_slow_entries; }
#ifdef E2D_NATIVEAOT
extern void *__real_RhpGcAlloc(void *type, uint32_t flags, uintptr_t elements, void *frame);
void *__wrap_RhpGcAlloc(void *type, uint32_t flags, uintptr_t elements, void *frame) {
    e2d_slow_entries++;
    return __real_RhpGcAlloc(type, flags, elements, frame);
}
#endif
