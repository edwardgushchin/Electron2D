#include <SDL3/SDL.h>
#include "../enet-native/memory.h"
#undef NDEBUG
#include <assert.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#if !defined(_WIN32)
#include <dlfcn.h>
#endif

static E2DMemorySnapshot state(void) { E2DMemorySnapshot s; e2d_memory_snapshot(&s); return s; }
static void owners(void) {
    E2DMemoryFunctions f; e2d_memory_functions(&f);
    SDL_malloc_func a; SDL_calloc_func z; SDL_realloc_func r; SDL_free_func d;
    SDL_GetOriginalMemoryFunctions(&a, &z, &r, &d);
    assert(a == f.allocate && z == f.zero && r == f.resize && d == f.release);
    SDL_GetMemoryFunctions(&a, &z, &r, &d);
    assert(a == f.allocate && z == f.zero && r == f.resize && d == f.release);
}
static int worker(void *value) {
    static SDL_TLSID id;
    assert(SDL_SetTLS(&id, value, SDL_free));
    assert(SDL_GetTLS(&id) == value);
    return 73;
}
static int exercise(const char *role) {
    if (!strcmp(role, "surface")) {
        SDL_Surface *s = SDL_CreateSurface(129, 73, SDL_PIXELFORMAT_RGBA32);
        if (!s) return 0;
        int ok = SDL_FillSurfaceRect(s, NULL, SDL_MapSurfaceRGBA(s, 17, 41, 63, 255));
        SDL_DestroySurface(s); return ok;
    }
    if (!strcmp(role, "properties")) {
        SDL_PropertiesID p = SDL_CreateProperties();
        if (!p) return 0;
        int ok = 1;
        for (int i = 0; i < 40 && ok; ++i) {
            char key[32]; snprintf(key, sizeof(key), "entry.%d", i);
            ok = SDL_SetStringProperty(p, key, "retained allocation value");
        }
        if (ok) assert(!strcmp(SDL_GetStringProperty(p, "entry.39", "missing"), "retained allocation value"));
        SDL_DestroyProperties(p); return ok;
    }
    assert(!strcmp(role, "io"));
    SDL_IOStream *s = SDL_IOFromDynamicMem();
    if (!s) return 0;
    unsigned char bytes[257]; memset(bytes, 73, sizeof(bytes));
    int ok = 1;
    for (int i = 0; i < 40 && ok; ++i) {
        ok = SDL_WriteIO(s, bytes, sizeof(bytes)) == sizeof(bytes);
        if (!ok) {
            assert(SDL_GetIOSize(s) == i * (Sint64)sizeof(bytes));
            if (i) {
                assert(SDL_SeekIO(s, 0, SDL_IO_SEEK_SET) == 0);
                assert(SDL_ReadIO(s, bytes, sizeof(bytes)) == sizeof(bytes) && bytes[0] == 73 && bytes[256] == 73);
            }
        }
    }
    if (ok) {
        assert(SDL_SeekIO(s, 0, SDL_IO_SEEK_SET) == 0);
        memset(bytes, 0, sizeof(bytes));
        assert(SDL_ReadIO(s, bytes, sizeof(bytes)) == sizeof(bytes) && bytes[0] == 73 && bytes[256] == 73);
    }
    if (ok) {
        void *owned = SDL_GetPointerProperty(SDL_GetIOProperties(s), SDL_PROP_IOSTREAM_DYNAMIC_MEMORY_POINTER, NULL);
        assert(owned && SDL_SetPointerProperty(SDL_GetIOProperties(s), SDL_PROP_IOSTREAM_DYNAMIC_MEMORY_POINTER, NULL));
        SDL_CloseIO(s); assert(((unsigned char *)owned)[0] == 73); SDL_free(owned);
    } else SDL_CloseIO(s);
    return ok;
}
int main(int argc, char **argv) {
    E2DMemorySnapshot before = state(); assert(!before.current);
    owners(); assert(SDL_GetNumAllocations() == 0);
    if (argc > 1) {
        void (*arm)(size_t) = NULL; size_t (*attempts)(void) = NULL;
#if !defined(_WIN32)
        arm = (void (*)(size_t))dlsym(RTLD_DEFAULT, "e2d_test_arm");
        attempts = (size_t (*)(void))dlsym(RTLD_DEFAULT, "e2d_test_attempts");
#endif
        assert(arm && attempts); size_t position = argc > 2 ? strtoull(argv[2], NULL, 10) : 0;
        arm(position); int ok = exercise(argv[1]); size_t calls = attempts(); arm(0);
        SDL_Quit(); SDL_CleanupTLS();
        assert(!state().current && state().allocated == state().retired && SDL_GetNumAllocations() == 0);
        // Some caches deliberately retry a failed request; ownership must still drain.
        if (!position) assert(ok);
        else assert(state().failures == 1 && position <= calls);
        assert(exercise(argv[1])); SDL_Quit(); SDL_CleanupTLS(); assert(!state().current);
        printf("{\"role\":\"%s\",\"position\":%zu,\"requests\":%zu,\"ok\":%d,\"peak\":%llu}\n",
            argv[1], position, calls, ok, (unsigned long long)state().peak);
        return 0;
    }
    unsigned char *p = SDL_malloc(0); assert(p && state().nativeCurrent == 1); SDL_free(p);
    // SDL normalizes zero to one; its alloc_size annotation still describes zero to the compiler.
    SDL_calloc_func volatile zero_allocate = SDL_calloc;
    SDL_realloc_func volatile zero_resize = SDL_realloc;
    p = zero_allocate(0, 900); assert(p && !p[0] && state().nativeCurrent == 1); SDL_free(p);
    p = SDL_calloc(7, 13); assert(p && state().nativeCurrent == 91);
    for (size_t i = 0; i < 91; ++i) assert(!p[i]);
    p[0] = 71; p[90] = 93;
    p = SDL_realloc(p, 4097); assert(p && state().nativeCurrent == 4097 && p[0] == 71 && p[90] == 93);
    volatile size_t impossible = SIZE_MAX;
    assert(!SDL_realloc(p, impossible) && p[0] == 71 && p[90] == 93);
    SDL_CleanupTLS(); assert(state().nativeCurrent == 4097);
    p = zero_resize(p, 0); assert(p && p[0] == 71 && state().nativeCurrent == 1);
    SDL_free(p); assert(!SDL_calloc(impossible, 2)); SDL_CleanupTLS(); assert(!state().nativeCurrent);
    for (int i = 0; i < 8; ++i) {
        void *value = SDL_malloc(997); assert(value);
        SDL_Thread *thread = SDL_CreateThread(worker, "native-ledger", value); assert(thread);
        int result = 0; SDL_WaitThread(thread, &result); assert(result == 73);
    }
    assert(exercise("surface") && exercise("properties") && exercise("io"));
    SDL_IOStream *stream = SDL_IOFromDynamicMem(); assert(stream);
    unsigned char byte = 93; assert(SDL_WriteIO(stream, &byte, 1) == 1);
    assert(SDL_WriteIO(stream, &byte, impossible) == 0 && SDL_GetIOSize(stream) == 1);
    assert(SDL_SeekIO(stream, 0, SDL_IO_SEEK_SET) == 0);
    byte = 0; assert(SDL_ReadIO(stream, &byte, 1) == 1 && byte == 93); SDL_CloseIO(stream);
    SDL_Quit(); SDL_CleanupTLS();
    E2DMemorySnapshot after = state();
    assert(!after.current && after.allocated == after.retired && SDL_GetNumAllocations() == 0);
    printf("SDL original allocator: zero/overflow/resize/failure/thread TLS/subsystem drain passed; peak=%llu\n", (unsigned long long)after.peak);
}
