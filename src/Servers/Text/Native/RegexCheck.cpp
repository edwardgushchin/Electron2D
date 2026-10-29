#include <cassert>
#include <chrono>
#include <cstdint>
#include <cstdio>
#include <string>
#include <vector>
#include <cstdlib>

extern "C" {
int e2d_text_memory(void *(*)(const void *, size_t), void *(*)(const void *, void *, size_t), void (*)(const void *, void *));
void *e2d_regex_create(const uint32_t *, size_t, int *, size_t *);
void e2d_regex_destroy(void *);
int e2d_regex_info(const void *, uint32_t *, uint32_t *, uint32_t *, const uint32_t **);
int e2d_regex_match(const void *, const uint32_t *, size_t, size_t, size_t *, size_t);
int e2d_regex_substitute(const void *, const uint32_t *, size_t, size_t, const uint32_t *, size_t, int, int, uint32_t *, size_t *);
int e2d_regex_error(int, uint32_t *, size_t);
}
static void *allocate(const void *, size_t size) { return std::malloc(size); }
static void *resize(const void *, void *value, size_t size) { return std::realloc(value, size); }
static void release(const void *, void *value) { std::free(value); }
static const uint32_t *data(const std::u32string &text) { return reinterpret_cast<const uint32_t *>(text.c_str()); }
static void *compile(const std::u32string &pattern) {
    int error = 0; size_t offset = 0; void *code = e2d_regex_create(data(pattern), pattern.size(), &error, &offset); assert(code); return code;
}
int main() {
    assert(e2d_text_memory(allocate, resize, release) == 0);
    int error = 0; size_t offset = 0;
    assert(!e2d_regex_create(data(U"("), 1, &error, &offset) && error > 0 && offset <= 1);
    uint32_t message[256]; assert(e2d_regex_error(error, message, 256) > 0);
    for (int iteration = 0; iteration < 512; iteration++) {
        auto code = compile(U"(?<item>a)(b)?"); uint32_t groups = 0, names = 0, width = 0; const uint32_t *table = nullptr;
        assert(e2d_regex_info(code, &groups, &names, &width, &table) == 0 && groups == 2 && names == 1 && width > 1 && table[0] == 1);
        const std::u32string subject = U"😀a"; size_t positions[6];
        assert(e2d_regex_match(code, data(subject), subject.size(), 0, positions, 6) > 0 && positions[0] == 1 && positions[1] == 2 && positions[4] == SIZE_MAX);
        assert(e2d_regex_match(code, data(subject), subject.size(), 0, positions, 1) < 0);
        assert(e2d_regex_match(code, data(subject), subject.size(), 4, positions, 6) < 0);
        size_t length = 1; uint32_t output[256]; const std::u32string replacement = U"${item}$$";
        assert(e2d_regex_substitute(code, data(subject), subject.size(), 0, data(replacement), replacement.size(), 1, 5000, output, &length) == -48 && length > 1);
        length = 256;
        assert(e2d_regex_substitute(code, data(subject), subject.size(), 0, data(replacement), replacement.size(), 1, 5000, output, &length) == 1 && length == 3 && output[0] == 0x1F600 && output[2] == '$');
        e2d_regex_destroy(code);
    }
    auto code = compile(U"(*NO_START_OPT)(?=.)"); const std::u32string large(1000000, U'a'); std::vector<uint32_t> output(2); size_t length = output.size();
    auto started = std::chrono::steady_clock::now();
    assert(e2d_regex_substitute(code, data(large), large.size(), 0, data(U"x"), 1, 1, 1, output.data(), &length) == -37);
    assert(std::chrono::steady_clock::now() - started < std::chrono::seconds(2)); e2d_regex_destroy(code);
    std::puts("PCRE2 bridge: 512 ownership cycles, metadata, bounds, sizing and native deadline passed.");
}
