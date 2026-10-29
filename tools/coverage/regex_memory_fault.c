#define _POSIX_C_SOURCE 200112L
#include <stdint.h>
#include <stdlib.h>

static _Thread_local uint32_t remaining;

__attribute__((visibility("default")))
void e2d_test_fail_after(uint32_t calls) { remaining = calls; }

static void *fault_malloc(size_t size) {
    if (remaining && !--remaining) return NULL;
    return malloc(size);
}

// Exercise the production ledger's failure path with one failed PCRE2 request.
#define malloc fault_malloc
#include "../enet-native/memory.c"
#undef malloc
