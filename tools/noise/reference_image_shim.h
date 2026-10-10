#include <algorithm>
#include <cstdint>
#include <iomanip>
#include <iostream>
#include <memory>
#include <vector>

using real_t = float;
#define MAX(a, b) ((a) > (b) ? (a) : (b))
#define ERR_FAIL_COND_V(condition, value) if (condition) return value
#define memnew(value) new value

template<class T> class Vector {
    std::shared_ptr<std::vector<T>> data = std::make_shared<std::vector<T>>();
    void detach() { if (!data.unique()) data = std::make_shared<std::vector<T>>(*data); }
public:
    struct Write { Vector *owner; T &operator[](int i) { owner->detach(); return (*owner->data)[i]; } } write{this};
    Vector() = default;
    Vector(const Vector &other): data(other.data), write{this} {}
    Vector &operator=(const Vector &other) { data = other.data; return *this; }
    void resize(int count) { detach(); data->resize(count); }
    int size() const { return data->size(); }
    const T &operator[](int i) const { return (*data)[i]; }
    const T *ptr() const { return data->data(); }
    T *ptrw() { detach(); return data->data(); }
};
template<class T> class Ref {
    std::shared_ptr<T> value;
public:
    Ref() = default;
    Ref(T *pointer): value(pointer) {}
    T *operator->() const { return value.get(); }
    void unref() { value.reset(); }
};
class Image {
    int width, height;
    Vector<uint8_t> pixels;
public:
    enum Format { FORMAT_L8, FORMAT_RGBA8 = 5 };
    Format format;
    Image(int w, int h, bool, Format f, Vector<uint8_t> bytes): width(w), height(h), pixels(bytes), format(f) {}
    Format get_format() const { return format; }
    int get_width() const { return width; }
    int get_height() const { return height; }
    Vector<uint8_t> get_data() const { return pixels; }
    static int get_format_pixel_size(Format format) { return format == FORMAT_L8 ? 1 : 4; }
};
struct Math {
    static float smoothstep(float a, float b, float value) { auto x = std::clamp((value - a) / (b - a), 0.0f, 1.0f); return x * x * (3 - 2 * x); }
};

// The stock Noise templates are inserted here by build_rgba_reference.py.
