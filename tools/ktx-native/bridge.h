#ifndef E2D_KTX_H
#define E2D_KTX_H
#include <stdint.h>
#if defined(_WIN32) && !defined(E2D_KTX_STATIC)
#if defined(E2D_KTX_BUILD)
#define E2D_KTX_API __declspec(dllexport)
#else
#define E2D_KTX_API __declspec(dllimport)
#endif
#else
#define E2D_KTX_API __attribute__((visibility("default")))
#endif
#ifdef __cplusplus
extern "C" {
#endif
typedef struct {
    uint32_t width, height, levels, version, format, flags, swizzle, reserved;
} e2d_ktx_info;
E2D_KTX_API int e2d_basis_create(const uint8_t *data, uint64_t size, e2d_ktx_info *info, void **result);
E2D_KTX_API int e2d_ktx_create(const uint8_t *data, uint64_t size, e2d_ktx_info *info, void **result);
E2D_KTX_API int e2d_ktx_level(void *context, uint32_t level, uint8_t *output, uint64_t size, uint32_t row_bytes, uint32_t rows);
E2D_KTX_API void e2d_ktx_destroy(void *context);
/* HDR ASTC output must be aligned to a two-byte half value. Other buffers may be unaligned. */
E2D_KTX_API int e2d_block_decode(uint32_t format, uint32_t width, uint32_t height,
    const uint8_t *source, uint64_t source_size, uint8_t *output, uint64_t output_size);
/* Kind: Basis KTX2, ETC2 RGB/RGBA, EAC R/RG, ASTC 4x4/8x8. HDR input is copied float32 RGBA. */
E2D_KTX_API int e2d_image_encode(uint32_t kind, uint32_t width, uint32_t height, uint32_t levels,
    uint32_t hdr, uint32_t normal, uint32_t quality, float rdo,
    const uint8_t *source, uint64_t source_size, uint8_t **output, uint64_t *output_size);
E2D_KTX_API void e2d_image_encoded_free(uint8_t *output);
#ifdef __cplusplus
}
#endif
#endif
