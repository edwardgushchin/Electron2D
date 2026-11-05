#include "encoder/basisu_comp.h"
#include "memory.h"
#include <array>
#include <cassert>
#include <cstdio>
#include <cstring>
#include <vector>
#include <sys/resource.h>

extern "C" void e2d_test_arm(size_t value);
extern "C" size_t e2d_test_attempts();
static uint64_t current() { E2DMemorySnapshot value{}; e2d_memory_snapshot(&value); return value.nativeCurrent; }

template <typename Call> size_t refusals(Call call) {
    auto baseline = current();
    auto run = [&](size_t failure) {
        e2d_test_arm(failure); bool success = false;
        try { success = call(); }
        catch (const std::bad_alloc &) { }
        auto attempts = e2d_test_attempts(); e2d_test_arm(0);
        assert(current() == baseline);
        return std::make_pair(success, attempts);
    };
    auto healthy = run(0); assert(healthy.first && healthy.second);
    for (size_t failure = 1; failure <= healthy.second; ++failure) {
        assert(!run(failure).first);
        assert(run(0).first);
    }
    return healthy.second;
}

template <typename Image, typename Fill, typename Resample> size_t resampler(Fill fill, Resample resample) {
    Image source(7, 5), destination(3, 2);
    fill(source);
    auto bytes = [](const Image &image) {
        auto first = reinterpret_cast<const uint8_t *>(image.get_ptr());
        return std::vector<uint8_t>(first, first + image.get_total_pixels() * sizeof(*image.get_ptr()));
    };
    auto original = bytes(source);
    assert(resample(source, destination));
    auto expected = bytes(destination);
    return refusals([&] {
        bool success = resample(source, destination);
        assert(bytes(source) == original);
        if (success) assert(bytes(destination) == expected);
        return success;
    });
}

int main() {
    const rlimit core{0, 0};
    assert(setrlimit(RLIMIT_CORE, &core) == 0);
    auto baseline = current();
    {
        basisu::basis_compressor_params params;
        auto &options = params.m_astc_hdr_6x6_options;
        assert(current() >= baseline + options.m_debug_image_prefix.capacity() + options.m_output_image_prefix.capacity() + 2);
    }
    assert(current() == baseline);
    auto parameters = refusals([] { basisu::basis_compressor_params params; return true; });
    auto jobs = refusals([] {
        basisu::job_pool pool(1);
        std::array<int, 64> capture{}; capture[0] = 1;
        int order = 0;
        pool.add_job([&order, capture] { order = order * 10 + capture[0]; });
        capture[0] = 2;
        pool.add_job([&order, capture] { order = order * 10 + capture[0]; });
        assert(order == 0);
        pool.wait_for_all(); assert(order == 21);
        return true;
    });
    auto ldr = resampler<basisu::image>([](auto &image) {
        for (uint32_t y = 0; y < 5; ++y) for (uint32_t x = 0; x < 7; ++x)
            image(x, y) = basisu::color_rgba(x * 17, y * 31, 128, 224);
    }, [](const auto &source, auto &destination) { return basisu::image_resample(source, destination); });
    auto hdr = resampler<basisu::imagef>([](auto &image) {
        for (uint32_t y = 0; y < 5; ++y) for (uint32_t x = 0; x < 7; ++x)
            image(x, y) = basisu::vec4F(x * .25f, y * .5f, 1, .25f);
    }, [](const auto &source, auto &destination) { return basisu::image_resample(source, destination); });
    assert(current() == baseline);
    std::printf("Basis storage: %zu parameter, %zu deferred LIFO job, %zu LDR and %zu HDR resampler refusals, full drain and exact retry passed.\n",
        parameters, jobs, ldr, hdr);
}
