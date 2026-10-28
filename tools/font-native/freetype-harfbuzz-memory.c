#include <stddef.h>
#if defined(_WIN32)
#define API __declspec(dllexport)
#else
#define API __attribute__((visibility("default")))
#endif
extern int e2d_hb_memory(void *(*)(size_t), void *(*)(void *, size_t), void (*)(void *));
API int e2d_ft_hb_memory(void *(*allocate)(size_t), void *(*resize)(void *, size_t), void (*release)(void *)) {
    return e2d_hb_memory(allocate, resize, release);
}
