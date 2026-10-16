#include "bridge.h"
#include "astcenc.h"
extern "C" {
#include "detex.h"
}
#include <algorithm>
#include <cstring>
#include <memory>
#include <iterator>
#include <new>

extern "C" int e2d_block_decode(uint32_t format, uint32_t width, uint32_t height,
    const uint8_t *source, uint64_t source_size, uint8_t *output, uint64_t output_size) {
    try {
        if (format > 11 || !width || !height || !source || !output ||
            static_cast<uint64_t>(width) * height > 64u * 1024u * 1024u / 16) return -1;
        bool astc = format >= 8, hdr = astc && (format & 1), signed_eac = format == 5 || format == 7;
        uint32_t block = format >= 10 ? 8 : 4, stride = signed_eac ? 16 : hdr ? 8 : 4;
        uint32_t block_bytes = astc || format == 2 || format == 6 || format == 7 ? 16 : 8;
        uint32_t columns = (width + block - 1) / block, rows = (height + block - 1) / block;
        if (source_size != static_cast<uint64_t>(columns) * rows * block_bytes ||
            output_size != static_cast<uint64_t>(width) * height * stride) return -1;
        if (hdr && (reinterpret_cast<uintptr_t>(output) & 1)) return -1;
        if (astc) {
            astcenc_config config{};
            auto status = astcenc_config_init(hdr ? ASTCENC_PRF_HDR : ASTCENC_PRF_LDR, block, block, 1,
                ASTCENC_PRE_MEDIUM, ASTCENC_FLG_DECOMPRESS_ONLY, &config);
            if (status != ASTCENC_SUCCESS) return -1;
            astcenc_context *raw = nullptr;
            status = astcenc_context_alloc(&config, 1, &raw, nullptr);
            if (status != ASTCENC_SUCCESS) return status == ASTCENC_ERR_OUT_OF_MEM ? -3 : -1;
            std::unique_ptr<astcenc_context, decltype(&astcenc_context_free)> context(raw, astcenc_context_free);
            void *planes[] = {output};
            astcenc_image image{width, height, 1, hdr ? ASTCENC_TYPE_F16 : ASTCENC_TYPE_U8, planes};
            astcenc_swizzle swizzle{ASTCENC_SWZ_R, ASTCENC_SWZ_G, ASTCENC_SWZ_B, ASTCENC_SWZ_A};
            return astcenc_decompress_image(context.get(), source, source_size, &image, &swizzle, 0) == ASTCENC_SUCCESS ? 0 : -1;
        }
        for (uint32_t by = 0; by < rows; ++by) for (uint32_t bx = 0; bx < columns; ++bx) {
            const auto *input = source + (static_cast<uint64_t>(by) * columns + bx) * block_bytes;
            uint8_t adjusted[16]{}; uint32_t rgba[16]; uint16_t channels[32]{}; float floating[64]{};
            std::fill(std::begin(rgba), std::end(rgba), 0xff000000u);
            bool success = false;
            switch (format) {
                case 0: success = detexDecompressBlockETC1(input, DETEX_MODE_MASK_ALL, 0, reinterpret_cast<uint8_t *>(rgba)); break;
                case 1: success = detexDecompressBlockETC2(input, DETEX_MODE_MASK_ALL, 0, reinterpret_cast<uint8_t *>(rgba)); break;
                case 2: success = detexDecompressBlockETC2_EAC(input, DETEX_MODE_MASK_ALL, 0, reinterpret_cast<uint8_t *>(rgba)); break;
                case 3: success = detexDecompressBlockETC2_PUNCHTHROUGH(input, DETEX_MODE_MASK_ALL, 0, reinterpret_cast<uint8_t *>(rgba)); break;
                default: {
                    std::memcpy(adjusted, input, block_bytes);
                    if (signed_eac) {
                        if (adjusted[0] == 128) adjusted[0] = 129;
                        if (format == 7 && adjusted[8] == 128) adjusted[8] = 129;
                    }
                    auto decode = format == 4 ? detexDecompressBlockEAC_R11 : format == 5 ? detexDecompressBlockEAC_SIGNED_R11
                        : format == 6 ? detexDecompressBlockEAC_RG11 : detexDecompressBlockEAC_SIGNED_RG11;
                    success = decode(adjusted, DETEX_MODE_MASK_ALL, 0, reinterpret_cast<uint8_t *>(channels));
                    uint32_t count = format >= 6 ? 2 : 1;
                    for (unsigned pixel = 0; pixel < 16; ++pixel) {
                        if (signed_eac) {
                            for (unsigned channel = 0; channel < count; ++channel) {
                                int sample = static_cast<int16_t>(channels[pixel * count + channel]);
                                floating[pixel * 4 + channel] = sample < 0 ? -((-sample >> 5) / 1023.0f) : (sample >> 5) / 1023.0f;
                            }
                            floating[pixel * 4 + 3] = 1;
                        } else for (unsigned channel = 0; channel < count; ++channel)
                            rgba[pixel] |= static_cast<uint32_t>(channels[pixel * count + channel] >> 8) << (channel * 8);
                    }
                }
            }
            if (!success) return -1;
            const auto *pixels = reinterpret_cast<const uint8_t *>(signed_eac ? static_cast<const void *>(floating) : static_cast<const void *>(rgba));
            for (uint32_t y = 0; y < std::min(4u, height - by * 4); ++y)
                std::memcpy(output + (static_cast<uint64_t>(by * 4 + y) * width + bx * 4) * stride,
                    pixels + y * 4 * stride, std::min(4u, width - bx * 4) * stride);
        }
        return 0;
    } catch (const std::bad_alloc &) { return -3; }
      catch (...) { return -1; }
}
