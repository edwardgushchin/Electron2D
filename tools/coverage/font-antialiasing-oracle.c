/* Independent public FreeType raster oracle, using the packaged library through LD_PRELOAD. */
#include <stdio.h>
#include <stdlib.h>
#include <ft2build.h>
#include FT_FREETYPE_H
#include FT_GLYPH_H
#include FT_STROKER_H
static void check(FT_Error error) { if (error) { fprintf(stderr, "FreeType error %d\n", error); exit(1); } }
int main(int argc, char **argv) {
    if (argc != 8) return 2;
    int size = atoi(argv[2]), hint = atoi(argv[3]), phase = atoi(argv[4]), radius = atoi(argv[5]), aa = atoi(argv[6]), layout = atoi(argv[7]);
    FT_Library library; FT_Face face; FT_Glyph glyph = NULL; FT_Stroker stroker = NULL;
    check(FT_Init_FreeType(&library)); check(FT_New_Face(library, argv[1], 0, &face));
    check(FT_Set_Char_Size(face, 0, size * 64, 72, 72));
    FT_Vector delta = {phase, 0}; FT_Set_Transform(face, NULL, &delta);
    FT_Int32 flags = FT_LOAD_NO_BITMAP | (hint == 0 ? FT_LOAD_NO_HINTING : hint == 1 ? FT_LOAD_TARGET_LIGHT : FT_LOAD_TARGET_NORMAL);
    check(FT_Load_Char(face, 'A', flags));
    FT_Render_Mode mode = aa == 0 ? FT_RENDER_MODE_MONO : aa == 2 && (layout == 1 || layout == 2) ? FT_RENDER_MODE_LCD : aa == 2 && (layout == 3 || layout == 4) ? FT_RENDER_MODE_LCD_V : FT_RENDER_MODE_NORMAL;
    FT_Bitmap *bitmap; int left, top;
    if (radius) {
        check(FT_Get_Glyph(face->glyph, &glyph)); check(FT_Stroker_New(library, &stroker));
        FT_Stroker_Set(stroker, radius, FT_STROKER_LINECAP_BUTT, FT_STROKER_LINEJOIN_ROUND, 0);
        check(FT_Glyph_Stroke(&glyph, stroker, 1)); check(FT_Glyph_To_Bitmap(&glyph, mode, NULL, 1));
        FT_BitmapGlyph result = (FT_BitmapGlyph)glyph; bitmap = &result->bitmap; left = result->left; top = result->top;
    } else {
        check(FT_Render_Glyph(face->glyph, mode)); bitmap = &face->glyph->bitmap; left = face->glyph->bitmap_left; top = face->glyph->bitmap_top;
    }
    int horizontal = bitmap->pixel_mode == FT_PIXEL_MODE_LCD, vertical = bitmap->pixel_mode == FT_PIXEL_MODE_LCD_V;
    unsigned width = horizontal ? bitmap->width / 3 : bitmap->width, height = vertical ? bitmap->rows / 3 : bitmap->rows;
    int major, minor, patch; FT_Library_Version(library, &major, &minor, &patch);
    printf("%d.%d.%d %u %u %d %d %d\n", major, minor, patch, width, height, left, top, horizontal || vertical);
    for (unsigned y = 0; y < height; y++) for (unsigned x = 0; x < width; x++) {
        unsigned char *row = bitmap->buffer + (vertical ? y * 3 : y) * bitmap->pitch;
        if (horizontal || vertical) {
            unsigned c[3]; for (int channel = 0; channel < 3; channel++) c[channel] = horizontal ? row[x * 3 + channel] : row[x + channel * bitmap->pitch];
            printf("%02X%02X%02XFF", c[layout == 2 || layout == 4 ? 2 : 0], c[1], c[layout == 2 || layout == 4 ? 0 : 2]);
        } else {
            unsigned alpha = bitmap->pixel_mode == FT_PIXEL_MODE_MONO ? ((row[x >> 3] & (0x80 >> (x & 7))) ? 255 : 0) : row[x] * 255 / (bitmap->num_grays - 1);
            printf("FFFFFF%02X", alpha);
        }
    }
    puts(""); if (glyph) FT_Done_Glyph(glyph); if (stroker) FT_Stroker_Done(stroker); FT_Done_Face(face); FT_Done_FreeType(library); return 0;
}
