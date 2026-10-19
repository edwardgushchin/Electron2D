#include "unicode/udata.h"
#include "unicode/unorm2.h"
#include "unicode/ustring.h"
#include "unicode/uchar.h"
#include "unicode/utf16.h"
#include <cassert>
#include <cstring>
#include <fstream>
#include <iostream>
#include <string>
#include <vector>

extern "C" int32_t e2d_text_case(const uint16_t *, int32_t, const char *, int32_t, uint16_t *, int32_t, int32_t *);
extern "C" int32_t e2d_text_strip_diacritics(const uint16_t *, int32_t, uint16_t *, int32_t, int32_t *);
extern "C" int32_t e2d_text_is_identifier(const uint16_t *, int32_t, int32_t *);

static void compare_strip(const UNormalizer2 *normalizer, const UChar *source, int32_t length) {
    UChar normalized[1024], expected[1024], actual[1024]; UErrorCode status = U_ZERO_ERROR;
    auto size = unorm2_normalize(normalizer, source, length, normalized, 1024, &status);
    assert(U_SUCCESS(status)); int32_t count = 0;
    for (int32_t at = 0; at < size;) {
        UChar32 value; U16_NEXT(normalized, at, size, value);
        if (u_getCombiningClass(value) == 0) U16_APPEND_UNSAFE(expected, count, value);
    }
    int32_t error = 0;
    auto written = e2d_text_strip_diacritics(reinterpret_cast<const uint16_t *>(source), length,
            reinterpret_cast<uint16_t *>(actual), 1024, &error);
    assert(error <= 0 && written == count && std::memcmp(expected, actual, count * sizeof(UChar)) == 0);
    auto required = e2d_text_strip_diacritics(reinterpret_cast<const uint16_t *>(source), length, nullptr, 0, &error);
    assert(required == count && (error == U_BUFFER_OVERFLOW_ERROR || (count == 0 && error <= 0)));
}

int main(int argc, char **argv) {
    assert(argc >= 2);
    std::ifstream file(argv[1], std::ios::binary);
    std::string data((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
    assert(!data.empty());
    UErrorCode status = U_ZERO_ERROR; udata_setCommonData(data.data(), &status); assert(U_SUCCESS(status));
    if (argc == 3 && std::string(argv[2]) == "missing-layout") {
        int32_t error = 0;
        auto value = e2d_text_is_identifier(reinterpret_cast<const uint16_t *>(u"abc"), 3, &error);
        assert(value == 0 && error == U_MISSING_RESOURCE_ERROR);
        std::cout << "Missing layout data fails explicitly instead of accepting incomplete identifier checks.\n"; return 0;
    }
    assert(u_getIntPropertyMaxValue(UCHAR_INDIC_SYLLABIC_CATEGORY) >= U_INSC_VOWEL_DEPENDENT);
    auto normalizer = unorm2_getNFKDInstance(&status); assert(U_SUCCESS(status));
    int checked = 0;
    for (UChar32 scalar = 0; scalar <= 0x10ffff; scalar++) {
        if (scalar >= 0xd800 && scalar <= 0xdfff) continue;
        UChar source[2]; int32_t length = 0; U16_APPEND_UNSAFE(source, length, scalar);
        compare_strip(normalizer, source, length); checked++;
    }
    const UChar32 pool[] = {0, 'A', 'e', 0x301, 0x327, 0x307, 0x34f, 0xfdfa, 0xfb03, 0xac01, 0x1d400, 0xd800};
    uint32_t random = 0x73f19u;
    for (int sample = 0; sample < 10000; sample++) {
        UChar source[64]; int32_t length = 0;
        for (int i = 0; i < 16; i++) {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            U16_APPEND_UNSAFE(source, length, pool[random % 12]);
        }
        compare_strip(normalizer, source, length);
    }
    std::vector<std::u16string> samples = {u"Iİ iı", u"ΟΣ ΣΟΣ", u"HELLO WORLD ijSSEL", u"Straße ﬃ",
        u"I\u0301 i\u0307\u0301", u"և հայ", std::u16string{u'A', 0, 0xd800, u'B'}};
    int casing = 0;
    for (auto locale : {"en", "tr", "az", "lt", "el", "hy", "nl", "ja"}) {
        for (const auto &sample : samples) for (int title = 0; title < 2; title++) {
            UChar expected[256], actual[256]; status = U_ZERO_ERROR;
            auto source = reinterpret_cast<const UChar *>(sample.data()); auto length = static_cast<int32_t>(sample.size());
            auto size = title ? u_strToTitle(expected, 256, source, length, nullptr, locale, &status)
                              : u_strToLower(expected, 256, source, length, locale, &status);
            assert(U_SUCCESS(status)); int32_t error = 0;
            auto count = e2d_text_case(reinterpret_cast<const uint16_t *>(sample.data()), length, locale, title,
                    reinterpret_cast<uint16_t *>(actual), 256, &error);
            assert(error <= 0 && count == size && std::memcmp(expected, actual, size * sizeof(UChar)) == 0); casing++;
        }
    }
    int32_t error = 0; uint16_t marker = 0xabcd;
    assert(e2d_text_case(nullptr, 1, "en", 0, &marker, 1, &error) == 0 && error == U_ILLEGAL_ARGUMENT_ERROR && marker == 0xabcd);
    assert(e2d_text_strip_diacritics(reinterpret_cast<const uint16_t *>(u"x"), 1, nullptr, 1, &error) == 0 && error == U_ILLEGAL_ARGUMENT_ERROR);
    std::cout << checked << " scalar decompositions, 10000 mixed normalization sequences, " << casing
              << " locale casing cases and ABI failure boundaries passed against direct ICU operations.\n";
}
