#pragma once
#ifdef ELECTRON2D_CPP_ALLOCATION_CHECK
#include "bridge.h"

// Only standalone static test executables replace allocation operators. Windows
// DLL imports would bypass that replacement and give a misleading pass.
#ifndef E2D_KTX_STATIC
#error The default C++ allocation check requires the static codec closure.
#endif
namespace e2d_test {
void begin();
void end(const char *operation);
struct Scope {
    const char *operation;
    explicit Scope(const char *operation) : operation(operation) { begin(); }
    ~Scope() { end(operation); }
};
inline int basis_create(const uint8_t *data, uint64_t size, e2d_ktx_info *info, void **result) {
    Scope check("basis_create"); return e2d_basis_create(data, size, info, result);
}
inline int ktx_create(const uint8_t *data, uint64_t size, e2d_ktx_info *info, void **result) {
    Scope check("ktx_create"); return e2d_ktx_create(data, size, info, result);
}
inline int ktx_level(void *context, uint32_t level, uint8_t *output, uint64_t size, uint32_t row_bytes, uint32_t rows) {
    Scope check("ktx_level"); return e2d_ktx_level(context, level, output, size, row_bytes, rows);
}
inline void ktx_destroy(void *context) { Scope check("ktx_destroy"); e2d_ktx_destroy(context); }
inline int block_decode(uint32_t format, uint32_t width, uint32_t height,
    const uint8_t *source, uint64_t source_size, uint8_t *output, uint64_t output_size) {
    Scope check("block_decode"); return e2d_block_decode(format, width, height, source, source_size, output, output_size);
}
inline int image_encode(uint32_t kind, uint32_t width, uint32_t height, uint32_t levels,
    uint32_t hdr, uint32_t normal, uint32_t quality, float rdo,
    const uint8_t *source, uint64_t source_size, uint8_t **output, uint64_t *output_size) {
    Scope check("image_encode"); return e2d_image_encode(kind, width, height, levels, hdr, normal, quality, rdo, source, source_size, output, output_size);
}
inline void image_encoded_free(uint8_t *output) { Scope check("image_encoded_free"); e2d_image_encoded_free(output); }
}
#define e2d_basis_create e2d_test::basis_create
#define e2d_ktx_create e2d_test::ktx_create
#define e2d_ktx_level e2d_test::ktx_level
#define e2d_ktx_destroy e2d_test::ktx_destroy
#define e2d_block_decode e2d_test::block_decode
#define e2d_image_encode e2d_test::image_encode
#define e2d_image_encoded_free e2d_test::image_encoded_free
#endif
