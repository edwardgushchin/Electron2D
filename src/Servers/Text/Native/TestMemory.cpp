#include "unicode/ubrk.h"
#include "unicode/uclean.h"
#include "unicode/uspoof.h"
#include "cmemory.h"
#include <cstdio>
#include <cstdlib>
#include <cstddef>
#include <limits>
#include <mutex>
#include <thread>
#include <vector>

extern "C" int32_t e2d_text_memory(UMemAllocFn *, UMemReallocFn *, UMemFreeFn *);
extern "C" int32_t e2d_text_init(const void *);
extern "C" UBreakIterator *e2d_break_open(int32_t, const char *, const uint16_t *, int32_t, int32_t *);
extern "C" void e2d_break_close(UBreakIterator *);
extern "C" int32_t e2d_break_next(UBreakIterator *);
extern "C" int32_t e2d_text_case(const uint16_t *, int32_t, const char *, int32_t, uint16_t *, int32_t, int32_t *);
extern "C" USpoofChecker *e2d_spoof_open(int32_t *);
extern "C" void e2d_spoof_close(USpoofChecker *);
extern "C" int32_t e2d_spoof_skeleton(const USpoofChecker *, const uint16_t *, int32_t, uint16_t *, int32_t, int32_t *);

static void require(bool value, const char *message) {
    if (!value) { std::fprintf(stderr, "%s\n", message); std::abort(); }
}
struct alignas(std::max_align_t) Block { size_t size; };
static std::mutex gate;
static size_t current, peak, calls, fail_at, failures;
static void *allocate(const void *, size_t size) {
    std::lock_guard<std::mutex> lock(gate);
    if (++calls == fail_at) { ++failures; return nullptr; }
    if (size > std::numeric_limits<size_t>::max() - sizeof(Block)) return nullptr;
    auto block = static_cast<Block *>(std::malloc(sizeof(Block) + size));
    if (!block) return nullptr;
    block->size = size; current += size;
    if (current > peak) peak = current;
    return block + 1;
}
static void release(const void *, void *value) {
    if (!value) return;
    std::lock_guard<std::mutex> lock(gate);
    auto block = static_cast<Block *>(value) - 1;
    require(current >= block->size, "Invalid allocation ownership");
    current -= block->size; std::free(block);
}
static void *resize(const void *context, void *value, size_t size) {
    if (!value) return allocate(context, size);
    std::lock_guard<std::mutex> lock(gate);
    if (++calls == fail_at) { ++failures; return nullptr; }
    if (size > std::numeric_limits<size_t>::max() - sizeof(Block)) return nullptr;
    auto block = static_cast<Block *>(value) - 1;
    const auto previous = block->size;
    block = static_cast<Block *>(std::realloc(block, sizeof(Block) + size));
    if (!block) return nullptr;
    block->size = size; current = current - previous + size;
    if (current > peak) peak = current;
    return block + 1;
}
static void *other(const void *, size_t) { std::abort(); }

int main(int argc, char **argv) {
    if (argc != 3) return 1;
    require(e2d_text_init(nullptr) == U_INVALID_STATE_ERROR, "Unregistered data initialization must reject");
    require(e2d_text_memory(nullptr, resize, release) == U_ILLEGAL_ARGUMENT_ERROR, "Incomplete callback set must reject");
    std::vector<std::thread> workers;
    for (int i = 0; i < 4; ++i) workers.emplace_back([] {
        require(e2d_text_memory(allocate, resize, release) == 0, "Concurrent identical registration failed");
    });
    for (auto &worker : workers) worker.join();
    require(e2d_text_memory(other, resize, release) == U_INVALID_STATE_ERROR, "Allocator replacement must reject");
    auto value = static_cast<unsigned char *>(uprv_malloc(32));
    require(value && current == 32, "Actual ICU allocation bypassed the callback"); value[0] = 73;
    fail_at = calls + 1;
    require(!uprv_realloc(value, 64) && value[0] == 73 && current == 32, "Failed resize changed ownership or data");
    fail_at = 0;
    value = static_cast<unsigned char *>(uprv_realloc(value, 64));
    require(value && value[0] == 73 && current == 64, "Successful resize lost data or accounting");
    value = static_cast<unsigned char *>(uprv_realloc(value, 7));
    require(value && value[0] == 73 && current == 7, "Shrinking did not replace the contribution");
    auto zero = uprv_realloc(value, 0); uprv_free(zero); uprv_free(uprv_malloc(0));
    require(current == 0, "ICU zero-size sentinel must own no payload");
    calls = failures = peak = 0; fail_at = std::strtoull(argv[2], nullptr, 10);

    FILE *file = std::fopen(argv[1], "rb");
    require(file && !std::fseek(file, 0, SEEK_END), "Data file unavailable");
    long size = std::ftell(file);
    require(size > 0 && size < 64 * 1024 * 1024 && !std::fseek(file, 0, SEEK_SET), "Invalid data size");
    std::vector<unsigned char> data(static_cast<size_t>(size));
    require(std::fread(data.data(), 1, data.size(), file) == data.size(), "Data read failed"); std::fclose(file);
    int32_t error = e2d_text_init(data.data());
    const char16_t sample[] = u"ภาษาไทยภาษาไทย 中华人民共和国 Cafe\u0301";
    const auto text = reinterpret_cast<const uint16_t *>(sample);
    const auto length = static_cast<int32_t>(sizeof(sample) / sizeof(sample[0]) - 1);
    if (!fail_at) require(error <= 0, "Normal data initialization failed");
    if (error <= 0 && !failures) {
        for (auto locale : {"th", "zh", "ja", "en"}) {
            for (int kind : {0, 1, 2}) {
                auto iterator = e2d_break_open(kind, locale, text, length, &error);
                if (!fail_at) require(iterator && error <= 0, "Normal iterator creation failed");
                if (iterator && error <= 0 && !failures) {
                    int boundaries = 0; ubrk_first(iterator);
                    int32_t boundary;
                    while ((boundary = e2d_break_next(iterator)) >= 0 && !failures) ++boundaries;
                    if (!fail_at) require(boundary == UBRK_DONE, "Normal traversal returned a native error");
                    if (!fail_at) require(boundaries > 0, "Actual boundary traversal failed");
                }
                if (iterator) e2d_break_close(iterator);
                if (failures) break;
            }
            if (failures) break;
        }
        uint16_t output[256];
        if (!failures) e2d_text_case(text, length, "nl", 1, output, 256, &error);
        if (!fail_at) require(error <= 0, "Normal titlecase failed");
        auto checker = failures ? nullptr : e2d_spoof_open(&error);
        if (!fail_at) require(checker && error <= 0, "Normal security checker failed");
        if (checker) {
            e2d_spoof_skeleton(checker, text, length, output, 256, &error);
            if (!fail_at) require(error <= 0, "Normal skeleton failed");
            e2d_spoof_close(checker);
        }
    }
    const size_t retained = current;
    u_cleanup();
    require(current == 0, "ICU cleanup leaked owned payload after success or failure");
    require(!fail_at || (calls >= fail_at && failures == 1), "Requested failure position was not exercised");
    std::printf("{\"Calls\":%zu,\"Peak\":%zu,\"Retained\":%zu,\"Failures\":%zu,\"Current\":%zu}\n", calls, peak, retained, failures, current);
}
