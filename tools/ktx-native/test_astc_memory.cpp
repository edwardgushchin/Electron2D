#include "astcenc.h"
#include "memory.h"
#include <array>
#include <cassert>
#include <cstdio>
#include <memory>
#include <new>

extern "C" void e2d_test_arm(size_t value);
extern "C" size_t e2d_test_attempts();
static uint64_t current() { E2DMemorySnapshot value{}; e2d_memory_snapshot(&value); return value.nativeCurrent; }

int main() {
    std::array<float, 8 * 8 * 4> pixels{};
    for (size_t i = 0; i < pixels.size(); i += 4) {
        pixels[i] = .25f; pixels[i + 1] = .5f; pixels[i + 2] = 1; pixels[i + 3] = .25f;
    }
    auto original = pixels;
    size_t total = 0;
    for (unsigned block : {4u, 8u}) for (bool hdr : {false, true}) {
        astcenc_config config{};
        assert(astcenc_config_init(hdr ? ASTCENC_PRF_HDR : ASTCENC_PRF_LDR, block, block, 1,
            ASTCENC_PRE_MEDIUM, ASTCENC_FLG_USE_ALPHA_WEIGHT, &config) == ASTCENC_SUCCESS);
        config.a_scale_radius = 1;
        void *planes[] = {pixels.data()};
        astcenc_image image{8, 8, 1, ASTCENC_TYPE_F32, planes};
        astcenc_swizzle swizzle{ASTCENC_SWZ_R, ASTCENC_SWZ_G, ASTCENC_SWZ_B, ASTCENC_SWZ_A};
        auto baseline = current();
        std::array<uint8_t, 64> output{};
        auto call = [&](size_t failure) {
            output.fill(0x5a); bool rejected = false;
            e2d_test_arm(failure);
            try {
                astcenc_context *raw = nullptr;
                auto status = astcenc_context_alloc(&config, 1, &raw, nullptr);
                std::unique_ptr<astcenc_context, decltype(&astcenc_context_free)> owner(raw, astcenc_context_free);
                if (status != ASTCENC_SUCCESS) { assert(status == ASTCENC_ERR_OUT_OF_MEM); rejected = true; }
                else assert(astcenc_compress_image(owner.get(), &image, &swizzle, output.data(),
                    block == 4 ? 64 : 16, 0) == ASTCENC_SUCCESS);
            } catch (const std::bad_alloc &) { rejected = true; }
            auto attempts = e2d_test_attempts(); e2d_test_arm(0);
            assert(current() == baseline && pixels == original);
            if (rejected) for (auto value : output) assert(value == 0x5a);
            return std::make_pair(rejected, attempts);
        };
        auto healthy = call(0); assert(!healthy.first && healthy.second >= 8);
        auto expected = output;
        for (size_t failure = 1; failure <= healthy.second; ++failure) {
            assert(call(failure).first);
            assert(!call(0).first && output == expected);
            ++total;
        }
    }
    std::printf("ASTC alpha preprocessing: LDR/HDR 4x4/8x8, %zu allocation refusals, full context/array drain and byte-identical retry passed.\n", total);
}
