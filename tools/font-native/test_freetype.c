#include <ft2build.h>
#include FT_FREETYPE_H
#include FT_MODULE_H
#include FT_SYSTEM_H
#include <stdio.h>
#include <stdlib.h>

struct MemoryState { long calls, fail_at, failures, live; };
static long recovered_contexts;

static void *allocate(FT_Memory memory, long size) {
    struct MemoryState *state = memory->user;
    if (++state->calls == state->fail_at) { state->failures++; return NULL; }
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

static long exercise(unsigned char *data, long size, long fail_at, int hinted) {
    struct MemoryState state = {0, fail_at, 0, 0};
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
    if ((!fail_at && error) || (retry_context && error) || (fail_at && state.failures != 1) || state.live) {
        fprintf(stderr, "FreeType allocation check: hinted=%d, fail_at=%ld, error=%d, failures=%ld, live=%ld\n",
            hinted, fail_at, error, state.failures, state.live);
        return -1;
    }
    return state.calls;
}

int main(int argc, char **argv) {
    if (argc != 2) return 1;
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
        long calls = exercise(data, size, 0, hinted);
        if (calls <= 0) return 8;
        for (long fail_at = 1; fail_at <= calls; fail_at++)
            if (exercise(data, size, fail_at, hinted) < 0) return 9;
        printf("FreeType allocation failures: hinted=%d, %ld positions, zero retained blocks.\n", hinted, calls);
    }
    printf("FreeType bytecode context recovery: %ld successful retries on the same face.\n", recovered_contexts);
    data[0] = 0;
    if (!FT_New_Memory_Face(library, data, size, 0, &face)) return 7;
    FT_Done_FreeType(library);
    free(data);
    puts("Target-native FreeType decodes and rasterizes bundled WOFF2; corrupt data is rejected.");
    return 0;
}
