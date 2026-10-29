#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <webp/decode.h>
extern int e2d_webp_encode(const uint8_t *, uint64_t, int, int, int, float, int, int (*)(const uint8_t *, size_t, void *), void *);
extern int e2d_webp_decode(const uint8_t *, uint64_t, uint8_t *, uint64_t, int, int);
extern int e2d_webp_memory(void *(*)(size_t), void (*)(void *));
typedef struct { uint8_t bytes[4096]; size_t size; } buffer;
static int write_bytes(const uint8_t *bytes, size_t length, void *context) {
    buffer *output = context;
    if (length > sizeof(output->bytes) - output->size) return 0;
    memcpy(output->bytes + output->size, bytes, length); output->size += length; return 1;
}
static int fail_write(const uint8_t *bytes, size_t length, void *context) { (void)bytes; (void)length; (void)context; return 0; }
// libwebpmux: first red 2x2 frame at (2, 2) on a 4x4 canvas, then a blue frame.
static const uint8_t animation[] = {
    0x52, 0x49, 0x46, 0x46, 0x84, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50,
    0x56, 0x50, 0x38, 0x58, 0x0a, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
    0x03, 0x00, 0x00, 0x03, 0x00, 0x00, 0x41, 0x4e, 0x49, 0x4d, 0x06, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x41, 0x4e, 0x4d, 0x46,
    0x28, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x01, 0x00,
    0x00, 0x01, 0x00, 0x00, 0x64, 0x00, 0x00, 0x02, 0x56, 0x50, 0x38, 0x4c,
    0x0f, 0x00, 0x00, 0x00, 0x2f, 0x01, 0x40, 0x00, 0x00, 0x07, 0x10, 0xfd,
    0x8f, 0xfe, 0x07, 0x22, 0xa2, 0xff, 0x01, 0x00, 0x41, 0x4e, 0x4d, 0x46,
    0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00,
    0x00, 0x03, 0x00, 0x00, 0x64, 0x00, 0x00, 0x02, 0x56, 0x50, 0x38, 0x4c,
    0x0f, 0x00, 0x00, 0x00, 0x2f, 0x03, 0xc0, 0x00, 0x00, 0x07, 0x10, 0xd1,
    0xff, 0xfe, 0x07, 0x22, 0xa2, 0xff, 0x01, 0x00,
};
int main(void) {
    if (e2d_webp_memory(malloc, free)) return 10;
    uint8_t rgba[7 * 5 * 4];
    for (size_t i = 0; i < sizeof(rgba); ++i) rgba[i] = (uint8_t)(i * 73);
    rgba[3] = 0;
    for (int method = 0; method <= 6; ++method) {
        buffer output = {{0}, 0}; int width = 0, height = 0;
        int encoded = e2d_webp_encode(rgba, sizeof(rgba), 7, 5, 0, 25, method, write_bytes, &output);
        if (encoded) { fprintf(stderr, "WebP method %d failed with status %d\n", method, encoded); return 1; }
        uint8_t *decoded = WebPDecodeRGBA(output.bytes, output.size, &width, &height);
        if (!decoded || width != 7 || height != 5 || memcmp(decoded, rgba, sizeof(rgba))) return 2;
        WebPFree(decoded);
        uint8_t pixels[sizeof(rgba) + 2];
        memset(pixels, 0xA5, sizeof(pixels));
        if (e2d_webp_decode(output.bytes, output.size, pixels + 1, sizeof(rgba), 7, 5) ||
            memcmp(pixels + 1, rgba, sizeof(rgba)) || pixels[0] != 0xA5 || pixels[sizeof(pixels) - 1] != 0xA5) return 5;
        if (e2d_webp_decode(output.bytes, output.size, pixels + 1, sizeof(rgba) - 1, 7, 5) >= 0 ||
            e2d_webp_decode(output.bytes, output.size, pixels + 1, sizeof(rgba), 5, 7) >= 0 ||
            e2d_webp_decode(output.bytes, UINT64_MAX, pixels + 1, sizeof(rgba), 7, 5) >= 0 ||
            e2d_webp_decode(output.bytes, 8, pixels + 1, sizeof(rgba), 7, 5) >= 0) return 6;
    }
    for (int repeat = 0; repeat < 128; ++repeat)
        if (e2d_webp_encode(rgba, sizeof(rgba), 7, 5, 0, 25, 2, fail_write, NULL) == 0) return 3;
    if (e2d_webp_encode(rgba, sizeof(rgba) - 1, 7, 5, 0, 25, 2, write_bytes, NULL) >= 0) return 4;
    uint8_t canvas[64], malformed[sizeof(animation)];
    memcpy(malformed, animation, sizeof(animation));
    malformed[76] = 0;
    for (int repeat = 0; repeat < 128; ++repeat) {
        if (e2d_webp_decode(animation, sizeof(animation), canvas, sizeof(canvas), 4, 4)) return 7;
        for (int y = 0; y < 4; ++y) for (int x = 0; x < 4; ++x) {
            int p = (y * 4 + x) * 4, red = x >= 2 && y >= 2;
            if (canvas[p] != (red ? 255 : 0) || canvas[p + 1] || canvas[p + 2] || canvas[p + 3] != (red ? 255 : 0)) return 8;
        }
        if (e2d_webp_decode(malformed, sizeof(malformed), canvas, sizeof(canvas), 4, 4) >= 0) return 9;
    }
    puts("WebP native bridge: 7 exact encoder/decoder profiles, output canaries, 128 writer failures, 128 offset animation/error cleanups and invalid decode/encode layouts passed.");
    return 0;
}
