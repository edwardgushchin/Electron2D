/* Test-only native exit callback: it must never enter managed execution. */
#include <SDL3/SDL.h>
#include "../enet-native/memory.h"
#include <stdlib.h>
#include <stdio.h>
static void *retained;
static void finish(void) {
    E2DMemorySnapshot before, after;
    e2d_memory_snapshot(&before); SDL_free(retained); e2d_memory_snapshot(&after);
    if (before.nativeCurrent != after.nativeCurrent + 65537 || before.peak != after.peak) abort();
    puts("SDL native atexit: 65537 bytes retired without a managed callback.");
}
#if defined(_WIN32)
__declspec(dllexport)
#else
__attribute__((visibility("default")))
#endif
void e2d_test_sdl_lifetime(void) {
    if (retained) abort();
    retained = SDL_malloc(65537);
    if (!retained || atexit(finish)) abort();
}
