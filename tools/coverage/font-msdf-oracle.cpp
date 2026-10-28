#include "font-memory.h"
// Independent glyph rasterization through public FreeType and msdfgen APIs.
#include <ft2build.h>
#include FT_FREETYPE_H
#include FT_OUTLINE_H
#include "msdfgen.h"
#include <algorithm>
#include <cstdio>
#include <cstdlib>

struct OutlineContext { msdfgen::Shape shape; msdfgen::Contour *contour = nullptr; msdfgen::Point2 at; };
static msdfgen::Point2 point(const FT_Vector *p) { return {p->x / 60.f, p->y / 60.f}; }
static int move(const FT_Vector *to, void *user) {
    auto &c = *static_cast<OutlineContext *>(user);
    if (!c.contour || !c.contour->edges.empty()) c.contour = &c.shape.addContour();
    c.at = point(to); return 0;
}
static int line(const FT_Vector *to, void *user) {
    auto &c = *static_cast<OutlineContext *>(user); const auto p = point(to);
    if (p != c.at) c.contour->addEdge(new msdfgen::LinearSegment(c.at, p)); c.at = p; return 0;
}
static int conic(const FT_Vector *control, const FT_Vector *to, void *user) {
    auto &c = *static_cast<OutlineContext *>(user); const auto p = point(to);
    c.contour->addEdge(new msdfgen::QuadraticSegment(c.at, point(control), p)); c.at = p; return 0;
}
static int cubic(const FT_Vector *one, const FT_Vector *two, const FT_Vector *to, void *user) {
    auto &c = *static_cast<OutlineContext *>(user); const auto p = point(to);
    c.contour->addEdge(new msdfgen::CubicSegment(c.at, point(one), point(two), p)); c.at = p; return 0;
}
int main(int argc, char **argv) {
    prepare_font_memory();
    if (argc != 8) return 1;
    FT_Library library = nullptr; FT_Face face = nullptr;
    if (FT_Init_FreeType(&library) || FT_New_Face(library, argv[1], 0, &face)) return 2;
    const int size = std::atoi(argv[2]), range = std::atoi(argv[3]), hinting = std::atoi(argv[4]);
    const auto character = static_cast<FT_ULong>(std::strtoul(argv[5], nullptr, 10));
    const float strength = std::strtof(argv[6], nullptr), shear = std::strtof(argv[7], nullptr);
    if (FT_Set_Char_Size(face, size * 64, size * 64, 72, 72)) return 3;
    const int flags = FT_LOAD_NO_BITMAP | (hinting == 0 ? FT_LOAD_NO_HINTING : hinting == 1 ? FT_LOAD_TARGET_LIGHT : 0);
    if (FT_Load_Glyph(face, FT_Get_Char_Index(face, character), flags)) return 4;
    if (strength && FT_Outline_Embolden(&face->glyph->outline, static_cast<FT_Pos>(strength * size * 4))) return 5;
    if (shear) { FT_Matrix matrix = {65536, 0, static_cast<FT_Fixed>(shear * 65536), 65536}; FT_Outline_Transform(&face->glyph->outline, &matrix); }
    OutlineContext c; FT_Outline_Funcs functions = {move, line, conic, cubic, 0, 0};
    if (FT_Outline_Decompose(&face->glyph->outline, &functions, &c)) return 6;
    if (!c.shape.contours.empty() && c.shape.contours.back().edges.empty()) c.shape.contours.pop_back();
    if (FT_Outline_Get_Orientation(&face->glyph->outline) == FT_ORIENTATION_POSTSCRIPT)
        for (auto &contour : c.shape.contours) contour.reverse();
    c.shape.setYAxisOrientation(msdfgen::Y_DOWNWARD); c.shape.normalize();
    if (!c.shape.validate() || c.shape.contours.empty()) return 7;
    const auto bounds = c.shape.getBounds(range); const int width = static_cast<int>(bounds.r - bounds.l), height = static_cast<int>(bounds.t - bounds.b);
    msdfgen::edgeColoringSimple(c.shape, 3.0); msdfgen::Bitmap<float, 4> image(width, height);
    msdfgen::generateMTSDF(image, c.shape, msdfgen::Projection({1, 1}, {-bounds.l, -bounds.b}), msdfgen::Range(range), msdfgen::MSDFGeneratorConfig(true, msdfgen::ErrorCorrectionConfig()));
    int major, minor, patch; FT_Library_Version(library, &major, &minor, &patch);
    std::printf("%d.%d.%d %d %d %.9g %.9g\n", major, minor, patch, width + 2, height + 2, bounds.l - 1, bounds.t + 1);
    for (int y = -1; y <= height; ++y) for (int x = -1; x <= width; ++x) for (int channel = 0; channel < 4; ++channel)
        std::printf("%02X", y < 0 || y >= height || x < 0 || x >= width ? 0 : static_cast<unsigned int>(std::clamp(image(x, y)[channel] * 256.f, 0.f, 255.f)));
    std::printf("\n"); FT_Done_Face(face); FT_Done_FreeType(library); return 0;
}
