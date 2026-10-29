#include <stdint.h>
#include <stddef.h>
#include <string.h>
#include <webp/encode.h>
#include <webp/demux.h>
#include "webp_memory.h"
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

EXPORT int e2d_webp_decode(const uint8_t *bytes, uint64_t length, uint8_t *rgba,
                          uint64_t capacity, int width, int height) {
    if (!e2d_webp_memory_ready()) return -5;
    WebPBitstreamFeatures features;
    if (!bytes || !rgba || !length || length > 64u * 1024u * 1024u || width <= 0 || height <= 0 ||
        width > (1 << 24) || height > (1 << 24) || (uint64_t)width * height > 268435456u ||
        (uint64_t)width * height * 4 != capacity || capacity > SIZE_MAX) return -1;
    if (WebPGetFeatures(bytes, (size_t)length, &features) != VP8_STATUS_OK ||
        features.width != width || features.height != height) return -2;
    if (!features.has_animation)
        return WebPDecodeRGBAInto(bytes, (size_t)length, rgba, (size_t)capacity, width * 4) ? 0 : -3;

    WebPData data = {bytes, (size_t)length};
    WebPAnimDecoder *decoder = WebPAnimDecoderNew(&data, NULL);
    if (!decoder) return -3;
    WebPAnimInfo info;
    uint8_t *frame = NULL;
    int timestamp = 0, status = -3;
    if (WebPAnimDecoderGetInfo(decoder, &info) && info.canvas_width == (uint32_t)width &&
        info.canvas_height == (uint32_t)height && WebPAnimDecoderGetNext(decoder, &frame, &timestamp)) {
        memcpy(rgba, frame, (size_t)capacity);
        status = 0;
    }
    WebPAnimDecoderDelete(decoder);
    return status;
}

EXPORT int e2d_webp_encode(const uint8_t *rgba, uint64_t length, int width, int height,
                          int lossy, float quality, int method, write_bytes write, void *context) {
    if (!e2d_webp_memory_ready()) return -5;
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
