#include "bridge.h"
#include <array>
#include <cassert>
#include <cmath>
#include <cstring>
#include <iostream>
#include <thread>
#include <vector>

int main() {
    std::array<uint8_t, 4 * 4 * 4 + 2 * 2 * 4 + 4> rgba;
    for (size_t i = 0; i < rgba.size(); i += 4) { rgba[i] = 224; rgba[i + 1] = 64; rgba[i + 2] = 32; rgba[i + 3] = 128; }
    for (uint32_t kind = 0; kind <= 6; ++kind) {
        uint8_t *output = nullptr; uint64_t bytes = 0;
        assert(e2d_image_encode(kind, 4, 4, 3, 0, 0, 1, 0, rgba.data(), rgba.size(), &output, &bytes) == 0);
        assert(output && bytes > 0);
        if (kind == 0) {
            e2d_ktx_info info{}; void *context = nullptr;
            assert(e2d_ktx_create(output, bytes, &info, &context) == 0 && info.width == 4 && info.height == 4 && info.levels == 3 && (info.flags & 4) == 0);
            e2d_ktx_destroy(context);
        } else {
            std::array<uint8_t, 64> decoded{};
            uint32_t format = kind == 1 ? 1 : kind == 2 ? 2 : kind == 3 ? 4 : kind == 4 ? 6 : kind == 5 ? 8 : 10;
            uint32_t block_bytes = kind == 1 || kind == 3 ? 8 : 16;
            assert(bytes == block_bytes * 3);
            assert(e2d_block_decode(format, 4, 4, output, block_bytes, decoded.data(), decoded.size()) == 0);
            assert(std::abs(static_cast<int>(decoded[0]) - 224) < 30);
            if (kind == 4) assert(std::abs(static_cast<int>(decoded[1]) - 64) < 30);
        }
        e2d_image_encoded_free(output);
    }
    std::array<float, 64> hdr;
    for (size_t i = 0; i < hdr.size(); i += 4) { hdr[i] = 2; hdr[i + 1] = .5f; hdr[i + 2] = .25f; hdr[i + 3] = 1; }
    for (uint32_t kind : {0u, 5u, 6u}) {
        uint8_t *output = nullptr; uint64_t bytes = 0;
        assert(e2d_image_encode(kind, 4, 4, 1, 1, 1, 1, 0, reinterpret_cast<uint8_t *>(hdr.data()), sizeof(hdr), &output, &bytes) == 0);
        assert(output && bytes); e2d_image_encoded_free(output);
    }
    uint8_t *output = reinterpret_cast<uint8_t *>(1); uint64_t bytes = 123;
    assert(e2d_image_encode(0, 4, 4, 3, 0, 0, 1, 0, rgba.data(), rgba.size() - 1, &output, &bytes) != 0 && !output && !bytes);
    assert(e2d_image_encode(7, 4, 4, 3, 0, 0, 1, 0, rgba.data(), rgba.size(), &output, &bytes) != 0);
    assert(e2d_image_encode(0, 4, 4, 3, 0, 0, 5, 0, rgba.data(), rgba.size(), &output, &bytes) != 0);
    assert(e2d_image_encode(0, 4, 4, 3, 0, 0, 1, NAN, rgba.data(), rgba.size(), &output, &bytes) != 0);
    for (uint32_t dimension : {0u, 16385u, UINT32_MAX})
        assert(e2d_image_encode(0, dimension, 4, 1, 0, 0, 1, 0, rgba.data(), rgba.size(), &output, &bytes) != 0);
    assert(e2d_image_encode(0, 4, 4, 2, 0, 0, 1, 0, rgba.data(), 80, &output, &bytes) != 0);
    assert(e2d_image_encode(0, 1, 1, 2, 0, 0, 1, 0, rgba.data(), 8, &output, &bytes) != 0);
    hdr[0] = INFINITY;
    assert(e2d_image_encode(5, 4, 4, 1, 1, 0, 1, 0, reinterpret_cast<uint8_t *>(hdr.data()), sizeof(hdr), &output, &bytes) != 0);
    std::array<std::thread, 4> workers;
    for (auto &worker : workers) worker = std::thread([&rgba] {
        uint8_t *encoded = nullptr; uint64_t size = 0;
        assert(e2d_image_encode(0, 4, 4, 3, 0, 1, 0, 1, rgba.data(), rgba.size(), &encoded, &size) == 0);
        e2d_ktx_info info{}; void *context = nullptr;
        assert(e2d_ktx_create(encoded, size, &info, &context) == 0 && info.levels == 3 && (info.flags & 4) == 0);
        e2d_ktx_destroy(context); e2d_image_encoded_free(encoded);
    });
    for (auto &worker : workers) worker.join();
    e2d_image_encoded_free(nullptr);
    std::cout << "Image encoder bounds, mip chains, ETC/EAC, ASTC and Basis LDR/HDR passed\n";
}
