#include <ktx.h>
#include "transcoder/basisu_transcoder.h"
#include <memory>
#include <mutex>
#include <vector>

extern "C" void e2d_ktx_initialize_basis() {
    static std::once_flag initialized;
    std::call_once(initialized, basist::basisu_transcoder_init);
}

static bool align_slices(const uint8_t *source, uint32_t size, std::vector<uint8_t> &storage, const uint8_t *&data, uint32_t &length) {
    constexpr uint32_t limit = 64u * 1024u * 1024u;
    const auto &header = *reinterpret_cast<const basist::basis_file_header *>(source);
    uint32_t count = header.m_total_slices, table = header.m_slice_desc_file_ofs;
    if (table < static_cast<uint32_t>(header.m_header_size) || table > size || static_cast<uint64_t>(count) * sizeof(basist::basis_slice_desc) > size - table) return false;
    const auto *slices = reinterpret_cast<const basist::basis_slice_desc *>(source + table);
    uint64_t capacity = size;
    for (uint32_t i = 0; i < count; ++i) {
        uint32_t offset = slices[i].m_file_ofs, bytes = slices[i].m_file_size;
        if (offset > size || bytes > size - offset) return false;
        if (offset % 16) capacity = ((capacity + 15) & ~uint64_t(15)) + bytes;
    }
    if (capacity > limit) return false;
    storage.resize(static_cast<size_t>(capacity) + 15);
    auto *owned = reinterpret_cast<uint8_t *>((reinterpret_cast<uintptr_t>(storage.data()) + 15) & ~uintptr_t(15));
    memcpy(owned, source, size); length = size;
    auto *copied = reinterpret_cast<basist::basis_slice_desc *>(owned + table);
    // Legacy slice offsets need not satisfy the native block types' alignment.
    for (uint32_t i = 0; i < count; ++i) {
        uint32_t offset = slices[i].m_file_ofs, bytes = slices[i].m_file_size;
        if (offset % 16 == 0) continue;
        length = (length + 15) & ~uint32_t(15);
        memcpy(owned + length, source + offset, bytes); copied[i].m_file_ofs = length; length += bytes;
    }
    auto &updated = *reinterpret_cast<basist::basis_file_header *>(owned);
    uint32_t header_size = updated.m_header_size;
    updated.m_data_size = length - header_size;
    updated.m_data_crc16 = basist::crc16(owned + header_size, length - header_size, 0);
    updated.m_header_crc16 = basist::crc16(owned + 8, header_size - 8, 0);
    data = owned; return true;
}

extern "C" int e2d_basis_decode(const uint8_t *data, uint32_t size, ktxTexture2 **result) {
    *result = nullptr;
    basist::basisu_transcoder transcoder;
    if (size < sizeof(basist::basis_file_header) || !transcoder.validate_header(data, size)) return -1;
    const auto *header = reinterpret_cast<const basist::basis_file_header *>(data);
    if (static_cast<uint64_t>(header->m_header_size) + static_cast<uint32_t>(header->m_data_size) != size ||
        static_cast<uint32_t>(header->m_total_slices) > 50) return -1;
    if (transcoder.get_total_images(data, size) != 1 || static_cast<unsigned>(transcoder.get_texture_type(data, size)) != 0) return -2;
    basist::basisu_image_info image;
    if (!transcoder.get_image_info(data, size, image, 0)) return -1;
    auto width = image.m_orig_width, height = image.m_orig_height, levels = image.m_total_levels;
    if (!width || !height || width > (1u << 24) || height > (1u << 24)) return -1;
    uint32_t full = 1, w = width, h = height;
    while (w > 1 || h > 1) { w = w > 1 ? w / 2 : 1; h = h > 1 ? h / 2 : 1; ++full; }
    if (levels != 1 && levels != full) return -1;
    uint64_t expanded = 0; w = width; h = height;
    for (uint32_t level = 0; level < levels; ++level) {
        basist::basisu_image_level_info info;
        if (!transcoder.get_image_level_info(data, size, info, 0, level) || info.m_orig_width != w || info.m_orig_height != h) return -1;
        expanded += static_cast<uint64_t>(w) * h * 16;
        w = w > 1 ? w / 2 : 1; h = h > 1 ? h / 2 : 1;
    }
    if (expanded > 64u * 1024u * 1024u || !transcoder.validate_file_checksums(data, size, true)) return -1;
    std::vector<uint8_t> aligned;
    if (!align_slices(data, size, aligned, data, size)) return -1;
    e2d_ktx_initialize_basis();
    if (!transcoder.start_transcoding(data, size)) return -1;
    bool hdr = basist::basis_tex_format_is_hdr(transcoder.get_basis_tex_format(data, size));
    ktxTextureCreateInfo create = {};
    create.vkFormat = hdr ? 90 : 37; // RGB16_SFLOAT or RGBA8_UNORM.
    create.baseWidth = width; create.baseHeight = height; create.baseDepth = 1;
    create.numDimensions = 2; create.numLevels = levels; create.numLayers = 1; create.numFaces = 1;
    ktxTexture2 *raw = nullptr;
    auto status = ktxTexture2_Create(&create, KTX_TEXTURE_CREATE_ALLOC_STORAGE, &raw);
    if (status != KTX_SUCCESS) return static_cast<int>(status);
    std::unique_ptr<ktxTexture2, decltype(&ktxTexture2_Destroy)> texture(raw, ktxTexture2_Destroy);
    if (texture->dataSize > 64u * 1024u * 1024u) return -1;
    auto format = hdr ? basist::transcoder_texture_format::cTFRGB_HALF : basist::transcoder_texture_format::cTFRGBA32;
    w = width; h = height;
    for (uint32_t level = 0; level < levels; ++level) {
        ktx_size_t offset = 0;
        auto bytes = static_cast<uint64_t>(w) * h * (hdr ? 6 : 4);
        if (ktxTexture2_GetImageOffset(texture.get(), level, 0, 0, &offset) != KTX_SUCCESS ||
            bytes != ktxTexture2_GetImageSize(texture.get(), level) || offset > texture->dataSize || bytes > texture->dataSize - offset) return -1;
        if (!transcoder.transcode_image_level(data, size, 0, level, texture->pData + offset, w * h, format)) return -1;
        w = w > 1 ? w / 2 : 1; h = h > 1 ? h / 2 : 1;
    }
    *result = texture.release();
    return 0;
}
