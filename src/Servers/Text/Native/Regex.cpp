#include "pcre2.h"
#include <chrono>
#include <cstring>
#include <memory>

#if defined(_WIN32)
#define E2D_REGEX_API extern "C" __declspec(dllexport)
#else
#define E2D_REGEX_API extern "C" __attribute__((visibility("default")))
#endif

namespace {
struct Operation {
    std::chrono::steady_clock::time_point deadline;
    explicit Operation(int milliseconds) : deadline(std::chrono::steady_clock::now() + std::chrono::milliseconds(milliseconds)) {}
    uint32_t calls = 0;
    static int callout(pcre2_callout_block *, void *context) {
        auto &operation = *static_cast<Operation *>(context);
        return (++operation.calls & 4095) == 0 && std::chrono::steady_clock::now() >= operation.deadline ? PCRE2_ERROR_CALLOUT : 0;
    }
};
struct MatchContext {
    Operation operation;
    std::unique_ptr<pcre2_match_context, decltype(&pcre2_match_context_free)> context{pcre2_match_context_create(nullptr), pcre2_match_context_free};
    std::unique_ptr<pcre2_match_data, decltype(&pcre2_match_data_free)> data;
    explicit MatchContext(const pcre2_code *code, int milliseconds = 5000) : operation(milliseconds), data(pcre2_match_data_create_from_pattern(code, nullptr), pcre2_match_data_free) {
        if (context) {
            pcre2_set_heap_limit(context.get(), 32768);
            pcre2_set_callout(context.get(), &Operation::callout, &operation);
        }
    }
    bool valid() const { return context && data; }
};
}

E2D_REGEX_API pcre2_code *e2d_regex_create(const uint32_t *pattern, size_t length, int *error, size_t *offset) {
    return pcre2_compile(pattern, length, PCRE2_DUPNAMES | PCRE2_AUTO_CALLOUT, error, offset, nullptr);
}
E2D_REGEX_API void e2d_regex_destroy(pcre2_code *code) { pcre2_code_free(code); }
E2D_REGEX_API int e2d_regex_info(const pcre2_code *code, uint32_t *groups, uint32_t *names, uint32_t *width, const uint32_t **table) {
    if (!code) return PCRE2_ERROR_NULL;
    int result = pcre2_pattern_info(code, PCRE2_INFO_CAPTURECOUNT, groups);
    if (result == 0) result = pcre2_pattern_info(code, PCRE2_INFO_NAMECOUNT, names);
    if (result == 0) result = pcre2_pattern_info(code, PCRE2_INFO_NAMEENTRYSIZE, width);
    if (result == 0) result = pcre2_pattern_info(code, PCRE2_INFO_NAMETABLE, table);
    return result;
}
E2D_REGEX_API int e2d_regex_match(const pcre2_code *code, const uint32_t *subject, size_t length, size_t offset, size_t *positions, size_t capacity) {
    if (!code || !subject || !positions) return PCRE2_ERROR_NULL;
    MatchContext match(code); if (!match.valid()) return PCRE2_ERROR_NOMEMORY;
    if (capacity < 2 * static_cast<size_t>(pcre2_get_ovector_count(match.data.get()))) return PCRE2_ERROR_BADDATA;
    int result = pcre2_match(code, subject, length, offset, 0, match.data.get(), match.context.get());
    if (result >= 0) std::memcpy(positions, pcre2_get_ovector_pointer(match.data.get()), 2 * pcre2_get_ovector_count(match.data.get()) * sizeof(size_t));
    return result;
}
E2D_REGEX_API int e2d_regex_substitute(const pcre2_code *code, const uint32_t *subject, size_t length, size_t offset,
    const uint32_t *replacement, size_t replacement_length, int all, int milliseconds, uint32_t *output, size_t *output_length) {
    if (!code || !subject || !replacement || !output_length) return PCRE2_ERROR_NULL;
    if (milliseconds < 1 || milliseconds > 5000) return PCRE2_ERROR_BADDATA;
    MatchContext match(code, milliseconds); if (!match.valid()) return PCRE2_ERROR_NOMEMORY;
    uint32_t flags = PCRE2_SUBSTITUTE_OVERFLOW_LENGTH | PCRE2_SUBSTITUTE_UNSET_EMPTY | PCRE2_SUBSTITUTE_UNKNOWN_UNSET;
    if (all) flags |= PCRE2_SUBSTITUTE_GLOBAL;
    return pcre2_substitute(code, subject, length, offset, flags, match.data.get(), match.context.get(), replacement, replacement_length, output, output_length);
}
E2D_REGEX_API int e2d_regex_error(int error, uint32_t *output, size_t capacity) { return pcre2_get_error_message(error, output, capacity); }
