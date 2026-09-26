// Minimal host for the immutable upstream geometry; no rendering algorithm is duplicated here.
#include <algorithm>
#include <cfloat>
#include <cmath>
#include <iomanip>
#include <iostream>
#include <vector>

using real_t = float;
using RID = int;
template<class T, class U> constexpr auto MIN(T a, U b) { return a < b ? a : b; }
template<class T, class U> constexpr auto MAX(T a, U b) { return a > b ? a : b; }
enum Side { SIDE_LEFT, SIDE_TOP, SIDE_RIGHT, SIDE_BOTTOM };
enum Corner { CORNER_TOP_LEFT, CORNER_TOP_RIGHT, CORNER_BOTTOM_RIGHT, CORNER_BOTTOM_LEFT };
namespace Math {
constexpr double PI = 3.1415926535897932384626433832795;
inline float cos(float value) { return std::cos(value); }
inline float sin(float value) { return std::sin(value); }
inline bool is_zero_approx(float value) { return std::abs(value) < 0.00001f; }
}
struct Vector2 {
    union { float x; float width; }; union { float y; float height; };
    Vector2(float a = 0, float b = 0) : x(a), y(b) {}
    Vector2 operator+(Vector2 b) const { return {x + b.x, y + b.y}; }
    Vector2 operator-(Vector2 b) const { return {x - b.x, y - b.y}; }
    Vector2 operator-() const { return {-x, -y}; }
    Vector2 operator*(Vector2 b) const { return {x * b.x, y * b.y}; }
    Vector2 operator/(Vector2 b) const { return {x / b.x, y / b.y}; }
    Vector2 &operator+=(Vector2 b) { x += b.x; y += b.y; return *this; }
    Vector2 minf(float b) const { return {MIN(x, b), MIN(y, b)}; }
    Vector2 maxf(float b) const { return {MAX(x, b), MAX(y, b)}; }
    bool is_zero_approx() const { return Math::is_zero_approx(x) && Math::is_zero_approx(y); }
};
using Point2 = Vector2;
struct Rect2 {
    Vector2 position, size;
    Rect2(float x = 0, float y = 0, float width = 0, float height = 0) : position(x, y), size(width, height) {}
    Rect2 grow_individual(float left, float top, float right, float bottom) const {
        auto result = *this; result.position.x -= left; result.position.y -= top;
        result.size.x += left + right; result.size.y += top + bottom; return result;
    }
    Rect2 grow(float amount) const { return grow_individual(amount, amount, amount, amount); }
    Rect2 grow_side(Side side, float amount) const {
        return grow_individual(side == SIDE_LEFT ? amount : 0, side == SIDE_TOP ? amount : 0,
            side == SIDE_RIGHT ? amount : 0, side == SIDE_BOTTOM ? amount : 0);
    }
    Point2 get_center() const { return position + Vector2(size.x * 0.5f, size.y * 0.5f); }
    Rect2 merge(Rect2 other) const {
        auto begin = Vector2(MIN(position.x, other.position.x), MIN(position.y, other.position.y));
        auto end = Vector2(MAX(position.x + size.x, other.position.x + other.size.x), MAX(position.y + size.y, other.position.y + other.size.y));
        return {begin.x, begin.y, end.x - begin.x, end.y - begin.y};
    }
};
struct Color { float r, g, b, a; Color(float red = 0, float green = 0, float blue = 0, float alpha = 1) : r(red), g(green), b(blue), a(alpha) {} };
template<class T> struct Vector : std::vector<T> { using std::vector<T>::vector; T *ptrw() { return this->data(); } };
struct TextServer { static float get_current_drawn_item_oversampling() { return 1.0f; } };
struct RenderingServer {
    Vector<int> indices; Vector<Vector2> vertices, uvs; Vector<Color> colors;
    static RenderingServer *get_singleton() { static RenderingServer instance; return &instance; }
    void clear() { indices.clear(); vertices.clear(); uvs.clear(); colors.clear(); }
    void canvas_item_add_triangle_array(RID, const Vector<int> &i, const Vector<Vector2> &v, const Vector<Color> &c, const Vector<Vector2> &u) { indices = i; vertices = v; colors = c; uvs = u; }
};
struct StyleBoxFlat {
    Color bg_color{.6f, .6f, .6f}, shadow_color{0, 0, 0, .6f}, border_color{.8f, .8f, .8f};
    float border_width[4]{}, expand_margin[4]{}, corner_radius[4]{};
    bool draw_center = true, blend_border = false, anti_aliased = true;
    Vector2 skew, shadow_offset;
    int corner_detail = 8, shadow_size = 0;
    float aa_size = 1;
    Rect2 get_draw_rect(const Rect2 &) const;
    void draw(RID, const Rect2 &) const;
};
inline void output_geometry(const StyleBoxFlat &style, Rect2 rect) {
    auto server = RenderingServer::get_singleton(); server->clear(); style.draw(0, rect);
    auto bounds = style.get_draw_rect(rect);
    for (unsigned i = 0; i < server->vertices.size(); i++) {
        const auto p = server->vertices[i]; const auto uv = server->uvs[i]; const auto color = server->colors[i];
        if (!std::isfinite(p.x) || !std::isfinite(p.y) || !std::isfinite(uv.x) || !std::isfinite(uv.y) ||
            !std::isfinite(color.r) || !std::isfinite(color.g) || !std::isfinite(color.b) || !std::isfinite(color.a)) { std::cout << "null\n"; return; }
    }
    std::cout << std::setprecision(9) << "{\"drawRect\":[" << bounds.position.x << ',' << bounds.position.y << ',' << bounds.size.x << ',' << bounds.size.y << "],\"vertices\":[";
    for (unsigned i = 0; i < server->vertices.size(); i++) {
        if (i) std::cout << ',';
        const auto p = server->vertices[i]; const auto uv = server->uvs[i]; const auto c = server->colors[i];
        std::cout << '[' << p.x << ',' << p.y << ',' << c.r << ',' << c.g << ',' << c.b << ',' << c.a << ',' << uv.x << ',' << uv.y << ']';
    }
    std::cout << "],\"indices\":[";
    for (unsigned i = 0; i < server->indices.size(); i++) { if (i) std::cout << ','; std::cout << server->indices[i]; }
    std::cout << "]}\n";
}
