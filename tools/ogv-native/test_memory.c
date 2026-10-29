#include "bridge.h"
#include "../enet-native/memory.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <pthread.h>
extern void e2d_test_arm(size_t);
extern size_t e2d_test_attempts(void);
static E2DMemoryFunctions memory;
static uint8_t *source;
static size_t source_bytes;
static void check(int value, const char *reason) { if (!value) { fprintf(stderr, "%s\n", reason); exit(1); } }
static int write_page(const uint8_t *data, size_t size, void *user) {
    check(data && size && user, "Invalid writer input");
    check(e2d_ogv_finish(*(void **)user)==-1,"Reentrant writer mutated its active encoder");return 1;
}
static void drained(void) {
    E2DMemorySnapshot state; e2d_memory_snapshot(&state);
    check(!state.nativeCurrent && state.allocated == state.retired, "Bridge ownership did not drain");
}
static int exercise(int encode, int exclusive) {
    void *context = NULL;
    if (encode) {
        int width = encode == 2 ? 512 : 66, height = encode == 2 ? 512 : 50;
        size_t extent = (size_t)width * height * 3;
        uint8_t *rgb = malloc(extent); check(rgb != NULL, "Caller image allocation failed");
        unsigned random = 123;
        for (size_t i = 0; i < extent; ++i) { random = random * 1664525U + 1013904223U; rgb[i] = encode == 2 ? (uint8_t)(random >> 24) : 0; }
        int status = e2d_ogv_create(width, height, 59, 48000, 2, encode == 2 ? 1.f : .75f, .5f, 4, 3, write_page, &context, &context);
        if (!status) {
            int32_t pcm[814 * 2] = {0};
            for (int i = 0; i < 4 && !status; ++i) status = e2d_ogv_frame(context, rgb, extent, pcm, 813);
            if (!status) status = e2d_ogv_finish(context);
        }
        if (status && context) check(e2d_ogv_finish(context) == status, "Encoder failure was not sticky");
        e2d_ogv_destroy(context); free(rgb); return status;
    }
    e2d_video_info info, untouched; memset(&info, 0x55, sizeof(info)); untouched = info;
    int status = e2d_video_create(source, source_bytes, 0, &info, &context);
    if (status) {
        check(!context && !memcmp(&info, &untouched, sizeof(info)), "Failed decoder published state");
        return status;
    }
    E2DMemorySnapshot before, after; e2d_memory_snapshot(&before);
    check(before.nativeCurrent > (size_t)info.width * info.height * 4, "Packet/context ownership is absent");
    float pcm[257 * 8]; const uint8_t *rgba;
    for (int loop = 0; loop < 3; ++loop) {
        for (int64_t frame = 0; frame < info.video_frames; ++frame)
            check(e2d_video_frame(context, frame, &rgba) >= 0, "Prepared frame failed");
        check(!e2d_video_seek_audio(context, 0), "Audio rewind failed");
        int frames; do { frames = e2d_video_audio(context, pcm, 257); } while (frames > 0);
        check(!frames, "Prepared audio failed");
    }
    e2d_memory_snapshot(&after);
    if (exclusive) check(before.allocated == after.allocated && before.retired == after.retired &&
          before.allocations == after.allocations && before.resizes == after.resizes, "Prepared bridge allocated");
    e2d_video_destroy(context); return 0;
}
static void *registration(void *unused) {
    (void)unused;
    for (int i = 0; i < 1000; ++i) check(!e2d_ogv_memory(&memory), "Concurrent registration failed");
    return NULL;
}
static void *parallel(void *unused) {
    (void)unused;
    for (int i = 0; i < 8; ++i) check(!exercise(0, 0), "Concurrent decoder failed");
    return NULL;
}
int main(int argc, char **argv) {
    check(argc == 4, "Expected fixture, encode/encode-noise/decode and failure position");
    FILE *file = fopen(argv[1], "rb"); check(file && !fseek(file, 0, SEEK_END), "Fixture missing");
    long bytes = ftell(file); check(bytes > 0 && !fseek(file, 0, SEEK_SET), "Fixture is empty");
    source_bytes = (size_t)bytes; source = malloc(source_bytes);
    check(source && fread(source, 1, source_bytes, file) == source_bytes && !fclose(file), "Fixture read failed");
    void *missing = NULL; e2d_video_info info;
    check(e2d_video_create(source, source_bytes, 0, &info, &missing) == -5 && !missing, "Unprepared decoder accepted");
    check(e2d_ogv_create(66, 50, 59, 48000, 2, .75f, .5f, 4, 3, write_page, NULL, &missing) == -5 && !missing, "Unprepared encoder accepted");
    e2d_memory_functions(&memory);
    E2DMemoryFunctions incomplete = memory; incomplete.resize = NULL;
    check(e2d_ogv_memory(NULL) && e2d_ogv_memory(&incomplete), "Incomplete callbacks accepted");
    pthread_t threads[4];
    for (int i = 0; i < 4; ++i) check(!pthread_create(threads + i, NULL, registration, NULL), "Registration thread failed");
    for (int i = 0; i < 4; ++i) check(!pthread_join(threads[i], NULL), "Registration join failed");
    E2DMemoryFunctions foreign = {malloc, calloc, realloc, free};
    check(e2d_ogv_memory(&foreign), "Allocator replacement accepted");
    drained();
    size_t fail_at = (size_t)strtoull(argv[3], NULL, 10);
    int encode = !strcmp(argv[2], "encode") ? 1 : !strcmp(argv[2], "encode-noise") ? 2 : 0;
    e2d_test_arm(fail_at); int status = exercise(encode, 1); size_t attempts = e2d_test_attempts();
    check(status == (fail_at ? -2 : 0), "Wrong allocation failure status"); drained();
    E2DMemorySnapshot before; e2d_memory_snapshot(&before);
    check(before.failures == (fail_at ? 1U : 0U), "Refusal did not reach common ledger");
    e2d_test_arm(0); check(!exercise(encode, 1), "Healthy retry failed"); drained();
    if (!fail_at && !encode) {
        for (int i = 0; i < 4; ++i) check(!pthread_create(threads + i, NULL, parallel, NULL), "Decoder thread failed");
        for (int i = 0; i < 4; ++i) check(!pthread_join(threads[i], NULL), "Decoder join failed");
        drained();
    }
    printf("{\"role\":\"%s\",\"attempts\":%zu,\"failure\":%zu,\"peak\":%llu}\n", argv[2], attempts, fail_at, (unsigned long long)before.nativePeak);
    free(source); return 0;
}
