#include <ft2build.h>
#include FT_FREETYPE_H
#include FT_MODULE_H
#include FT_SYSTEM_H
#include <brotli/decode.h>
#include <stdio.h>
#include <stdlib.h>

extern int e2d_ft_hb_memory(void *(*)(size_t), void *(*)(void *, size_t), void (*)(void *));

struct MemoryState { long calls, fail_at, failures, live, decoders, decoder_failed; };
static long recovered_contexts;
static size_t decoder_size;

static void *probe_allocate(void *opaque, size_t size) {
    (void)opaque;
    if (!decoder_size) decoder_size = size;
    return malloc(size);
}

static void probe_release(void *opaque, void *block) {
    (void)opaque;
    free(block);
}

static void *allocate(FT_Memory memory, long size) {
    struct MemoryState *state = memory->user;
    state->calls++;
    if ((size_t)size == decoder_size) state->decoders++;
    if (state->calls == state->fail_at) {
        state->failures++;
        if ((size_t)size == decoder_size) state->decoder_failed++;
        return NULL;
    }
    void *result = malloc((size_t)size);
    if (result) state->live++;
    return result;
}

static void release(FT_Memory memory, void *block) {
    struct MemoryState *state = memory->user;
    if (block) { state->live--; free(block); }
}

static void *resize(FT_Memory memory, long current, long size, void *block) {
    struct MemoryState *state = memory->user;
    (void)current;
    if (++state->calls == state->fail_at) { state->failures++; return NULL; }
    int fresh = block == NULL;
    void *result = realloc(block, (size_t)size);
    if (result && fresh) state->live++;
    return result;
}

static long exercise(unsigned char *data, long size, long fail_at, int hinted, int invalid) {
    struct MemoryState state = {0, fail_at, 0, 0, 0, 0};
    struct FT_MemoryRec_ memory = {&state, allocate, release, resize};
    FT_Library library = NULL;
    FT_Face face = NULL;
    FT_Error error = FT_New_Library(&memory, &library);
    if (!error) {
        FT_Add_Default_Modules(library);
        FT_Set_Default_Properties(library);
        error = FT_New_Memory_Face(library, data, size, 0, &face);
        if (!error) error = FT_Set_Char_Size(face, 0, 24 * 64, 72, 72);
        if (!error) error = FT_Load_Glyph(face, FT_Get_Char_Index(face, 'A'),
            FT_LOAD_RENDER | (hinted ? 0 : FT_LOAD_NO_HINTING));
    }
    int retry_context = error == FT_Err_Could_Not_Find_Context && face != NULL;
    if (retry_context) {
        error = FT_Load_Glyph(face, FT_Get_Char_Index(face, 'A'), FT_LOAD_RENDER);
        if (!error) recovered_contexts++;
    }
    if (face) FT_Done_Face(face);
    if (library) FT_Done_Library(library);
    int wrong_result = invalid ? error == 0 : error != 0;
    if ((!fail_at && (wrong_result || state.decoders != 1)) || (retry_context && error) ||
        (fail_at && state.failures != 1) ||
        (state.decoder_failed && error != FT_Err_Out_Of_Memory) || state.live) {
        fprintf(stderr, "FreeType allocation check: hinted=%d, fail_at=%ld, error=%d, failures=%ld, live=%ld, decoders=%ld\n",
            hinted, fail_at, error, state.failures, state.live, state.decoders);
        return -1;
    }
    return state.calls;
}

static long compressed_offset(const unsigned char *data, long size) {
    if (size < 48) return -1;
    long at = 48;
    int count = (data[12] << 8) | data[13];
    for (int i = 0; i < count; i++) {
        if (at >= size) return -1;
        unsigned flags = data[at++], tag = flags & 63;
        int glyph = tag == 10 || tag == 11;
        if (tag == 63) {
            if (at > size - 4) return -1;
            glyph = data[at] == 'g' && data[at + 1] == 'l' && data[at + 2] == 'y' && data[at + 3] == 'f';
            glyph |= data[at] == 'l' && data[at + 1] == 'o' && data[at + 2] == 'c' && data[at + 3] == 'a';
            at += 4;
        }
        int lengths = 1 + (glyph ? (flags >> 6) != 3 : (flags >> 6) != 0);
        for (int j = 0; j < lengths; j++) {
            int bytes = 0;
            do { if (at >= size || ++bytes > 5) return -1; } while (data[at++] & 128);
        }
    }
    return at < size ? at : -1;
}

int main(int argc, char **argv) {
    if (argc != 2) return 1;
    if (e2d_ft_hb_memory(malloc, realloc, free)) return 11;
    BrotliDecoderState *decoder = BrotliDecoderCreateInstance(probe_allocate, probe_release, NULL);
    if (!decoder || !decoder_size) return 10;
    BrotliDecoderDestroyInstance(decoder);
    FILE *file = fopen(argv[1], "rb");
    if (!file || fseek(file, 0, SEEK_END)) return 2;
    long size = ftell(file);
    if (size <= 0 || size > 16 * 1024 * 1024 || fseek(file, 0, SEEK_SET)) return 3;
    unsigned char *data = malloc((size_t)size);
    if (!data || fread(data, 1, (size_t)size, file) != (size_t)size) return 4;
    fclose(file);
    FT_Library library;
    FT_Face face;
    if (FT_Init_FreeType(&library) || FT_New_Memory_Face(library, data, size, 0, &face)) return 5;
    if (!FT_Get_Char_Index(face, 'A') || FT_Set_Pixel_Sizes(face, 0, 24) ||
        FT_Load_Char(face, 'A', FT_LOAD_RENDER) || !face->glyph->bitmap.width || !face->glyph->bitmap.rows) return 6;
    FT_Done_Face(face);
    for (int hinted = 0; hinted <= 1; hinted++) {
        long calls = exercise(data, size, 0, hinted, 0);
        if (calls <= 0) return 8;
        for (long fail_at = 1; fail_at <= calls; fail_at++)
            if (exercise(data, size, fail_at, hinted, 0) < 0) return 9;
        printf("FreeType allocation failures: hinted=%d, %ld positions, zero retained blocks.\n", hinted, calls);
    }
    printf("FreeType bytecode context recovery: %ld successful retries on the same face.\n", recovered_contexts);
    long compressed = compressed_offset(data, size);
    if (compressed < 0) return 11;
    unsigned char first = data[compressed];
    data[compressed] = 0x11; /* Reserved Brotli window bits, inside a valid WOFF2 directory. */
    if (exercise(data, size, 0, 0, 1) < 0) return 12;
    data[compressed] = first;
    printf("WOFF2 Brotli allocator: %zu-byte decoder state is supplied; corrupt-stream cleanup passed.\n", decoder_size);
    data[0] = 0;
    if (!FT_New_Memory_Face(library, data, size, 0, &face)) return 7;
    FT_Done_FreeType(library);
    free(data);
    puts("Target-native FreeType decodes and rasterizes bundled WOFF2; corrupt data is rejected.");
    return 0;
}
