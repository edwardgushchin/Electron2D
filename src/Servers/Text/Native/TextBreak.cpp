#include "unicode/ubrk.h"
#include "unicode/udata.h"
#include "unicode/uversion.h"
#include "unicode/uchar.h"

#if defined(_WIN32)
#define E2D_TEXT_API extern "C" __declspec(dllexport)
#else
#define E2D_TEXT_API extern "C" __attribute__((visibility("default")))
#endif

// The caller owns this aligned buffer for the remaining process lifetime.
E2D_TEXT_API int32_t e2d_text_init(const void *data) {
    UErrorCode error = U_ZERO_ERROR;
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
    return ubrk_next(iterator);
}

E2D_TEXT_API int32_t e2d_break_status(UBreakIterator *iterator) {
    return ubrk_getRuleStatus(iterator);
}

E2D_TEXT_API void e2d_break_close(UBreakIterator *iterator) {
    ubrk_close(iterator);
}
