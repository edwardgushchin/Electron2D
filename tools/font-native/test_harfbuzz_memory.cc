#include <ft2build.h>
#include FT_FREETYPE_H
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <cstddef>
#include <limits>
#include <mutex>
#include <thread>
#include <vector>

extern "C" int e2d_ft_hb_memory(void *(*)(size_t), void *(*)(void *, size_t), void (*)(void *));
extern "C" void *e2d_ft_hb_malloc(size_t);
extern "C" void *e2d_ft_hb_calloc(size_t, size_t);
extern "C" void *e2d_ft_hb_realloc(void *, size_t);
extern "C" void e2d_ft_hb_free(void *);

static void require(bool value, const char *message) {
    if (!value) { std::fprintf(stderr, "%s\n", message); std::abort(); }
}
struct alignas(std::max_align_t) Block { size_t size; };
static std::mutex gate;
static size_t current, peak, calls, fail_at, failures;
static void *allocate(size_t size) {
    std::lock_guard<std::mutex> lock(gate);
    if (++calls == fail_at) { ++failures; return nullptr; }
    if (size > std::numeric_limits<size_t>::max() - sizeof(Block)) return nullptr;
    auto block = static_cast<Block *>(std::malloc(sizeof(Block) + size));
    if (!block) return nullptr;
    block->size = size;
    current += size;
    if (current > peak) peak = current;
    return block + 1;
}
static void release(void *value) {
    if (!value) return;
    std::lock_guard<std::mutex> lock(gate);
    auto block = static_cast<Block *>(value) - 1;
    current -= block->size;
    std::free(block);
}
static void *resize(void *value, size_t size) {
    if (!value) return allocate(size);
    std::lock_guard<std::mutex> lock(gate);
    if (++calls == fail_at) { ++failures; return nullptr; }
    if (size > std::numeric_limits<size_t>::max() - sizeof(Block)) return nullptr;
    auto block = static_cast<Block *>(value) - 1;
    size_t previous = block->size;
    block = static_cast<Block *>(std::realloc(block, sizeof(Block) + size));
    if (!block) return nullptr;
    block->size = size;
    current = current - previous + size;
    if (current > peak) peak = current;
    return block + 1;
}
static void exercise(const unsigned char *data, long size, bool normal) {
    FT_Library library = nullptr;
    FT_Face face = nullptr;
    int error = FT_Init_FreeType(&library);
    if (!error) error = FT_New_Memory_Face(library, data, size, 0, &face);
    if (!error) error = FT_Set_Pixel_Sizes(face, 0, 24);
    if (!error) error = FT_Load_Char(face, 'A', FT_LOAD_RENDER | FT_LOAD_FORCE_AUTOHINT);
    if (normal) require(!error && face->glyph->bitmap.width > 0, "Actual automatic hinting failed");
    if (face) FT_Done_Face(face);
    if (library) FT_Done_FreeType(library);
}
int main(int argc, char **argv) {
    if (argc != 2) return 1;
    require(!e2d_ft_hb_malloc(16), "Unregistered allocation must fail");
    require(e2d_ft_hb_memory(nullptr, resize, release) == -1, "Missing allocator must reject");
    require(!e2d_ft_hb_memory(allocate, resize, release), "Initial registration failed");
    require(!e2d_ft_hb_memory(allocate, resize, release), "Identical registration failed");
    require(e2d_ft_hb_memory(std::malloc, resize, release) == -1, "Allocator replacement must reject");
    auto value = static_cast<unsigned char *>(e2d_ft_hb_calloc(4, 8));
    require(value && current == 32, "Zeroed allocation not accounted");
    for (int i = 0; i < 32; ++i) require(value[i] == 0, "calloc did not clear payload");
    value[0] = 73;
    fail_at = calls + 1;
    require(!e2d_ft_hb_realloc(value, 64) && current == 32 && value[0] == 73, "Failed resize changed ownership");
    fail_at = 0;
    value = static_cast<unsigned char *>(e2d_ft_hb_realloc(value, 64));
    require(value && current == 64 && value[0] == 73, "Successful resize lost bytes");
    require(!e2d_ft_hb_realloc(value, 0) && current == 0, "Zero resize did not release ownership");
    size_t previous_calls = calls;
    require(!e2d_ft_hb_calloc(2, std::numeric_limits<size_t>::max()) && calls == previous_calls, "calloc overflow reached allocator");
    e2d_ft_hb_free(nullptr);
    FILE *file = std::fopen(argv[1], "rb");
    require(file && !std::fseek(file, 0, SEEK_END), "Font unavailable");
    long size = std::ftell(file);
    require(size > 0 && size < 16 * 1024 * 1024 && !std::fseek(file, 0, SEEK_SET), "Invalid font size");
    std::vector<unsigned char> data(static_cast<size_t>(size));
    require(std::fread(data.data(), 1, data.size(), file) == data.size(), "Font read failed");
    std::fclose(file);
    exercise(data.data(), size, true);
    size_t baseline = current;
    previous_calls = calls;
    exercise(data.data(), size, true);
    size_t requests = calls - previous_calls;
    require(requests && current == baseline, "Automatic hinting bypassed callbacks or leaked per-face storage");
    for (size_t i = 1; i <= requests; ++i) {
        fail_at = calls + i;
        size_t previous_failures = failures;
        exercise(data.data(), size, false);
        require(failures == previous_failures + 1 && current == baseline, "Autohinter allocation refusal leaked or was not reached");
    }
    fail_at = 0;
    std::vector<std::thread> workers;
    for (int i = 0; i < 4; ++i) workers.emplace_back([&] {
        require(!e2d_ft_hb_memory(allocate, resize, release), "Concurrent identical registration failed");
        for (int j = 0; j < 128; ++j) exercise(data.data(), size, true);
    });
    for (auto &worker : workers) worker.join();
    require(current == baseline && peak > baseline, "Concurrent ownership or transient peak failed");
    std::printf("HarfBuzz allocator: %zu autohinter refusal positions, 512 concurrent autohinter faces, current=%zu peak=%zu\n", requests, current, peak);
}
