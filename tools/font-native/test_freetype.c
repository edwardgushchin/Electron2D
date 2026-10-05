#include <ft2build.h>
#include FT_FREETYPE_H
#include <stdio.h>
#include <stdlib.h>

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
    data[0] = 0;
    if (!FT_New_Memory_Face(library, data, size, 0, &face)) return 7;
    FT_Done_FreeType(library);
    free(data);
    puts("Target-native FreeType decodes and rasterizes bundled WOFF2; corrupt data is rejected.");
    return 0;
}
