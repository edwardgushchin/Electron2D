#include "memory.h"
#undef NDEBUG
#include <assert.h>
#include <stdio.h>
#include <string.h>

static E2DMemorySnapshot read_state(void) { E2DMemorySnapshot s; e2d_memory_snapshot(&s); return s; }
int main(void) {
    assert(e2d_memory_abi() == 1);
    int32_t error = -1;
    unsigned char *p = e2d_memory_allocate(73, 0, 1, &error);
    assert(p && !error && !p[0] && !p[72]); p[0] = 23; p[72] = 91;
    assert(read_state().nativeCurrent == 73);
    p = e2d_memory_reallocate(p, 4097, &error); assert(p && !error && p[0] == 23 && p[72] == 91);
    E2DMemorySnapshot stable = read_state();
    assert(!e2d_memory_reallocate(p, SIZE_MAX, &error) && error == 2 && p[0] == 23);
    assert(read_state().nativeCurrent == stable.nativeCurrent && read_state().peak == stable.peak);
    p = e2d_memory_reallocate(p, 0, &error); assert(p && !error && !read_state().nativeCurrent);
    e2d_memory_release(p); e2d_memory_release(NULL);
    for (size_t a = 1; a <= 4096; a *= 2) {
        p = e2d_memory_allocate(93, a, 1, &error); assert(p && !error && (uintptr_t)p % a == 0 && !p[92]);
        p[0] = 61; p[92] = 83;
        assert(!e2d_memory_reallocate(p, 100, &error) && error == 1 && p[0] == 61 && p[92] == 83);
        e2d_memory_release(p);
    }
    assert(!e2d_memory_allocate(1, 3, 0, &error) && error == 1);
    assert(!e2d_memory_allocate(SIZE_MAX, 0, 0, &error) && error == 2);
    p = e2d_memory_reallocate(NULL, 0, &error); assert(p && !error); e2d_memory_release(p);
    stable = read_state(); assert(!stable.nativeCurrent && stable.allocated == stable.retired);
    e2d_memory_lock(); assert(!e2d_memory_managed_locked(1000)); e2d_memory_unlock();
    p = e2d_memory_allocate(7000, 0, 0, &error); assert(p && !error);
    assert(read_state().current == 8000 && read_state().peak == 8000);
    e2d_memory_release(p); assert(read_state().current == 1000 && read_state().peak == 8000);
    e2d_memory_lock();
    assert(!e2d_memory_managed_locked(9000));
    e2d_memory_snapshot_locked(&stable); assert(stable.current == 9000 && stable.peak == 9000);
    assert(e2d_memory_managed_locked(UINT64_MAX) == -1);
    e2d_memory_unlock(); assert(read_state().current == 9000);
    e2d_memory_lock(); assert(!e2d_memory_managed_locked(INT64_MAX - 100)); e2d_memory_unlock();
    assert(!e2d_memory_allocate(101, 0, 0, &error) && error == 2);
    e2d_memory_lock(); assert(!e2d_memory_managed_locked(0)); e2d_memory_unlock();
    assert(!read_state().current && read_state().allocated == read_state().retired);
    puts("Native shared ledger: payload/alignment/resize/failure/drain/composition/range passed.");
}
