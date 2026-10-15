#ifndef E2D_OGV_H
#define E2D_OGV_H
#include <stddef.h>
#include <stdint.h>
#ifdef _WIN32
#if defined(E2D_OGV_STATIC)
#define E2D_API
#elif defined(E2D_OGV_BUILD)
#define E2D_API __declspec(dllexport)
#else
#define E2D_API __declspec(dllimport)
#endif
#else
#define E2D_API __attribute__((visibility("default")))
#endif
typedef int (*e2d_ogv_writer)(const uint8_t *, size_t, void *);
E2D_API int e2d_ogv_create(int width, int height, int fps, int rate, int channels, float video_quality, float audio_quality, int speed, int keyframes, e2d_ogv_writer writer, void *user, void **result);
E2D_API int e2d_ogv_frame(void *context, const uint8_t *rgb, size_t rgb_bytes, const int32_t *pcm, int samples);
E2D_API int e2d_ogv_finish(void *context);
E2D_API void e2d_ogv_destroy(void *context);
#endif
