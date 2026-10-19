#include "unicode/uchar.h"
#include "unicode/unorm2.h"
#include "unicode/uscript.h"
#include "unicode/ustring.h"
#include "unicode/utf16.h"
#include <climits>

#if defined(_WIN32)
#define E2D_TEXT_API extern "C" __declspec(dllexport)
#else
#define E2D_TEXT_API extern "C" __attribute__((visibility("default")))
#endif

static bool valid_buffers(const uint16_t *text, int32_t length, uint16_t *output, int32_t capacity) {
    return length >= 0 && capacity >= 0 && (text || length == 0) && (output || capacity == 0);
}

E2D_TEXT_API int32_t e2d_text_case(const uint16_t *text, int32_t length, const char *locale,
        int32_t title, uint16_t *output, int32_t capacity, int32_t *error) {
    UErrorCode status = U_ZERO_ERROR;
    if (!valid_buffers(text, length, output, capacity) || !locale || (title != 0 && title != 1)) {
        *error = U_ILLEGAL_ARGUMENT_ERROR; return 0;
    }
    auto source = reinterpret_cast<const UChar *>(text);
    auto destination = reinterpret_cast<UChar *>(output);
    int32_t count = title ? u_strToTitle(destination, capacity, source, length, nullptr, locale, &status)
                          : u_strToLower(destination, capacity, source, length, locale, &status);
    *error = status; return count;
}

E2D_TEXT_API int32_t e2d_text_strip_diacritics(const uint16_t *text, int32_t length,
        uint16_t *output, int32_t capacity, int32_t *error) {
    UErrorCode status = U_ZERO_ERROR;
    if (!valid_buffers(text, length, output, capacity)) { *error = U_ILLEGAL_ARGUMENT_ERROR; return 0; }
    auto normalizer = unorm2_getNFKDInstance(&status);
    if (U_FAILURE(status)) { *error = status; return 0; }
    auto source = reinterpret_cast<const UChar *>(text);
    int32_t count = 0;
    for (int32_t at = 0; at < length;) {
        UChar32 scalar; U16_NEXT(source, at, length, scalar);
        UChar decomposition[64];
        int32_t size = unorm2_getDecomposition(normalizer, scalar, decomposition, 64, &status);
        if (U_FAILURE(status)) { *error = status == U_BUFFER_OVERFLOW_ERROR ? U_INTERNAL_PROGRAM_ERROR : status; return 0; }
        if (size < 0) { size = 0; U16_APPEND_UNSAFE(decomposition, size, scalar); }
        // Removing every nonzero combining class makes cross-scalar reordering irrelevant.
        for (int32_t part = 0; part < size;) {
            UChar32 value; U16_NEXT(decomposition, part, size, value);
            if (u_getCombiningClass(value) != 0) continue;
            int32_t units = U16_LENGTH(value);
            if (count > INT32_MAX - units) { *error = U_INDEX_OUTOFBOUNDS_ERROR; return 0; }
            if (count + units <= capacity) { int32_t write = count; U16_APPEND_UNSAFE(output, write, value); }
            count += units;
        }
    }
    *error = count > capacity ? U_BUFFER_OVERFLOW_ERROR : U_ZERO_ERROR; return count;
}

E2D_TEXT_API uint32_t e2d_text_character_properties(uint32_t scalar) {
    if (scalar > 0x10ffff) return 0;
    return (u_isalpha(scalar) ? 1u : 0u) | (u_ispunct(scalar) ? 2u : 0u);
}

static bool single_script(const UChar *text, int32_t start, int32_t end) {
    UScriptCode selected = USCRIPT_COMMON;
    for (int32_t at = start; at < end;) {
        UChar32 scalar; U16_NEXT(text, at, end, scalar);
        UErrorCode error = U_ZERO_ERROR;
        auto script = uscript_getScript(scalar, &error);
        if (U_FAILURE(error)) return false;
        if (script <= USCRIPT_INHERITED) continue;
        if (selected > USCRIPT_INHERITED && selected != script) return false;
        selected = script;
    }
    return true;
}

static int32_t conjunct_start(const UChar *text, int32_t before) {
    bool virama = false;
    while (before > 0) {
        UChar32 scalar; U16_PREV(text, 0, before, scalar);
        if (u_getCombiningClass(scalar) == 9) { virama = true; break; }
        if (u_charType(scalar) != U_NON_SPACING_MARK || u_getCombiningClass(scalar) == 0) return -1;
    }
    if (!virama) return -1;
    while (before > 0) {
        UChar32 scalar; U16_PREV(text, 0, before, scalar);
        if (u_charType(scalar) == U_NON_SPACING_MARK) continue;
        return u_isalpha(scalar) ? before : -1;
    }
    return -1;
}

static bool valid_joiner(const UChar *text, int32_t length, int32_t before, int32_t after, UChar32 joiner) {
    if (joiner == 0x200c) {
        int32_t left = before, right = after; UChar32 a = 0, b = 0;
        while (left > 0) { U16_PREV(text, 0, left, a); if (u_getIntPropertyValue(a, UCHAR_JOINING_TYPE) != U_JT_TRANSPARENT) break; }
        while (right < length) { U16_NEXT(text, right, length, b); if (u_getIntPropertyValue(b, UCHAR_JOINING_TYPE) != U_JT_TRANSPARENT) break; }
        auto aj = u_getIntPropertyValue(a, UCHAR_JOINING_TYPE), bj = u_getIntPropertyValue(b, UCHAR_JOINING_TYPE);
        if ((aj == U_JT_LEFT_JOINING || aj == U_JT_DUAL_JOINING) &&
                (bj == U_JT_RIGHT_JOINING || bj == U_JT_DUAL_JOINING) && single_script(text, left, right)) return true;
    }
    auto start = conjunct_start(text, before);
    if (start < 0) return false;
    int32_t right = after;
    if (joiner == 0x200c) {
        while (right < length) {
            UChar32 scalar; U16_NEXT(text, right, length, scalar);
            if (u_charType(scalar) == U_NON_SPACING_MARK && u_getCombiningClass(scalar) != 0) continue;
            return u_isalpha(scalar) && single_script(text, start, right);
        }
        return false;
    }
    if (right < length) {
        UChar32 scalar; U16_NEXT(text, right, length, scalar);
        if (u_getIntPropertyValue(scalar, UCHAR_INDIC_SYLLABIC_CATEGORY) == U_INSC_VOWEL_DEPENDENT) return false;
    }
    return single_script(text, start, right);
}

E2D_TEXT_API int32_t e2d_text_is_identifier(const uint16_t *text, int32_t length, int32_t *error) {
    UErrorCode status = U_ZERO_ERROR; *error = U_ZERO_ERROR;
    if (length < 0 || (!text && length != 0)) { *error = U_ILLEGAL_ARGUMENT_ERROR; return 0; }
    if (length == 0) return 0;
    if (u_getIntPropertyMaxValue(UCHAR_INDIC_SYLLABIC_CATEGORY) < U_INSC_VOWEL_DEPENDENT) { *error = U_MISSING_RESOURCE_ERROR; return 0; }
    auto source = reinterpret_cast<const UChar *>(text);
    auto normalizer = unorm2_getNFCInstance(&status);
    if (U_FAILURE(status)) { *error = status; return 0; }
    bool normalized = unorm2_isNormalized(normalizer, source, length, &status);
    if (U_FAILURE(status)) { *error = status; return 0; }
    if (!normalized) return 0;
    for (int32_t at = 0; at < length;) {
        int32_t before = at; UChar32 scalar; U16_NEXT(source, at, length, scalar);
        if (!U_IS_UNICODE_CHAR(scalar)) return 0;
        auto script = uscript_getScript(scalar, &status);
        if (U_FAILURE(status)) { *error = status; return 0; }
        if (uscript_getUsage(script) != USCRIPT_USAGE_RECOMMENDED) return 0;
        if (before != 0 && (scalar == 0x200c || scalar == 0x200d)) {
            if (!valid_joiner(source, length, before, at, scalar)) return 0;
        } else if (!(before == 0 ? scalar == '_' || u_hasBinaryProperty(scalar, UCHAR_XID_START)
                                 : u_hasBinaryProperty(scalar, UCHAR_XID_CONTINUE))) return 0;
    }
    return 1;
}
