#include <stdint.h>
#include <stddef.h>
#include <webp/encode.h>
#if defined(_WIN32)
#define EXPORT __declspec(dllexport)
#else
#define EXPORT __attribute__((visibility("default")))
#endif

typedef int (*write_bytes)(const uint8_t *, size_t, void *);
typedef struct { write_bytes write; void *context; } output;
static int write_picture(const uint8_t *data, size_t size, const WebPPicture *picture) {
    const output *sink = (const output *)picture->custom_ptr;
    return sink->write(data, size, sink->context);
}

EXPORT int e2d_webp_encode(const uint8_t *rgba, uint64_t length, int width, int height,
                          int lossy, float quality, int method, write_bytes write, void *context) {
    WebPConfig config;
    WebPPicture picture;
    output sink = {write, context};
    if (!rgba || !write || width <= 0 || height <= 0 || width > WEBP_MAX_DIMENSION || height > WEBP_MAX_DIMENSION ||
        (uint64_t)width * (uint64_t)height * 4 != length || length > 64u * 1024u * 1024u ||
        (lossy != 0 && lossy != 1) || !(quality >= 0 && quality <= 100) || method < 0 || method > 6)
        return -1;
    if (!WebPConfigInit(&config) || !WebPPictureInit(&picture)) return -2;
    config.lossless = !lossy;
    config.quality = quality;
    config.method = method;
    config.exact = 1;
    config.use_sharp_yuv = 1;
    if (!WebPValidateConfig(&config)) return -3;
    picture.use_argb = 1;
    picture.width = width;
    picture.height = height;
    picture.writer = write_picture;
    picture.custom_ptr = &sink;
    int status = 0;
    if (!WebPPictureImportRGBA(&picture, rgba, width * 4) || !WebPEncode(&config, &picture))
        status = picture.error_code ? (int)picture.error_code : -4;
    WebPPictureFree(&picture);
    return status;
}
