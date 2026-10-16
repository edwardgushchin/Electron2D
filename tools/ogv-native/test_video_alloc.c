#include "bridge.h"
#include <stdio.h>
#include <stdlib.h>

static int measuring;
static size_t allocations, releases, bytes;
void *__real_malloc(size_t);
void *__real_calloc(size_t, size_t);
void *__real_realloc(void *, size_t);
void __real_free(void *);
void *__wrap_malloc(size_t n) { if (measuring) { allocations++; bytes += n; } return __real_malloc(n); }
void *__wrap_calloc(size_t n, size_t s) { if (measuring) { allocations++; bytes += n * s; } return __real_calloc(n, s); }
void *__wrap_realloc(void *p, size_t n) { if (measuring) { allocations++; bytes += n; } return __real_realloc(p, n); }
void __wrap_free(void *p) { if (measuring && p) releases++; __real_free(p); }

int main(int argc, char **argv) {
    if (argc < 2) return 2;
    for (int file = 1; file < argc; file++) {
        FILE *input = fopen(argv[file], "rb");
        if (!input || fseek(input, 0, SEEK_END)) return 3;
        long length = ftell(input);
        if (length <= 0 || fseek(input, 0, SEEK_SET)) return 3;
        uint8_t *data = malloc((size_t)length);
        if (!data || fread(data, 1, (size_t)length, input) != (size_t)length) return 3;
        fclose(input);
        void *reader = NULL;
        e2d_video_info info;
        if (e2d_video_create(data, (size_t)length, 0, &info, &reader)) return 4;
        size_t invalid_sizes[] = {0, 1, 20, (size_t)length - 1, 536870913};
        for (size_t i = 0; i < sizeof(invalid_sizes) / sizeof(invalid_sizes[0]); i++) {
            void *invalid = NULL;
            e2d_video_info ignored;
            if (e2d_video_create(data, invalid_sizes[i], 0, &ignored, &invalid) >= 0 || invalid) return 7;
        }
        free(data);
        allocations = releases = bytes = 0;
        float pcm[257 * 8];
        const uint8_t *rgba;
        measuring = 1;
        for (int loop = 0; loop < 64; loop++) {
            for (int64_t frame = 0; frame < info.video_frames; frame++)
                if (e2d_video_frame(reader, frame, &rgba) < 0) return 5;
            if (e2d_video_seek_audio(reader, 0)) return 5;
            int count;
            do { count = e2d_video_audio(reader, pcm, 257); } while (count > 0);
            if (count < 0) return 5;
        }
        measuring = 0;
        printf("%s: 64 prepared loops, allocations=%zu bytes=%zu frees=%zu\n", argv[file], allocations, bytes, releases);
        e2d_video_destroy(reader);
        if (allocations || releases) return 6;
    }
    return 0;
}
