/* Independent bitmap oracle for FontRenderingTests. Uses only public FreeType APIs.
 * Build: cc font-raster-oracle.c $(pkg-config --cflags --libs freetype2) -o font-raster-oracle
 * Run with LD_PRELOAD pointing at the exact packaged MonoGame.Library.FreeType native
 * library: ./font-raster-oracle OpenSans_SemiBold.woff2 SIZE PHASE OUTLINE
 * Profiles (SIZE PHASE OUTLINE): 16 0 0; 16 16 0; 32 0 0; 16 0 64; Label outer: 16 0 128.
 * Phase and outline use 26.6 units. The first output line reports the loaded library
 * version, width, height, left and top. The second is row-major hexadecimal alpha.
 * FontRenderingTests expects the package's reported FreeType version 2.13.3.
 */
#include <stdio.h>
#include <stdlib.h>
#include <ft2build.h>
#include FT_FREETYPE_H
#include FT_GLYPH_H
#include FT_STROKER_H

static void check(FT_Error error) {
    if (error) { fprintf(stderr, "FreeType error %d\n", error); exit(1); }
}

int main(int argc, char **argv) {
    if (argc != 5) { fputs("Expected FONT SIZE PHASE OUTLINE\n", stderr); return 2; }
    FT_Library library; FT_Face face; FT_Glyph glyph = NULL; FT_Stroker stroker = NULL;
    check(FT_Init_FreeType(&library));
    int major, minor, patch; FT_Library_Version(library, &major, &minor, &patch);
    check(FT_New_Face(library, argv[1], 0, &face));
    check(FT_Set_Char_Size(face, 0, atoi(argv[2]) * 64, 72, 72));
    FT_Vector delta = { atoi(argv[3]), 0 }; FT_Set_Transform(face, NULL, &delta);
    check(FT_Load_Char(face, 'A', FT_LOAD_TARGET_LIGHT | FT_LOAD_NO_BITMAP));
    FT_Bitmap *bitmap; int left, top;
    if (atoi(argv[4])) {
        check(FT_Get_Glyph(face->glyph, &glyph)); check(FT_Stroker_New(library, &stroker));
        FT_Stroker_Set(stroker, atoi(argv[4]), FT_STROKER_LINECAP_BUTT, FT_STROKER_LINEJOIN_ROUND, 0);
        check(FT_Glyph_Stroke(&glyph, stroker, 1));
        check(FT_Glyph_To_Bitmap(&glyph, FT_RENDER_MODE_NORMAL, NULL, 1));
        FT_BitmapGlyph converted = (FT_BitmapGlyph)glyph;
        bitmap = &converted->bitmap; left = converted->left; top = converted->top;
    } else {
        check(FT_Render_Glyph(face->glyph, FT_RENDER_MODE_NORMAL));
        bitmap = &face->glyph->bitmap; left = face->glyph->bitmap_left; top = face->glyph->bitmap_top;
    }
    if (bitmap->pixel_mode != FT_PIXEL_MODE_GRAY) { fputs("Expected grayscale bitmap\n", stderr); return 1; }
    printf("%d.%d.%d %u %u %d %d\n", major, minor, patch, bitmap->width, bitmap->rows, left, top);
    for (unsigned y = 0; y < bitmap->rows; y++)
        for (unsigned x = 0; x < bitmap->width; x++) printf("%02X", bitmap->buffer[y * bitmap->pitch + x]);
    puts("");
    if (glyph) FT_Done_Glyph(glyph);
    if (stroker) FT_Stroker_Done(stroker);
    FT_Done_Face(face); FT_Done_FreeType(library);
    return 0;
}
