#include "bridge.h"
#include "encoder/basisu_comp.h"
#include "astcenc.h"
#include <algorithm>
#include <cmath>
#include <cstdlib>
#include <cstring>
#include <memory>
#include <mutex>
#include <vector>

extern "C" void e2d_ktx_initialize_basis();

namespace {
constexpr uint64_t limit = 64u * 1024u * 1024u;
struct level { uint32_t width, height; uint64_t offset, bytes; };

bool levels_for(uint32_t width, uint32_t height, uint32_t count, uint32_t stride,
    uint64_t size, std::vector<level> &levels) {
    if (!width || !height || width > 16384 || height > 16384 || !count || count > 15) return false;
    uint64_t total = 0;
    for (uint32_t mip = 0; mip < count; ++mip) {
        if (mip && width == 1 && height == 1 && levels.back().width == 1 && levels.back().height == 1) return false;
        uint64_t bytes = static_cast<uint64_t>(width) * height * stride;
        if (bytes > limit || total > limit - bytes) return false;
        levels.push_back({width, height, total, bytes}); total += bytes;
        width = std::max(1u, width / 2); height = std::max(1u, height / 2);
    }
    return total == size && (count == 1 || (levels.back().width == 1 && levels.back().height == 1));
}

int astc(uint32_t kind, bool hdr, const uint8_t *source, const std::vector<level> &levels,
    std::vector<uint8_t> &output) {
    uint32_t block = kind == 5 ? 4 : 8;
    for (const auto &mip : levels) {
        uint64_t bytes = static_cast<uint64_t>((mip.width + block - 1) / block) * ((mip.height + block - 1) / block) * 16;
        if (bytes > limit - output.size()) return -1;
        auto offset = output.size(); output.resize(offset + static_cast<size_t>(bytes));
        astcenc_config config{};
        if (astcenc_config_init(hdr ? ASTCENC_PRF_HDR : ASTCENC_PRF_LDR, block, block, 1,
            ASTCENC_PRE_MEDIUM, 0, &config) != ASTCENC_SUCCESS) return -1;
        astcenc_context *raw = nullptr;
        auto status = astcenc_context_alloc(&config, 1, &raw, nullptr);
        if (status != ASTCENC_SUCCESS) return status == ASTCENC_ERR_OUT_OF_MEM ? -3 : -1;
        std::unique_ptr<astcenc_context, decltype(&astcenc_context_free)> context(raw, astcenc_context_free);
        std::vector<uint8_t> aligned;
        std::vector<float> floating;
        void *pixels;
        if (hdr) { floating.resize(static_cast<size_t>(mip.bytes / 4)); std::memcpy(floating.data(), source + mip.offset, static_cast<size_t>(mip.bytes)); pixels = floating.data(); }
        else { aligned.assign(source + mip.offset, source + mip.offset + mip.bytes); pixels = aligned.data(); }
        void *planes[] = {pixels};
        astcenc_image image{mip.width, mip.height, 1, hdr ? ASTCENC_TYPE_F32 : ASTCENC_TYPE_U8, planes};
        astcenc_swizzle swizzle{ASTCENC_SWZ_R, ASTCENC_SWZ_G, ASTCENC_SWZ_B, ASTCENC_SWZ_A};
        if (astcenc_compress_image(context.get(), &image, &swizzle, output.data() + offset,
            static_cast<size_t>(bytes), 0) != ASTCENC_SUCCESS) return -1;
    }
    return 0;
}

int basis(uint32_t kind, bool hdr, bool normal, uint32_t quality, float rdo,
    const uint8_t *source, const std::vector<level> &levels, std::vector<uint8_t> &output) {
    e2d_ktx_initialize_basis();
    static std::once_flag initialized;
    std::call_once(initialized, [] { if (!basisu::basisu_encoder_init(false)) throw std::bad_alloc(); });
    basisu::job_pool jobs(1);
    basisu::basis_compressor_params params;
    params.set_format_mode(hdr ? basist::basis_tex_format::cUASTC_HDR_4x4 : basist::basis_tex_format::cUASTC_LDR_4x4);
    params.m_pJob_pool = &jobs; params.m_multithreading = false; params.m_status_output = false;
    params.m_ktx2_and_basis_srgb_transfer_function = false;
    params.m_perceptual = !normal; params.m_pack_uastc_ldr_4x4_flags = quality;
    params.m_rdo_uastc_ldr_4x4 = rdo > 0;
    params.m_rdo_uastc_ldr_4x4_quality_scalar = rdo;
    params.m_rdo_uastc_ldr_4x4_multithreading = false;
    if (hdr) params.m_uastc_hdr_4x4_options.set_quality_level(quality);
    params.m_create_ktx2_file = kind == 0;
    if (hdr) {
        params.m_source_images_hdr.resize(1);
        if (levels.size() > 1) params.m_source_mipmap_images_hdr.resize(1);
    } else {
        params.m_source_images.resize(1);
        if (levels.size() > 1) params.m_source_mipmap_images.resize(1);
    }
    for (size_t i = 0; i < levels.size(); ++i) {
        const auto &mip = levels[i];
        if (hdr) {
            basisu::imagef image(mip.width, mip.height);
            for (uint32_t y = 0; y < mip.height; ++y) for (uint32_t x = 0; x < mip.width; ++x) {
                float pixel[4]; std::memcpy(pixel, source + mip.offset + (static_cast<uint64_t>(y) * mip.width + x) * 16, 16);
                image(x, y) = basisu::vec4F(pixel[0], pixel[1], pixel[2], pixel[3]);
            }
            if (i == 0) params.m_source_images_hdr[0] = std::move(image);
            else params.m_source_mipmap_images_hdr[0].push_back(std::move(image));
        } else {
            basisu::image image(source + mip.offset, mip.width, mip.height, 4);
            if (kind == 4) for (uint32_t y = 0; y < mip.height; ++y) for (uint32_t x = 0; x < mip.width; ++x) image(x, y).a = image(x, y).g;
            if (i == 0) params.m_source_images[0] = std::move(image);
            else params.m_source_mipmap_images[0].push_back(std::move(image));
        }
    }
    basisu::basis_compressor compressor;
    if (!compressor.init(params) || compressor.process() != basisu::basis_compressor::cECSuccess) return -1;
    if (kind == 0) {
        const auto &data = compressor.get_output_ktx2_file();
        if (data.empty() || data.size() > limit) return -1;
        output.assign(data.begin(), data.end()); return 0;
    }
    const auto &data = compressor.get_output_basis_file();
    basist::basisu_transcoder transcoder;
    if (!transcoder.start_transcoding(data.data(), data.size())) return -1;
    auto format = kind == 1 ? basist::transcoder_texture_format::cTFETC1_RGB
        : kind == 2 ? basist::transcoder_texture_format::cTFETC2_RGBA
        : kind == 3 ? basist::transcoder_texture_format::cTFETC2_EAC_R11 : basist::transcoder_texture_format::cTFETC2_EAC_RG11;
    uint32_t block_bytes = kind == 2 || kind == 4 ? 16 : 8;
    for (uint32_t i = 0; i < levels.size(); ++i) {
        const auto &mip = levels[i]; auto blocks = ((mip.width + 3) / 4) * ((mip.height + 3) / 4);
        uint64_t bytes = static_cast<uint64_t>(blocks) * block_bytes;
        if (bytes > limit - output.size()) return -1;
        auto offset = output.size(); output.resize(offset + static_cast<size_t>(bytes));
        if (!transcoder.transcode_image_level(data.data(), data.size(), 0, i, output.data() + offset, blocks, format)) return -1;
    }
    return 0;
}
}

extern "C" int e2d_image_encode(uint32_t kind, uint32_t width, uint32_t height, uint32_t count,
    uint32_t hdr, uint32_t normal, uint32_t quality, float rdo,
    const uint8_t *source, uint64_t source_size, uint8_t **output, uint64_t *output_size) {
    if (output) *output = nullptr;
    if (output_size) *output_size = 0;
    try {
        if (!source || !output || !output_size || kind > 6 || hdr > 1 || normal > 1 || quality > 4 ||
            !std::isfinite(rdo) || rdo < 0 || rdo > 50 || (hdr && kind >= 1 && kind <= 4)) return -1;
        std::vector<level> levels;
        if (!levels_for(width, height, count, hdr ? 16 : 4, source_size, levels)) return -1;
        if (hdr) for (uint64_t i = 0; i < source_size; i += 4) { float value; std::memcpy(&value, source + i, 4); if (!std::isfinite(value)) return -1; }
        std::vector<uint8_t> bytes;
        int status = kind >= 5 ? astc(kind, hdr != 0, source, levels, bytes)
            : basis(kind, hdr != 0, normal != 0, quality, rdo, source, levels, bytes);
        if (status != 0) return status;
        if (bytes.empty() || bytes.size() > limit) return -1;
        auto *allocation = static_cast<uint8_t *>(std::malloc(bytes.size()));
        if (!allocation) return -3;
        std::memcpy(allocation, bytes.data(), bytes.size());
        *output_size = bytes.size(); *output = allocation; return 0;
    } catch (const std::bad_alloc &) { return -3; }
    catch (...) { return -1; }
}
extern "C" void e2d_image_encoded_free(uint8_t *output) { std::free(output); }
