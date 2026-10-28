#ifndef ELECTRON2D_FONT_ORACLE_MEMORY_H
#define ELECTRON2D_FONT_ORACLE_MEMORY_H
#include <stdlib.h>

typedef int (*FontMemory)(void *(*)(size_t), void *(*)(void *, size_t), void (*)(void *));
#if defined(_WIN32)
#include <windows.h>
#else
#ifdef __cplusplus
extern "C"
#endif
int e2d_ft_hb_memory(void *(*)(size_t), void *(*)(void *, size_t), void (*)(void *))
#if defined(__APPLE__)
    __attribute__((weak_import));
#else
    __attribute__((weak));
#endif
#endif

static void prepare_font_memory(void) {
    // Private instrumented builds require a caller allocator; stock libraries use their own.
#if defined(_WIN32)
    FontMemory memory = (FontMemory)GetProcAddress(GetModuleHandleA("Electron2DFreeType.dll"), "e2d_ft_hb_memory");
#else
    FontMemory memory = e2d_ft_hb_memory;
#endif
    if (memory && memory(malloc, realloc, free)) abort();
}
#endif
