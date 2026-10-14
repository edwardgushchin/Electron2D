#include "msdfgen.h"
#include <algorithm>
#include <cmath>
#include <cstdint>
#include <memory>
#include <new>

#if defined(_WIN32)
#define E2D_FONT_API extern "C" __declspec(dllexport)
#else
#define E2D_FONT_API extern "C" __attribute__((visibility("default")))
#endif

struct E2DMSDFEdge {
    int32_t kind, contour;
    double ax, ay, bx, by, cx, cy, dx, dy;
};

// Cold generation borrows decomposed public outline coordinates; its byte result has a separate release.
E2D_FONT_API int32_t e2d_font_msdf_generate(const E2DMSDFEdge *edges, int32_t count,
        int32_t range, int32_t reverse, uint8_t **pixels, int32_t *width,
        int32_t *height, double *left, double *top) {
    if (!pixels || !width || !height || !left || !top) return 1;
    *pixels = nullptr; *width = *height = 0; *left = *top = 0;
    if (count < 0 || count > 131072 || (count && !edges) || range <= 0 || range > 16384) return 1;
    try {
        msdfgen::Shape shape;
        for (int32_t i = 0; i < count; ++i) {
            const auto &edge = edges[i];
            if (edge.contour < 0 || edge.contour > static_cast<int32_t>(shape.contours.size()) || edge.kind < 1 || edge.kind > 3) return 1;
            const double values[] = {edge.ax, edge.ay, edge.bx, edge.by, edge.cx, edge.cy, edge.dx, edge.dy};
            for (double value : values) if (!std::isfinite(value) || std::abs(value) > 1048576) return 1;
            if (edge.contour == static_cast<int32_t>(shape.contours.size())) shape.addContour();
            auto &contour = shape.contours[edge.contour];
            const msdfgen::Point2 a(edge.ax, edge.ay), b(edge.bx, edge.by), c(edge.cx, edge.cy), d(edge.dx, edge.dy);
            if (edge.kind == 1) contour.addEdge(new msdfgen::LinearSegment(a, b));
            else if (edge.kind == 2) contour.addEdge(new msdfgen::QuadraticSegment(a, b, c));
            else contour.addEdge(new msdfgen::CubicSegment(a, b, c, d));
        }
        if (shape.contours.empty()) return 0;
        if (reverse) for (auto &contour : shape.contours) contour.reverse();
        shape.setYAxisOrientation(msdfgen::Y_DOWNWARD);
        shape.normalize();
        if (!shape.validate()) return 1;
        const auto bounds = shape.getBounds(range);
        const double w = bounds.r - bounds.l, h = bounds.t - bounds.b;
        if (!std::isfinite(w) || !std::isfinite(h) || w < 0 || h < 0 || w > 16382 || h > 16382) return 2;
        const int32_t imageWidth = static_cast<int32_t>(w), imageHeight = static_cast<int32_t>(h);
        if (!imageWidth || !imageHeight) return 0;
        const int32_t paddedWidth = imageWidth + 2, paddedHeight = imageHeight + 2;
        const int64_t bytes = static_cast<int64_t>(paddedWidth) * paddedHeight * 4;
        if (bytes > 64 * 1024 * 1024) return 2;
        msdfgen::edgeColoringSimple(shape, 3.0);
        msdfgen::Bitmap<float, 4> image(imageWidth, imageHeight);
        msdfgen::generateMTSDF(image, shape,
            msdfgen::Projection(msdfgen::Vector2(1, 1), msdfgen::Vector2(-bounds.l, -bounds.b)),
            msdfgen::Range(range), msdfgen::MSDFGeneratorConfig(true, msdfgen::ErrorCorrectionConfig()));
        auto result = std::make_unique<uint8_t[]>(static_cast<size_t>(bytes));
        std::fill_n(result.get(), static_cast<size_t>(bytes), uint8_t(0));
        for (int y = 0; y < imageHeight; ++y) for (int x = 0; x < imageWidth; ++x)
            for (int c = 0; c < 4; ++c) {
                if (std::isnan(image(x, y)[c])) return 1;
                result[((y + 1) * paddedWidth + x + 1) * 4 + c] =
                    static_cast<uint8_t>(std::clamp(image(x, y)[c] * 256.f, 0.f, 255.f));
            }
        *width = paddedWidth; *height = paddedHeight; *left = bounds.l - 1; *top = bounds.t + 1;
        *pixels = result.release(); return 0;
    } catch (const std::bad_alloc &) { return 3; }
    catch (...) { return 4; }
}

E2D_FONT_API void e2d_font_msdf_free(uint8_t *pixels) { delete[] pixels; }
