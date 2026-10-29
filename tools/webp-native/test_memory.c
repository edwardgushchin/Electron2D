#define main bridge_contract_main
#include "test_bridge.c"
#undef main
#include "../enet-native/memory.h"
#include <pthread.h>

extern void e2d_test_arm(size_t);
extern size_t e2d_test_attempts(void);
static void require(int value, const char *message) { if (!value) { fprintf(stderr, "%s\n", message); exit(11); } }
static void *allocate(size_t size) { int32_t error; return e2d_memory_allocate(size, 0, 0, &error); }
static void *register_memory(void *unused) { (void)unused; for (int i = 0; i < 1000; ++i) require(!e2d_webp_memory(allocate, e2d_memory_release), "Concurrent registration failed"); return NULL; }
static uint8_t rgba[47 * 33 * 4], decoded[sizeof(rgba)];
static buffer encoded;

static int exercise(int mode) {
    if (mode < 14) { encoded.size = 0; return e2d_webp_encode(rgba, sizeof(rgba), 47, 33, mode / 7, 25, mode % 7, write_bytes, &encoded); }
    if (mode < 28) return e2d_webp_decode(encoded.bytes, encoded.size, decoded, sizeof(decoded), 47, 33);
    return e2d_webp_decode(animation, sizeof(animation), decoded, 64, 4, 4);
}

int main(int argc, char **argv) {
    require(argc == 4, "Expected mode, failed request and output path");
    int mode = atoi(argv[1]); size_t fail_at = strtoull(argv[2], NULL, 10);
    require(e2d_webp_decode(NULL, 0, NULL, 0, 0, 0) == -5, "Unprepared codec accepted use");
    require(e2d_webp_memory(NULL, e2d_memory_release) != 0, "Incomplete allocator accepted");
    pthread_t workers[4];
    for (int i = 0; i < 4; ++i) require(!pthread_create(workers + i, NULL, register_memory, NULL), "Worker creation failed");
    for (int i = 0; i < 4; ++i) require(!pthread_join(workers[i], NULL), "Worker join failed");
    require(e2d_webp_memory(malloc, free) != 0, "Allocator replacement accepted");
    for (size_t i = 0; i < sizeof(rgba); ++i) rgba[i] = (uint8_t)(i * 73);
    rgba[3] = 0;
    if (mode >= 14 && mode < 28) require(exercise(mode - 14) == 0, "Decode fixture encoding failed");
    E2DMemorySnapshot before, after; e2d_memory_snapshot(&before); require(!before.nativeCurrent, "Setup did not drain");
    e2d_test_arm(fail_at);
    int status = exercise(mode); size_t attempts = e2d_test_attempts(); e2d_memory_snapshot(&after);
    require(!after.nativeCurrent && after.allocated == after.retired, "Failed codec leaked native memory");
    require(after.failures - before.failures == (fail_at && fail_at <= attempts ? 1u : 0u), "Failure was not counted");
    require(fail_at || status == 0, "Healthy codec failed");
    e2d_test_arm(0); require(!exercise(mode), "Healthy retry failed"); e2d_memory_snapshot(&after);
    require(!after.nativeCurrent && after.nativePeak > 0, "Retry leaked or peak was lost");
    if (mode < 14 || mode >= 28) {
        int decode_mode = mode < 14 ? mode + 14 : mode;
        require(!exercise(decode_mode), "Encoded result did not decode");
    }
    if (mode < 7 || (mode >= 14 && mode < 21)) require(!memcmp(decoded, rgba, sizeof(rgba)), "Lossless bytes changed");
    if (mode == 28) for (int y = 0; y < 4; ++y) for (int x = 0; x < 4; ++x) {
        int p = (y * 4 + x) * 4, red = x >= 2 && y >= 2;
        require(decoded[p] == (red ? 255 : 0) && !decoded[p+1] && !decoded[p+2] && decoded[p+3] == (red ? 255 : 0), "Animation canvas changed");
    }
    FILE *file = fopen(argv[3], "wb"); require(file != NULL, "Output missing");
    require(fwrite(decoded, 1, mode == 28 ? 64 : sizeof(decoded), file) == (mode == 28 ? 64 : sizeof(decoded)), "Decoded output write failed");
    require(!fclose(file), "Output close failed");
    char path[4096]; require(snprintf(path, sizeof(path), "%s.webp", argv[3]) < (int)sizeof(path), "Output path too long");
    file = fopen(path, "wb"); require(file != NULL, "Encoded output missing");
    const uint8_t *wire = mode == 28 ? animation : encoded.bytes; size_t wire_size = mode == 28 ? sizeof(animation) : encoded.size;
    require(fwrite(wire, 1, wire_size, file) == wire_size && !fclose(file), "Encoded output write failed");
    printf("{\"mode\":%d,\"failAt\":%zu,\"attempts\":%zu,\"survived\":%s,\"nativePeak\":%llu}\n", mode, fail_at, attempts, status == 0 ? "true" : "false", (unsigned long long)after.nativePeak);
    return 0;
}
