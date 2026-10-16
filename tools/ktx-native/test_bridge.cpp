#include "bridge.h"
#include <algorithm>
#include <cstdio>
#include <cstdlib>
#include <fstream>
#include <iterator>
#include <stdexcept>
#include <vector>

static void check(bool value, const char *message) { if (!value) throw std::runtime_error(message); }

int main(int argc, char **argv) {
    try {
        check(argc == 3, "Expected input and output paths.");
        bool reject = std::string(argv[1]) == "--reject";
        std::ifstream input(reject ? argv[2] : argv[1], std::ios::binary);
        check(input.good(), "Could not open input.");
        std::vector<uint8_t> source((std::istreambuf_iterator<char>(input)), std::istreambuf_iterator<char>());
        auto original = source;
        void *context = nullptr;
        e2d_ktx_info info;
        auto status = e2d_ktx_create(source.data(), source.size(), &info, &context);
        if (reject) { check(status != 0 && !context, "Invalid source was published."); return 0; }
        check(status == 0 && context, "Valid input failed.");
        check(source == original, "Decoder mutated borrowed source bytes.");
        check(info.width == 7 && info.height == 5 && info.levels == 3 && info.version == 2 && (info.format == 37 || info.format == 90), "Unexpected output layout.");
        std::fill(source.begin(), source.end(), 0);
        std::ofstream output(argv[2], std::ios::binary);
        uint32_t width = info.width, height = info.height, stride = info.format == 90 ? 6 : 4;
        for (unsigned level = 0; level < info.levels; ++level) {
            std::vector<uint8_t> pixels(width * height * stride);
            check(e2d_ktx_level(context, level, pixels.data(), pixels.size(), width * stride, height) == 0, "Level copy failed after source retirement.");
            output.write(reinterpret_cast<char *>(pixels.data()), pixels.size());
            check(e2d_ktx_level(context, level, pixels.data(), pixels.size(), width * stride + 1, height) != 0, "Invalid output pitch accepted.");
            width = std::max(1u, width / 2); height = std::max(1u, height / 2);
        }
        e2d_ktx_destroy(context);
        for (size_t length = 0; length < original.size(); ++length) {
            context = nullptr;
            int status = e2d_ktx_create(original.data(), length, &info, &context);
            check(status != 0 && !context, "Truncated input was published.");
        }
        uint32_t seed = 0x91e10da5;
        for (unsigned iteration = 0; iteration < 512; ++iteration) {
            auto altered = original;
            for (unsigned change = 0; change < 4; ++change) {
                seed = seed * 1664525u + 1013904223u;
                auto offset = seed % (iteration < 256 ? std::min<size_t>(original.size(), 128) : original.size());
                altered[offset] ^= static_cast<uint8_t>((seed >> 16) | 1);
            }
            context = nullptr;
            if (const auto *repro = std::getenv("ELECTRON2D_KTX_REPRO")) {
                std::ofstream file(repro, std::ios::binary);
                file.write(reinterpret_cast<const char *>(altered.data()), altered.size());
                std::fprintf(stderr, "KTX mutation %u seed %u\n", iteration, seed);
            }
            int status = e2d_ktx_create(altered.data(), altered.size(), &info, &context);
            check((status == 0) == (context != nullptr), "Failure returned a live or missing owner.");
            e2d_ktx_destroy(context);
        }
        context = nullptr;
        check(e2d_ktx_create(nullptr, original.size(), &info, &context) != 0 && !context, "Null input accepted.");
        check(e2d_ktx_level(nullptr, 0, nullptr, 0, 0, 0) != 0, "Null level owner accepted.");
        e2d_ktx_destroy(nullptr);
        return 0;
    } catch (const std::exception &error) { std::fprintf(stderr, "%s\n", error.what()); return 1; }
}
