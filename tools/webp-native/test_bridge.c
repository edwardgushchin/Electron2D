#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <webp/decode.h>
extern int e2d_webp_encode(const uint8_t *, uint64_t, int, int, int, float, int, int (*)(const uint8_t *, size_t, void *), void *);
typedef struct { uint8_t bytes[4096]; size_t size; } buffer;
static int write_bytes(const uint8_t *bytes, size_t length, void *context) {
    buffer *output = context;
    if (length > sizeof(output->bytes) - output->size) return 0;
    memcpy(output->bytes + output->size, bytes, length); output->size += length; return 1;
}
static int fail_write(const uint8_t *bytes, size_t length, void *context) { (void)bytes; (void)length; (void)context; return 0; }
int main(void) {
    uint8_t rgba[7 * 5 * 4];
    for (size_t i = 0; i < sizeof(rgba); ++i) rgba[i] = (uint8_t)(i * 73);
    rgba[3] = 0;
    for (int method = 0; method <= 6; ++method) {
        buffer output = {{0}, 0}; int width = 0, height = 0;
        if (e2d_webp_encode(rgba, sizeof(rgba), 7, 5, 0, 25, method, write_bytes, &output)) return 1;
        uint8_t *decoded = WebPDecodeRGBA(output.bytes, output.size, &width, &height);
        if (!decoded || width != 7 || height != 5 || memcmp(decoded, rgba, sizeof(rgba))) return 2;
        WebPFree(decoded);
    }
    for (int repeat = 0; repeat < 128; ++repeat)
        if (e2d_webp_encode(rgba, sizeof(rgba), 7, 5, 0, 25, 2, fail_write, NULL) == 0) return 3;
    if (e2d_webp_encode(rgba, sizeof(rgba) - 1, 7, 5, 0, 25, 2, write_bytes, NULL) >= 0) return 4;
    puts("WebP native bridge: 7 efforts decode exact, 128 writer failures release native state, invalid layout rejected.");
    return 0;
}
