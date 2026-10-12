/* Independent public FreeType raster oracle for native strike/autohinter policy. */
#include <stdio.h>
#include <stdlib.h>
#include <ft2build.h>
#include FT_FREETYPE_H
static void check(FT_Error error) { if (error) { fprintf(stderr, "FreeType error %d\n", error); exit(1); } }
int main(int argc, char **argv) {
    if (argc != 5) return 2;
    FT_Library library; FT_Face face;
    check(FT_Init_FreeType(&library)); check(FT_New_Face(library, argv[1], 0, &face));
    check(FT_Set_Char_Size(face, 0, atoi(argv[2]) * 64, 72, 72));
    FT_Int32 flags = FT_LOAD_TARGET_NORMAL;
    if (atoi(argv[3]) && !FT_HAS_COLOR(face)) flags |= FT_LOAD_NO_BITMAP;
    else if (FT_HAS_COLOR(face)) flags |= FT_LOAD_COLOR;
    if (atoi(argv[4])) flags |= FT_LOAD_FORCE_AUTOHINT;
    check(FT_Load_Char(face, 'A', flags)); check(FT_Render_Glyph(face->glyph, FT_RENDER_MODE_NORMAL));
    FT_Bitmap *bitmap = &face->glyph->bitmap;
    printf("%u %u %d %d\n", bitmap->width, bitmap->rows, face->glyph->bitmap_left, face->glyph->bitmap_top);
    for (unsigned y = 0; y < bitmap->rows; y++) for (unsigned x = 0; x < bitmap->width; x++) {
        unsigned char *row = bitmap->buffer + y * bitmap->pitch;
        unsigned alpha = bitmap->pixel_mode == FT_PIXEL_MODE_MONO ? ((row[x >> 3] & (0x80 >> (x & 7))) ? 255 : 0) : row[x] * 255 / (bitmap->num_grays - 1);
        printf("%02X", alpha);
    }
    puts(""); FT_Done_Face(face); FT_Done_FreeType(library); return 0;
}
