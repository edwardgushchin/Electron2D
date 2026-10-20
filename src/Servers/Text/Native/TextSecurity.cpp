#include "unicode/uspoof.h"
#include "unicode/uset.h"

#if defined(_WIN32)
#define E2D_TEXT_API extern "C" __declspec(dllexport)
#else
#define E2D_TEXT_API extern "C" __attribute__((visibility("default")))
#endif

E2D_TEXT_API USpoofChecker *e2d_spoof_open(int32_t *error) {
    UErrorCode status = U_ZERO_ERROR;
    auto checker = uspoof_open(&status);
    auto allowed = uset_openEmpty();
    if (allowed == nullptr && U_SUCCESS(status)) status = U_MEMORY_ALLOCATION_ERROR;
    if (U_SUCCESS(status)) {
        auto recommended = uspoof_getRecommendedSet(&status);
        auto inclusion = uspoof_getInclusionSet(&status);
        if (U_SUCCESS(status)) {
            uset_addAll(allowed, recommended);
            uset_addAll(allowed, inclusion);
            uspoof_setAllowedChars(checker, allowed, &status);
            uspoof_setRestrictionLevel(checker, USPOOF_MODERATELY_RESTRICTIVE);
        }
    }
    uset_close(allowed);
    if (U_FAILURE(status)) {
        uspoof_close(checker);
        checker = nullptr;
    }
    *error = status;
    return checker;
}

E2D_TEXT_API void e2d_spoof_close(USpoofChecker *checker) { uspoof_close(checker); }

E2D_TEXT_API int32_t e2d_spoof_skeleton(const USpoofChecker *checker,
        const uint16_t *text, int32_t length, uint16_t *output, int32_t capacity, int32_t *error) {
    UErrorCode status = U_ZERO_ERROR;
    auto result = uspoof_getSkeleton(checker, 0, reinterpret_cast<const UChar *>(text), length,
            reinterpret_cast<UChar *>(output), capacity, &status);
    *error = status;
    return result;
}

E2D_TEXT_API int32_t e2d_spoof_check(const USpoofChecker *checker,
        const uint16_t *text, int32_t length, int32_t *error) {
    UErrorCode status = U_ZERO_ERROR;
    auto result = uspoof_check(checker, reinterpret_cast<const UChar *>(text), length, nullptr, &status);
    *error = status;
    return result;
}
