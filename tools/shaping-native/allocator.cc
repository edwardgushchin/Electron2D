#include "../enet-native/memory.h"

// Original native ownership survives language/cache teardown after managed execution stops.
extern "C" void *e2d_shaping_malloc(size_t size) { return e2d_memory_malloc(size); }
extern "C" void *e2d_shaping_calloc(size_t count, size_t size) { return e2d_memory_calloc(count, size); }
extern "C" void *e2d_shaping_realloc(void *value, size_t size) {
    if (!size) { e2d_memory_free(value); return nullptr; }
    return e2d_memory_realloc(value, size);
}
extern "C" void e2d_shaping_free(void *value) { e2d_memory_free(value); }
#if defined(_WIN32)
#define E2D_SHAPING_API __declspec(dllexport)
#else
#define E2D_SHAPING_API __attribute__((visibility("default")))
#endif
extern "C" E2D_SHAPING_API void e2d_shaping_memory(E2DMemoryFunctions *result) { e2d_memory_functions(result); }
