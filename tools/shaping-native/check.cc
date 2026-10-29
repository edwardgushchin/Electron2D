#include <hb.h>
#include <hb-ot.h>
#include "../enet-native/memory.h"
#include <dlfcn.h>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <fstream>
#include <vector>
#undef NDEBUG
#include <cassert>

#define FUNCTIONS(F) \
    F(hb_version_string) F(hb_shape_list_shapers) F(hb_blob_create) F(hb_blob_destroy) F(hb_blob_get_empty) \
    F(hb_face_create) F(hb_face_destroy) F(hb_face_get_empty) F(hb_face_get_glyph_count) \
    F(hb_font_create) F(hb_font_destroy) F(hb_font_get_empty) F(hb_font_set_scale) F(hb_font_set_variations) F(hb_ot_font_set_funcs) \
    F(hb_buffer_create) F(hb_buffer_destroy) F(hb_buffer_get_empty) F(hb_buffer_add_utf32) F(hb_buffer_guess_segment_properties) \
    F(hb_buffer_set_direction) F(hb_buffer_set_language) F(hb_language_from_string) F(hb_buffer_allocation_successful) \
    F(hb_buffer_get_glyph_infos) F(hb_buffer_get_glyph_positions) F(hb_shape_full)
struct API {
#define FIELD(name) decltype(&::name) name;
    FUNCTIONS(FIELD)
#undef FIELD
};
static E2DMemorySnapshot state() { E2DMemorySnapshot value; e2d_memory_snapshot(&value); return value; }
static void retired() {
    auto s = state();
    if (s.nativeCurrent || s.allocated != s.retired) {
        std::fprintf(stderr, "Native shaping did not drain at exit: %llu bytes\n", (unsigned long long)s.nativeCurrent);
        std::abort();
    }
    std::fprintf(stderr, "Native shaping exit drain passed; peak=%llu\n", (unsigned long long)s.peak);
}
struct Glyph { unsigned id, cluster, flags; int x, y, dx, dy; };
static void print(const std::vector<Glyph> &output) {
    std::putchar('[');
    for (size_t i = 0; i < output.size(); i++) {
        const auto &g = output[i];
        std::printf("%s[%u,%u,%u,%d,%d,%d,%d]", i ? "," : "",g.id,g.cluster,g.flags,g.x,g.y,g.dx,g.dy);
    }
    std::putchar(']');
}
static bool shape(API &h, const std::vector<char> &data, unsigned role, std::vector<Glyph> &output) {
    auto epoch = e2d_memory_failure_epoch();
    auto blob = h.hb_blob_create(data.data(), (unsigned)data.size(), HB_MEMORY_MODE_DUPLICATE, nullptr, nullptr);
    auto face = h.hb_face_create(blob, 0); h.hb_blob_destroy(blob);
    auto font = h.hb_font_create(face);
    auto buffer = h.hb_buffer_create();
    bool ok = face != h.hb_face_get_empty() && font != h.hb_font_get_empty() &&
        buffer != h.hb_buffer_get_empty() && h.hb_face_get_glyph_count(face) > 0;
    if (ok) {
        h.hb_ot_font_set_funcs(font); h.hb_font_set_scale(font, 1536, 1536);
        if (role & 4) { hb_variation_t variation{HB_TAG('w','g','h','t'), 700.f}; h.hb_font_set_variations(font, &variation, 1); }
        static const uint32_t latin[] = {0x41,0x66,0x66,0x69,0x6e,0x65,0x20,0x65,0x301,0x20,0x1f600};
        static const uint32_t arabic[] = {0x628,0x628,0x20,0x627,0x644,0x633,0x644,0x627,0x645};
        static const uint32_t indic[] = {0x928,0x92e,0x938,0x94d,0x924,0x947,0x20,0x915,0x94d,0x937};
        auto text = (role & 8) ? indic : (role & 1) ? arabic : latin;
        unsigned length = (role & 8) ? sizeof(indic)/sizeof(*indic) : (role & 1) ? sizeof(arabic)/sizeof(*arabic) : sizeof(latin)/sizeof(*latin);
        auto language = h.hb_language_from_string((role & 1) ? "ar" : "en", -1);
        if (!language) ok = false;
        h.hb_buffer_set_language(buffer, language);
        h.hb_buffer_set_direction(buffer, (role & 1) ? HB_DIRECTION_RTL : HB_DIRECTION_LTR);
        h.hb_buffer_add_utf32(buffer, text, (int)length, 0, (int)length);
        h.hb_buffer_guess_segment_properties(buffer);
        hb_feature_t feature{HB_TAG('l','i','g','a'), 0, 0, (unsigned)-1};
        ok = h.hb_shape_full(font, buffer, (role & 2) ? &feature : nullptr, (role & 2) ? 1 : 0, nullptr) && ok;
        ok = h.hb_buffer_allocation_successful(buffer) && ok;
        if (ok) {
            unsigned n = 0, m = 0;
            auto infos = h.hb_buffer_get_glyph_infos(buffer, &n); auto positions = h.hb_buffer_get_glyph_positions(buffer, &m);
            assert(n == m);
            for (unsigned i = 0; i < n; i++) output.push_back({infos[i].codepoint, infos[i].cluster, infos[i].mask & 7,
                positions[i].x_advance, positions[i].y_advance, positions[i].x_offset, positions[i].y_offset});
        }
    }
    h.hb_buffer_destroy(buffer); h.hb_font_destroy(font); h.hb_face_destroy(face);
    return ok && epoch == e2d_memory_failure_epoch();
}
int main(int argc, char **argv) {
    assert(argc == 4 || argc == 5);
    assert(!state().nativeCurrent); assert(std::atexit(retired) == 0);
    void *library = dlopen(argv[1], RTLD_NOW | RTLD_LOCAL);
    if (!library) { std::fprintf(stderr, "%s\n", dlerror()); return 1; }
    auto selected = (void (*)(E2DMemoryFunctions *))dlsym(library, "e2d_shaping_memory");
    if (selected) {
        E2DMemoryFunctions actual, expected; selected(&actual); e2d_memory_functions(&expected);
        assert(actual.allocate == expected.allocate && actual.zero == expected.zero &&
            actual.resize == expected.resize && actual.release == expected.release);
    }
    API h;
#define LOAD(name) h.name = (decltype(h.name))dlsym(library, #name); assert(h.name);
    FUNCTIONS(LOAD)
#undef LOAD
    assert(!std::strcmp(h.hb_version_string(), HB_VERSION_STRING));
    const char **shapers = h.hb_shape_list_shapers(); assert(shapers[0] && !std::strcmp(shapers[0], "ot") && !shapers[1]);
    std::ifstream input(argv[2], std::ios::binary); assert(input);
    std::vector<char> data((std::istreambuf_iterator<char>(input)), {}); assert(data.size() && data.size() <= UINT32_MAX);
    auto arm = (void (*)(size_t))dlsym(library, "e2d_shaping_test_arm");
    auto attempts = (size_t (*)())dlsym(library, "e2d_shaping_test_attempts");
    auto failures = (size_t (*)())dlsym(library, "e2d_shaping_test_failures");
    size_t position = argc == 5 ? std::strtoull(argv[4], nullptr, 10) : 0;
    assert(!position || arm);
    if (arm) arm(position);
    std::vector<Glyph> output; unsigned role = (unsigned)std::strtoul(argv[3], nullptr, 10);
    bool ok = shape(h, data, role, output); size_t calls = attempts ? attempts() : 0;
    if (position) assert(failures() == 1);
    if (arm) arm(0);
    auto live = state().nativeCurrent;
    std::vector<Glyph> retry;
    if (position) assert(shape(h, data, role, retry));
    else assert(ok);
    if (selected) assert(state().allocations > 0 && state().nativePeak > data.size());
    std::printf("{\"ok\":%s,\"requests\":%zu,\"live\":%llu,\"glyphs\":", ok ? "true" : "false", calls, (unsigned long long)live);
    print(output); std::printf(",\"retry\":"); print(retry); std::puts("}");
}
