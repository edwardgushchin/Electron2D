#ifndef ELECTRON2D_MEMORY_FUNCTIONS_H
#define ELECTRON2D_MEMORY_FUNCTIONS_H
#include <stddef.h>
#ifdef __cplusplus
extern "C" {
#endif
typedef struct {
    void *(*allocate)(size_t);
    void *(*zero)(size_t, size_t);
    void *(*resize)(void *, size_t);
    void (*release)(void *);
} E2DMemoryFunctions;
#ifdef __cplusplus
}
#endif
#endif
