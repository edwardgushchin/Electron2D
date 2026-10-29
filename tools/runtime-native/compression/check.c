/* Exercises the actual PAL and common allocation core in an isolated child. */
#include "pal_zlib.h"
#include <brotli/encode.h>
#include <brotli/decode.h>
#include "../../enet-native/memory.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

extern void e2d_test_arm(size_t);
extern size_t e2d_test_attempts(void);
static unsigned char source[65536], encoded[131072], decoded[65536], fixture[131072];
static size_t fixture_size;
static size_t encoded_size;
static int quality;
static void require(int value, const char *message) { if (!value) { fprintf(stderr, "%s\n", message); exit(2); } }
static void bytes(const unsigned char *value, size_t size) { require(size == sizeof(source) && !memcmp(value, source, size), "Decoded bytes changed"); }
static void *custom_allocate(void *opaque, size_t size) {
    void *value = malloc(size); if (value) ++*(size_t*)opaque; return value;
}
static void custom_release(void *opaque, void *value) { if (value) --*(size_t*)opaque; free(value); }

static int zlib_case(int mode) {
    int window = mode < 3 ? (mode == 0 ? -15 : mode == 1 ? 15 : 31) : (mode == 3 ? -15 : mode == 4 ? 15 : 31);
    PAL_ZStream stream = {0};
    int status = mode < 3 ? CompressionNative_DeflateInit2_(&stream, 6, 8, window, 8, 0) : CompressionNative_InflateInit2_(&stream, window);
    if (status != PAL_Z_OK) { require(status == PAL_Z_MEMERROR && !stream.internalState, "Initialization failed without retiring its state"); return 0; }
    int success = 1;
    for (int pass = 0; pass < (mode < 3 ? 1 : 2); ++pass) {
        if (pass) require(CompressionNative_InflateReset2_(&stream, window) == PAL_Z_OK, "Inflate reset failed");
        stream.nextIn = mode < 3 ? source : fixture; stream.availIn = mode < 3 ? sizeof(source) : fixture_size;
        stream.nextOut = mode < 3 ? encoded : decoded; stream.availOut = mode < 3 ? sizeof(encoded) : sizeof(decoded);
        status = mode < 3 ? CompressionNative_Deflate(&stream, PAL_Z_FINISH) : CompressionNative_Inflate(&stream, PAL_Z_FINISH);
        if (status != PAL_Z_STREAMEND) { require(status == PAL_Z_MEMERROR, "Unexpected codec failure"); success = 0; break; }
        require(!stream.availIn, "Unconsumed input");
        if (mode < 3) encoded_size = sizeof(encoded) - stream.availOut;
        if (mode >= 3) bytes(decoded, sizeof(decoded) - stream.availOut);
    }
    status = mode < 3 ? CompressionNative_DeflateEnd(&stream) : CompressionNative_InflateEnd(&stream);
    require(status == PAL_Z_OK && !stream.internalState, "Stream retirement failed");
    return success;
}

static int brotli_case(int mode) {
    size_t output_size = mode == 6 ? sizeof(encoded) : sizeof(decoded);
    if (mode == 6) {
        int success = BrotliEncoderCompress(quality, 22, BROTLI_MODE_GENERIC, sizeof(source), source, &output_size, encoded);
        if (success) encoded_size = output_size;
        return success;
    }
    if (mode == 7) {
        int success = BrotliDecoderDecompress(fixture_size, fixture, &output_size, decoded) == BROTLI_DECODER_RESULT_SUCCESS;
        if (success) bytes(decoded, output_size);
        return success;
    }
    size_t available_in = mode == 8 ? sizeof(source) : fixture_size;
    size_t available_out = mode == 8 ? sizeof(encoded) : sizeof(decoded);
    const uint8_t *input = mode == 8 ? source : fixture;
    uint8_t *output = mode == 8 ? encoded : decoded;
    if (mode == 8) {
        BrotliEncoderState *state = BrotliEncoderCreateInstance(NULL, NULL, NULL);
        if (!state) return 0;
        require(BrotliEncoderSetParameter(state, BROTLI_PARAM_QUALITY, quality), "Encoder parameter rejected");
        int success = BrotliEncoderCompressStream(state, BROTLI_OPERATION_FINISH, &available_in, &input, &available_out, &output, NULL);
        if (success) require(!available_in && !BrotliEncoderHasMoreOutput(state), "Encoder did not finish");
        if (success) encoded_size = sizeof(encoded) - available_out;
        BrotliEncoderDestroyInstance(state);
        return success;
    }
    BrotliDecoderState *state = BrotliDecoderCreateInstance(NULL, NULL, NULL);
    if (!state) return 0;
    BrotliDecoderResult result = BrotliDecoderDecompressStream(state, &available_in, &input, &available_out, &output, NULL);
    int success = result == BROTLI_DECODER_RESULT_SUCCESS;
    if (success) { require(!available_in && BrotliDecoderIsFinished(state), "Decoder did not finish"); bytes(decoded, sizeof(decoded) - available_out); }
    else require(result == BROTLI_DECODER_RESULT_ERROR, "Unexpected decoder result");
    BrotliDecoderDestroyInstance(state);
    return success;
}

int main(int argc, char **argv) {
    require(argc == 5, "Expected mode, failure position, independent fixture and encoded output");
    int mode = atoi(argv[1]); size_t fail_at = strtoull(argv[2], NULL, 10);
    quality = getenv("E2D_COMPRESSION_QUALITY") ? atoi(getenv("E2D_COMPRESSION_QUALITY")) : 5;
    for (size_t i = 0; i < sizeof(source); ++i) source[i] = (unsigned char)(i * 113 + i / 7);
    if (getenv("E2D_COMPRESSION_RANDOM")) {
        uint32_t state = 1;
        for (size_t i = 0; i < sizeof(source); ++i) { state ^= state << 13; state ^= state >> 17; state ^= state << 5; source[i] = (unsigned char)state; }
    }
    FILE *file = fopen(argv[3], "rb"); require(file != NULL, "Fixture missing");
    fixture_size = fread(fixture, 1, sizeof(fixture), file); require(!ferror(file) && feof(file), "Fixture too large"); fclose(file);
    E2DMemorySnapshot before, after; e2d_memory_snapshot(&before);
    size_t custom_live = 0;
    BrotliEncoderState *custom_encoder = BrotliEncoderCreateInstance(custom_allocate, custom_release, &custom_live);
    BrotliDecoderState *custom_decoder = BrotliDecoderCreateInstance(custom_allocate, custom_release, &custom_live);
    require(custom_encoder && custom_decoder && custom_live, "Explicit allocator was not used");
    BrotliEncoderDestroyInstance(custom_encoder); BrotliDecoderDestroyInstance(custom_decoder);
    e2d_memory_snapshot(&after);
    require(!custom_live && after.allocated == before.allocated, "Explicit allocator ownership changed");
    PAL_ZStream invalid = {0};
    require(CompressionNative_DeflateInit2_(&invalid, 6, 8, 99, 8, 0) == PAL_Z_STREAMERROR && !invalid.internalState, "Invalid encoder initialization leaked");
    require(CompressionNative_InflateInit2_(&invalid, 99) == PAL_Z_STREAMERROR && !invalid.internalState, "Invalid decoder initialization leaked");
    e2d_memory_snapshot(&after); require(after.nativeCurrent == before.nativeCurrent, "Invalid initialization leaked");
    e2d_test_arm(fail_at);
    int success = mode < 6 ? zlib_case(mode) : brotli_case(mode);
    size_t attempts = e2d_test_attempts();
    e2d_memory_snapshot(&after);
    require(after.nativeCurrent == before.nativeCurrent && after.allocated - after.retired == after.nativeCurrent, "Native storage leaked");
    require(after.failures - before.failures == (fail_at && fail_at <= attempts ? 1u : 0u), "Failure was not observed by the ledger");
    require(fail_at || success, "Healthy codec failed");
    e2d_test_arm(0);
    require(mode < 6 ? zlib_case(mode) : brotli_case(mode), "Healthy retry failed");
    e2d_memory_snapshot(&after); require(after.nativeCurrent == before.nativeCurrent, "Retry leaked");
    if (mode < 3 || mode == 6 || mode == 8) {
        file = fopen(argv[4], "wb"); require(file != NULL, "Cannot write encoded output");
        require(fwrite(encoded, 1, encoded_size, file) == encoded_size && !fclose(file), "Cannot finish encoded output");
    }
    printf("{\"mode\":%d,\"failAt\":%zu,\"attempts\":%zu,\"survived\":%s,\"nativePeak\":%llu}\n", mode, fail_at, attempts, success ? "true" : "false", (unsigned long long)after.nativePeak);
    return 0;
}
