#ifndef E2D_CODEC_MEMORY_H
#define E2D_CODEC_MEMORY_H
#include <ogg/ogg.h>
#include "memory.h"
#undef _ogg_malloc
#undef _ogg_calloc
#undef _ogg_realloc
#undef _ogg_free
#define _ogg_malloc e2d_ogv_allocate
#define _ogg_calloc e2d_ogv_zero
#define _ogg_realloc e2d_ogv_resize
#define _ogg_free e2d_ogv_release
#endif
