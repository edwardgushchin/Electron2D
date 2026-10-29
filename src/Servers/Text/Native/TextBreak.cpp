#include "unicode/ubrk.h"
#include "unicode/udata.h"
#include "unicode/uversion.h"
#include "unicode/uchar.h"
#include "unicode/uclean.h"
#include "unicode/uloc.h"
#include <atomic>
#include "TextMemory.h"

#if defined(_WIN32)
#define E2D_TEXT_API extern "C" __declspec(dllexport)
#else
#define E2D_TEXT_API extern "C" __attribute__((visibility("default")))
#endif

static std::atomic<bool> memory_ready{false};
static std::atomic_flag memory_gate = ATOMIC_FLAG_INIT;
static UMemAllocFn *memory_allocate;
static UMemReallocFn *memory_resize;
static UMemFreeFn *memory_release;
extern thread_local int32_t e2d_icu_break_error;
extern "C" int32_t e2d_icu_prepare_locales();

void *e2d_text_allocate(size_t size, void *) {
    return memory_ready.load(std::memory_order_acquire) ? memory_allocate(nullptr, size) : nullptr;
}
void e2d_text_release(void *value, void *) { if (value) memory_release(nullptr, value); }

// Process caches retain these callbacks; replacing them would mix allocation owners.
E2D_TEXT_API int32_t e2d_text_memory(UMemAllocFn *allocate, UMemReallocFn *resize, UMemFreeFn *release) {
    if (!allocate || !resize || !release) return U_ILLEGAL_ARGUMENT_ERROR;
    while (memory_gate.test_and_set(std::memory_order_acquire)) {}
    UErrorCode error = U_ZERO_ERROR;
    if (memory_ready.load(std::memory_order_relaxed)) {
        if (memory_allocate != allocate || memory_resize != resize || memory_release != release)
            error = U_INVALID_STATE_ERROR;
    } else {
        u_setMemoryFunctions(nullptr, allocate, resize, release, &error);
        if (U_SUCCESS(error)) {
            memory_allocate = allocate; memory_resize = resize; memory_release = release;
            memory_ready.store(true, std::memory_order_release);
        }
    }
    memory_gate.clear(std::memory_order_release);
    return error;
}

// The caller owns this aligned buffer for the remaining process lifetime.
E2D_TEXT_API int32_t e2d_text_init(const void *data) {
    if (!memory_ready.load(std::memory_order_acquire)) return U_INVALID_STATE_ERROR;
    UErrorCode error = U_ZERO_ERROR;
    // Prepare the fallible default before any iterator can use its reference-only getter.
    uloc_setDefault(nullptr, &error);
    if (U_FAILURE(error)) return error;
    error = static_cast<UErrorCode>(e2d_icu_prepare_locales());
    if (U_FAILURE(error)) return error;
    udata_setCommonData(data, &error);
    return error;
}

E2D_TEXT_API void e2d_text_versions(uint8_t *icu, uint8_t *unicode) {
    u_getVersion(icu);
    u_getUnicodeVersion(unicode);
}

E2D_TEXT_API int32_t e2d_text_is_nonprinting(uint32_t scalar) {
    return !u_isgraph(static_cast<UChar32>(scalar)) && !u_isblank(static_cast<UChar32>(scalar));
}

// Text remains borrowed until the caller resets or closes the iterator.
E2D_TEXT_API UBreakIterator *e2d_break_open(int32_t kind, const char *locale,
        const uint16_t *text, int32_t length, int32_t *error) {
    UErrorCode status = U_ZERO_ERROR;
    auto iterator = ubrk_open(static_cast<UBreakIteratorType>(kind), locale,
            reinterpret_cast<const UChar *>(text), length, &status);
    *error = status;
    return iterator;
}

E2D_TEXT_API int32_t e2d_break_set(UBreakIterator *iterator, const uint16_t *text, int32_t length) {
    UErrorCode error = U_ZERO_ERROR;
    ubrk_setText(iterator, reinterpret_cast<const UChar *>(text), length, &error);
    return error;
}

E2D_TEXT_API int32_t e2d_break_first(UBreakIterator *iterator) {
    return ubrk_first(iterator);
}

E2D_TEXT_API int32_t e2d_break_next(UBreakIterator *iterator) {
    e2d_icu_break_error = 0;
    auto result = ubrk_next(iterator);
    return e2d_icu_break_error > 0 ? -e2d_icu_break_error - 1 : result;
}

E2D_TEXT_API int32_t e2d_break_status(UBreakIterator *iterator) {
    return ubrk_getRuleStatus(iterator);
}

E2D_TEXT_API void e2d_break_close(UBreakIterator *iterator) {
    ubrk_close(iterator);
}
