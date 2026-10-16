#include "bridge.h"
#include <ktx.h>
#include <cstring>
#include <new>
#include <vector>

static constexpr uint64_t limit = 64u * 1024u * 1024u;

extern "C" int e2d_basis_decode(const uint8_t *data, uint32_t size, ktxTexture2 **result);

extern "C" int e2d_basis_create(const uint8_t *data, uint64_t size, e2d_ktx_info *info, void **result) {
    if (info) *info = {};
    if (result) *result = nullptr;
    if (!info || !result || !data || size < 64 || size > limit) return -1;
    ktxTexture2 *texture = nullptr;
    try {
        auto status = e2d_basis_decode(data, static_cast<uint32_t>(size), &texture);
        if (status) return status;
        info->width = texture->baseWidth; info->height = texture->baseHeight; info->levels = texture->numLevels;
        info->flags = 8; info->version = static_cast<uint32_t>(ktxTexture2_c); info->format = texture->vkFormat; info->swizzle = 0x61626772u;
        *result = texture;
        return 0;
    } catch (...) {
        if (texture) ktxTexture2_Destroy(texture);
        return -3;
    }
}

static uint32_t word(const uint8_t *p, bool big = false) {
    uint32_t v = 0;
    for (unsigned i = 0; i < 4; ++i) v |= static_cast<uint32_t>(p[big ? 3 - i : i]) << (i * 8);
    return v;
}

static uint64_t wide(const uint8_t *p) { return word(p) | static_cast<uint64_t>(word(p + 4)) << 32; }

static bool range(uint64_t offset, uint64_t bytes, uint64_t size) { return offset <= size && bytes <= size - offset; }

static bool metadata(const uint8_t *data, uint64_t size, bool big) {
    if (size > 1024u * 1024u || size % 4) return false;
    uint64_t position = 0;
    for (unsigned entries = 0; position < size; ++entries) {
        if (entries >= 4096 || !range(position, 4, size)) return false;
        auto length = word(data + position, big); position += 4;
        if (!length || !range(position, length, size)) return false;
        auto *end = static_cast<const uint8_t *>(std::memchr(data + position, 0, length));
        if (!end || end == data + position) return false;
        position += (static_cast<uint64_t>(length) + 3u) & ~uint64_t(3);
    }
    return position == size;
}

static int preflight(const uint8_t *data, uint64_t size) {
    const uint8_t signature[12] = {0xab, 0x4b, 0x54, 0x58, 0x20, 0x31, 0x31, 0xbb, 0x0d, 0x0a, 0x1a, 0x0a};
    bool first = std::memcmp(data, signature, 12) == 0;
    uint8_t second_signature[12]; std::memcpy(second_signature, signature, 12);
    second_signature[5] = 0x32; second_signature[6] = 0x30;
    if (!first && (size < 104 || std::memcmp(data, second_signature, 12) != 0)) return -1;
    bool big = first && word(data + 12) == 0x01020304;
    if (first && !big && word(data + 12) != 0x04030201) return -1;
    auto width = word(data + (first ? 36 : 20), big), height = word(data + (first ? 40 : 24), big);
    auto depth = word(data + (first ? 44 : 28), big), layers = word(data + (first ? 48 : 32), big);
    auto faces = word(data + (first ? 52 : 36), big), levels = word(data + (first ? 56 : 40), big);
    if (depth || layers || faces != 1) return -2;
    if (!width || !height || width > (1u << 24) || height > (1u << 24)) return -1;
    if (first && !levels) levels = 1;
    uint32_t full = 1, w = width, h = height;
    while (w > 1 || h > 1) { w = w > 1 ? w / 2 : 1; h = h > 1 ? h / 2 : 1; ++full; }
    if (levels != 1 && levels != full) return -1;
    uint64_t expanded = 0; w = width; h = height;
    for (unsigned i = 0; i < levels; ++i) {
        expanded += static_cast<uint64_t>(w) * h * 16;
        w = w > 1 ? w / 2 : 1; h = h > 1 ? h / 2 : 1;
    }
    if (expanded > limit) return -1;
    if (first) {
        auto kv_size = word(data + 60, big);
        if (!range(64, kv_size, size) || !metadata(data + 64, kv_size, big)) return -1;
        uint64_t position = 64u + static_cast<uint64_t>(kv_size);
        for (unsigned i = 0; i < levels; ++i) {
            if (!range(position, 4, size)) return -1;
            auto length = word(data + position, big); position += 4;
            if (!length || !range(position, length, size)) return -1;
            position += (static_cast<uint64_t>(length) + 3u) & ~uint64_t(3);
        }
        return position == size ? 0 : -1;
    }
    uint64_t table_end = 80u + 24u * levels;
    if (!range(0, table_end, size)) return -1;
    uint64_t offsets[28] = {}, sizes[28] = {};
    offsets[0] = word(data + 48); sizes[0] = word(data + 52);
    offsets[1] = word(data + 56); sizes[1] = word(data + 60);
    offsets[2] = wide(data + 64); sizes[2] = wide(data + 72);
    if (sizes[0] < 28) return -1;
    for (unsigned i = 0; i < levels; ++i) {
        const auto *entry = data + 80 + i * 24;
        offsets[i + 3] = wide(entry); sizes[i + 3] = wide(entry + 8);
        if (!sizes[i + 3] || wide(entry + 16) > limit) return -1;
    }
    for (unsigned i = 0; i < levels + 3; ++i) {
        if (!sizes[i]) continue;
        if (offsets[i] < table_end || !range(offsets[i], sizes[i], size)) return -1;
        for (unsigned j = 0; j < i; ++j)
            if (sizes[j] && offsets[i] < offsets[j] + sizes[j] && offsets[j] < offsets[i] + sizes[i]) return -1;
    }
    if (sizes[1] && !metadata(data + offsets[1], sizes[1], false)) return -1;
    const auto *dfd = data + offsets[0];
    if (word(dfd) != sizes[0]) return -1;
    uint64_t descriptor = 4;
    while (descriptor < sizes[0]) {
        if (!range(descriptor, 8, sizes[0])) return -1;
        auto length = word(dfd + descriptor + 4) >> 16;
        if (length < 8 || !range(descriptor, length, sizes[0]) ||
            (descriptor == 4 && (length < 24 || (length - 24) % 16))) return -1;
        if (descriptor == 4 && dfd[descriptor + 8] == 1) {
            auto pixel_bits = static_cast<unsigned>(dfd[descriptor + 16]) * 8;
            for (uint64_t sample = descriptor + 24; sample < descriptor + length; sample += 16) {
                auto channel = dfd[sample + 3] & 15u;
                auto bits = static_cast<unsigned>(dfd[sample + 2]) + 1;
                auto offset = word(dfd + sample) & 0xffffu;
                if (bits > 32 || offset > pixel_bits || bits > pixel_bits - offset || (channel > 2 && channel != 15)) return -1;
            }
        }
        descriptor += length;
    }
    if (descriptor != sizes[0]) return -1;
    if (word(data + 44) == 1) {
        auto header_size = 20u + 20u * levels;
        if (sizes[2] < header_size) return -1;
        const auto *global = data + offsets[2];
        auto counts = word(global);
        if (!(counts & 0xffffu) || !(counts >> 16)) return -1;
        uint64_t total = header_size;
        for (unsigned field = 4; field < 20; field += 4) total += word(global + field);
        if (total != sizes[2]) return -1;
        for (unsigned level = 0; level < levels; ++level) {
            auto *image = global + 20 + level * 20;
            auto rgb_offset = word(image + 4), rgb_size = word(image + 8);
            auto alpha_offset = word(image + 12), alpha_size = word(image + 16);
            if (word(image) || !rgb_size || !range(rgb_offset, rgb_size, sizes[level + 3]) ||
                (alpha_size && (!range(alpha_offset, alpha_size, sizes[level + 3]) ||
                    (rgb_offset < static_cast<uint64_t>(alpha_offset) + alpha_size && alpha_offset < static_cast<uint64_t>(rgb_offset) + rgb_size)))) return -1;
        }
    }
    return 0;
}

static void destroy(ktxTexture *texture) {
    if (texture->classId == ktxTexture1_c) ktxTexture1_Destroy(reinterpret_cast<ktxTexture1 *>(texture));
    else ktxTexture2_Destroy(reinterpret_cast<ktxTexture2 *>(texture));
}

extern "C" int e2d_ktx_create(const uint8_t *data, uint64_t size, e2d_ktx_info *info, void **result) {
    if (result) *result = nullptr;
    if (info) *info = {};
    if (!result || !info) return -1;
    if (!data || size < 64 || size > limit) return -1;
    auto checked = preflight(data, size);
    if (checked) return checked;
    ktxTexture *texture = nullptr;
    try {
        uint32_t legacy = 0;
        std::vector<uint8_t> normalized;
        if (data[5] == 0x31) {
            bool big = word(data + 12) == 0x01020304;
            auto format = word(data + 28, big);
            if (format == 0x1909 || format == 0x8040 || format == 0x190a || format == 0x8045) {
                bool alpha = format == 0x190a || format == 0x8045;
                legacy = alpha ? 0x190a : 0x1909;
                if (word(data + 16, big) != 0x1401 || word(data + 20, big) != 1 || word(data + 24, big) != legacy || word(data + 32, big) != legacy) return -1;
                normalized.assign(data, data + size);
                auto put = [&](unsigned offset, uint32_t value) {
                    for (unsigned i = 0; i < 4; ++i) normalized[offset + (big ? 3 - i : i)] = static_cast<uint8_t>(value >> (i * 8));
                };
                put(24, alpha ? 0x8227 : 0x1903); put(28, alpha ? 0x822b : 0x8229); put(32, alpha ? 0x8227 : 0x1903);
                data = normalized.data();
            }
        }
        auto status = ktxTexture_CreateFromMemory(data, static_cast<size_t>(size), 0, &texture);
        if (status != KTX_SUCCESS) return static_cast<int>(status);
        if (texture->numDimensions != 2 || texture->isArray || texture->isCubemap || texture->numFaces != 1) {
            destroy(texture);
            return -2;
        }
        uint32_t levels = 1, w = texture->baseWidth, h = texture->baseHeight;
        while (w && h) {
            if (w == 1 && h == 1) break;
            w = w > 1 ? w / 2 : 1;
            h = h > 1 ? h / 2 : 1;
            ++levels;
        }
        if (!texture->baseWidth || !texture->baseHeight || texture->baseWidth > (1u << 24) || texture->baseHeight > (1u << 24) ||
            (texture->numLevels != 1 && texture->numLevels != levels) ||
            (texture->classId == ktxTexture1_c ? ktxTexture1_GetDataSizeUncompressed(reinterpret_cast<ktxTexture1 *>(texture)) :
                ktxTexture2_GetDataSizeUncompressed(reinterpret_cast<ktxTexture2 *>(texture))) > limit) {
            destroy(texture);
            return -1;
        }
        status = texture->classId == ktxTexture1_c ? ktxTexture1_LoadImageData(reinterpret_cast<ktxTexture1 *>(texture), nullptr, 0) :
            ktxTexture2_LoadImageData(reinterpret_cast<ktxTexture2 *>(texture), nullptr, 0);
        if (status != KTX_SUCCESS) { destroy(texture); return static_cast<int>(status); }
        info->width = texture->baseWidth;
        info->height = texture->baseHeight;
        info->levels = texture->numLevels;
        info->version = static_cast<uint32_t>(texture->classId);
        info->flags = (texture->orientation.x == KTX_ORIENT_X_LEFT ? 1u : 0u) |
                      (texture->orientation.y == KTX_ORIENT_Y_UP ? 2u : 0u);
        info->swizzle = 0x61626772u; // rgba, independent of native byte order.
        unsigned swizzle_size = 0;
        void *swizzle = nullptr;
        if (ktxHashList_FindValue(&texture->kvDataHead, KTX_SWIZZLE_KEY, &swizzle_size, &swizzle) == KTX_SUCCESS) {
            if (swizzle_size != 5 || static_cast<const char *>(swizzle)[4] != 0) {
                destroy(texture); return -1;
            }
            info->swizzle = 0;
            for (unsigned i = 0; i < 4; ++i) info->swizzle |= static_cast<uint32_t>(static_cast<const uint8_t *>(swizzle)[i]) << (i * 8);
        }
        if (texture->classId == ktxTexture1_c) {
            info->format = legacy ? legacy : reinterpret_cast<ktxTexture1 *>(texture)->glInternalformat;
            if (info->format == 0x8c41 || info->format == 0x8c43) info->flags |= 4;
        }
        else {
            auto *second = reinterpret_cast<ktxTexture2 *>(texture);
            if (ktxTexture2_GetTransferFunction_e(second) == KHR_DF_TRANSFER_SRGB) info->flags |= 4;
            if (ktxTexture2_IsTranscodable(second)) {
                info->flags |= 8;
                auto target = ktxTexture2_IsHDR(second) ? KTX_TTF_RGB_HALF : KTX_TTF_RGBA32;
                status = ktxTexture2_TranscodeBasis(second, target, 0);
                if (status != KTX_SUCCESS) { destroy(texture); return static_cast<int>(status); }
            }
            info->format = second->vkFormat;
        }
        if (texture->dataSize > limit) { destroy(texture); return -1; }
        *result = texture;
        return 0;
    } catch (...) {
        if (texture) destroy(texture);
        return -3;
    }
}

extern "C" int e2d_ktx_level(void *context, uint32_t level, uint8_t *output, uint64_t size, uint32_t row_bytes, uint32_t rows) {
    auto *texture = static_cast<ktxTexture *>(context);
    if (!texture || !output || level >= texture->numLevels || !row_bytes || !rows ||
        size != static_cast<uint64_t>(row_bytes) * rows || size > limit) return -1;
    ktx_size_t offset = 0;
    auto status = texture->classId == ktxTexture1_c ? ktxTexture1_GetImageOffset(reinterpret_cast<ktxTexture1 *>(texture), level, 0, 0, &offset) :
        ktxTexture2_GetImageOffset(reinterpret_cast<ktxTexture2 *>(texture), level, 0, 0, &offset);
    if (status != KTX_SUCCESS) return -1;
    auto pitch = texture->classId == ktxTexture1_c && !texture->isCompressed ? (row_bytes + 3u) & ~3u : row_bytes;
    auto image_size = texture->classId == ktxTexture1_c ? ktxTexture1_GetImageSize(reinterpret_cast<ktxTexture1 *>(texture), level) :
        ktxTexture2_GetImageSize(reinterpret_cast<ktxTexture2 *>(texture), level);
    auto required = static_cast<uint64_t>(pitch) * rows;
    if (required != image_size || offset > texture->dataSize || required > texture->dataSize - offset) return -1;
    for (uint32_t row = 0; row < rows; ++row)
        std::memcpy(output + static_cast<size_t>(row) * row_bytes, texture->pData + offset + static_cast<size_t>(row) * pitch, row_bytes);
    return 0;
}

extern "C" void e2d_ktx_destroy(void *context) {
    if (context) destroy(static_cast<ktxTexture *>(context));
}
