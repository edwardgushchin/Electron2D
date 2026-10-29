#include "bridge.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <limits.h>
#include <math.h>
#define CHECK(test) do { if (!(test)) { fprintf(stderr, "check failed at line %d: %s\n", __LINE__, #test); exit(1); } } while (0)
typedef struct { FILE *file; int calls, fail_after; } sink;
static int write_output(const uint8_t *bytes, size_t length, void *user) {
    sink *s = user; s->calls++;
    if (s->fail_after && s->calls >= s->fail_after) return 0;
    return s->file ? fwrite(bytes, 1, length, s->file) == length : 1;
}
static void test_failure(int point) {
    sink s = {NULL, 0, point == 0 ? 1 : 0}; void *c = NULL;
    int result = e2d_ogv_create(66, 50, 59, 48000, 2, .75f, .5f, 4, 3, write_output, &s, &c);
    if (!point) { CHECK(result == -4 && c); e2d_ogv_destroy(c); return; }
    CHECK(result == 0); s.fail_after = s.calls + 1;
    uint8_t rgb[66 * 50 * 3] = {0}; int32_t pcm[814 * 2] = {0};
    result = 0;
    for (int i = 0; i < point && !result; i++) result = e2d_ogv_frame(c, rgb, sizeof(rgb), pcm, 813);
    if (!result) result = e2d_ogv_finish(c);
    CHECK(result == -4); CHECK(e2d_ogv_finish(c) != 0); int calls = s.calls;
    CHECK(e2d_ogv_frame(c, rgb, sizeof(rgb), pcm, 813) != 0); CHECK(calls == s.calls); e2d_ogv_destroy(c);
}
int main(int argc, char **argv) {
    E2DMemoryFunctions memory = {malloc, calloc, realloc, free};
    if (e2d_ogv_memory(&memory)) return 10;
    CHECK(argc == 2); void *c = (void *)1; sink s = {0};
    CHECK(e2d_ogv_create(1, 2, 60, 48000, 2, .75f, .5f, 4, 64, write_output, &s, &c) == -1 && !c);
    CHECK(e2d_ogv_create(INT_MAX, 2, 60, 48000, 2, .75f, .5f, 4, 64, write_output, &s, &c) == -1 && !c);
    CHECK(e2d_ogv_create(66, 50, 0, 48000, 2, .75f, .5f, 4, 64, write_output, &s, &c) == -1 && !c);
    CHECK(e2d_ogv_create(66, 50, 60, 48000, 3, .75f, .5f, 4, 64, write_output, &s, &c) == -1 && !c);
    CHECK(e2d_ogv_create(66, 50, 60, 48000, 2, .75f, .5f, 4, 64, NULL, &s, &c) == -1 && !c);
    CHECK(e2d_ogv_frame(NULL, NULL, 0, NULL, 0) == -1); CHECK(e2d_ogv_finish(NULL) == -1); e2d_ogv_destroy(NULL);
    for (int ch = 2; ch <= 8; ch += 2) for (int frames = 0; frames <= 17; frames = frames == 0 ? 1 : frames == 1 ? 17 : 18) {
        char name[4096]; CHECK(snprintf(name, sizeof(name), "%s/native-%d-%d.ogv", argv[1], ch, frames) < (int)sizeof(name));
        sink out = {fopen(name, "wb"), 0, 0}; CHECK(out.file);
        CHECK(e2d_ogv_create(66, 50, 59, 48000, ch, .75f, .5f, 4, 3, write_output, &out, &c) == 0);
        uint8_t rgb[66 * 50 * 3]; for (int y = 0; y < 50; y++) for (int x = 0; x < 66; x++) { int at = (y * 66 + x) * 3; rgb[at] = x < 32 ? 255 : 0; rgb[at + 1] = (uint8_t)(y * 5); rgb[at + 2] = 0; }
        int32_t pcm[814 * 8]; const int frequencies[] = {220, 330, 440, 55, 550, 660, 770, 880};
        CHECK(e2d_ogv_frame(c, rgb, sizeof(rgb) - 1, pcm, 813) == -1);
        for (int n = 0; n < frames; n++) { for (int y = 0; y < 50; y++) for (int x = 32; x < 66; x++) rgb[(y * 66 + x) * 3 + 2] = (n & 1) ? 255 : 0; int count = (n + 1) * 48000 / 59 - n * 48000 / 59;
            for (int i = 0; i < count; i++) for (int channel = 0; channel < ch; channel++) pcm[i * ch + channel] = (int32_t)(sin(6.283185307179586 * frequencies[channel] * (n * 48000 / 59 + i) / 48000) * 214748364.7);
            CHECK(e2d_ogv_frame(c, rgb, sizeof(rgb), pcm, count) == 0); }
        CHECK(e2d_ogv_finish(c) == 0); CHECK(e2d_ogv_finish(c) == -1); CHECK(e2d_ogv_frame(c, rgb, sizeof(rgb), pcm, 813) == -1);
        e2d_ogv_destroy(c); CHECK(fclose(out.file) == 0);
        char expected[4108]; CHECK(snprintf(expected, sizeof(expected), "%s.expect.json", name) < (int)sizeof(expected));
        FILE *metadata = fopen(expected, "w"); CHECK(metadata); fprintf(metadata, "{\"frames\":%d,\"rate\":48000,\"channels\":%d,\"fps\":59,\"width\":66,\"height\":50,\"keyframes\":3,\"input_channels\":%d}", frames, ch == 4 ? 6 : ch, ch); CHECK(fclose(metadata) == 0);
    }
    {
        char name[4096]; CHECK(snprintf(name, sizeof(name), "%s/native-noise.ogv", argv[1]) < (int)sizeof(name));
        sink out = {fopen(name, "wb"), 0, 0}; CHECK(out.file);
        CHECK(e2d_ogv_create(512, 512, 59, 48000, 2, 1.f, .5f, 4, 3, write_output, &out, &c) == 0);
        uint8_t *rgb = malloc(512 * 512 * 3); CHECK(rgb); int32_t pcm[814 * 2] = {0}; uint32_t random = 123;
        for (int n = 0; n < 3; n++) {
            for (int y = 0; y < 512; y++) for (int x = 0; x < 512; x++) for (int channel = 0; channel < 3; channel++) {
                random = random * 1664525u + 1013904223u;
                rgb[(y * 512 + x) * 3 + channel] = x < 32 ? (channel == 0 ? 255 : 0) : (uint8_t)(random >> 24);
            }
            CHECK(e2d_ogv_frame(c, rgb, 512 * 512 * 3, pcm, (n + 1) * 48000 / 59 - n * 48000 / 59) == 0);
        }
        CHECK(e2d_ogv_finish(c) == 0); e2d_ogv_destroy(c); free(rgb); CHECK(fclose(out.file) == 0);
        char expected[4108]; CHECK(snprintf(expected, sizeof(expected), "%s.expect.json", name) < (int)sizeof(expected));
        FILE *metadata = fopen(expected, "w"); CHECK(metadata);
        fputs("{\"frames\":3,\"rate\":48000,\"channels\":2,\"fps\":59,\"width\":512,\"height\":512,\"keyframes\":3}", metadata); CHECK(fclose(metadata) == 0);
    }
    for (int i = 0; i < 8; i++) { test_failure(0); test_failure(1); test_failure(5); }
    puts("OGV native lifecycle, bounds, short/empty/surround output and callback failures passed"); return 0;
}
