#include "bridge.h"
#include <algorithm>
#include <atomic>
#include <cstdio>
#include <cstdlib>
#include <fstream>
#include <iterator>
#include <stdexcept>
#include <thread>
#include <vector>

static void check(bool value, const char *message) { if (!value) throw std::runtime_error(message); }

static uint16_t crc16(const uint8_t *bytes, size_t size) {
    uint16_t crc = 0xffff;
    for (size_t i = 0; i < size; ++i) {
        uint16_t q = bytes[i] ^ (crc >> 8), k = (q >> 4) ^ q;
        crc = (((crc << 8) ^ k) ^ (k << 5)) ^ (k << 12);
    }
    return static_cast<uint16_t>(~crc);
}

static void repair_crc(std::vector<uint8_t> &bytes) {
    const size_t header = bytes[4] | (bytes[5] << 8);
    check(header >= 14 && header < bytes.size(), "Invalid mutation header.");
    auto data = crc16(bytes.data() + header, bytes.size() - header);
    bytes[12] = data & 255; bytes[13] = data >> 8;
    auto head = crc16(bytes.data() + 8, header - 8);
    bytes[6] = head & 255; bytes[7] = head >> 8;
}

static void mixed(const char *ktx, const char *basis) {
    std::vector<uint8_t> sources[2];
    const char *paths[] = {ktx, basis};
    for (unsigned i = 0; i < 2; ++i) {
        std::ifstream input(paths[i], std::ios::binary); check(input.good(), "Could not open mixed input.");
        sources[i] = std::vector<uint8_t>((std::istreambuf_iterator<char>(input)), std::istreambuf_iterator<char>());
    }
    std::atomic<unsigned> ready{0}; std::atomic<bool> start{false}, passed{true};
    std::vector<std::thread> threads;
    for (unsigned i = 0; i < 8; ++i) threads.emplace_back([&, i] {
        ++ready; while (!start.load()) std::this_thread::yield();
        for (unsigned repeat = 0; repeat < 16; ++repeat) {
            unsigned kind = (i + repeat) % 2; const auto &source = sources[kind];
            auto create = kind ? e2d_basis_create : e2d_ktx_create;
            e2d_ktx_info info{}; void *owner = nullptr;
            if (create(source.data(), source.size(), &info, &owner) != 0 || !owner || info.width != 7 || info.height != 5 || info.levels != 3 || (info.flags & 8) == 0) passed = false;
            uint8_t pixels[7 * 5 * 4]{};
            if (owner && e2d_ktx_level(owner, 0, pixels, sizeof(pixels), 7 * 4, 5) != 0) passed = false;
            e2d_ktx_destroy(owner);
        }
    });
    while (ready.load() != 8) std::this_thread::yield(); start = true;
    for (auto &thread : threads) thread.join();
    check(passed, "Mixed concurrent first-use decoding failed.");
}

int main(int argc, char **argv) {
    try {
        if (argc == 4 && std::string(argv[1]) == "--mixed") { mixed(argv[2], argv[3]); return 0; }
        bool basis = argc == 4 && std::string(argv[1]) == "--basis";
        if (basis) { --argc; ++argv; }
        check(argc == 3, "Expected input and output paths.");
        auto create = basis ? e2d_basis_create : e2d_ktx_create;
        bool reject = std::string(argv[1]) == "--reject";
        std::ifstream input(reject ? argv[2] : argv[1], std::ios::binary);
        check(input.good(), "Could not open input.");
        std::vector<uint8_t> source((std::istreambuf_iterator<char>(input)), std::istreambuf_iterator<char>());
        auto original = source;
        if (basis) { auto repaired = original; repair_crc(repaired); check(repaired == original, "Independent Basis checksum recipe disagrees."); }
        void *context = nullptr;
        e2d_ktx_info info;
        auto status = create(source.data(), source.size(), &info, &context);
        if (reject) { check(status != 0 && !context, "Invalid source was published."); return 0; }
        check(status == 0 && context, "Valid input failed.");
        check(source == original, "Decoder mutated borrowed source bytes.");
        if (basis) {
            auto unaligned = std::vector<uint8_t>(original.size() + 1);
            std::copy(original.begin(), original.end(), unaligned.begin() + 1);
            void *copy = nullptr; e2d_ktx_info copied{};
            check(create(unaligned.data() + 1, original.size(), &copied, &copy) == 0 && copy, "Unaligned borrowed input failed.");
            e2d_ktx_destroy(copy);
        }
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
            int status = create(original.data(), length, &info, &context);
            check(status != 0 && !context, "Truncated input was published.");
        }
        uint32_t seed = 0x91e10da5;
        for (unsigned iteration = 0; iteration < 512; ++iteration) {
            auto altered = original;
            for (unsigned change = 0; change < 4; ++change) {
                seed = seed * 1664525u + 1013904223u;
                auto header = static_cast<size_t>(original[4] | (original[5] << 8));
                auto offset = basis && iteration >= 256 ? header + seed % (original.size() - header)
                    : seed % (iteration < 256 ? std::min<size_t>(original.size(), 128) : original.size());
                altered[offset] ^= static_cast<uint8_t>((seed >> 16) | 1);
            }
            if (basis && iteration >= 256) repair_crc(altered);
            context = nullptr;
            if (const auto *repro = std::getenv("ELECTRON2D_KTX_REPRO")) {
                std::ofstream file(repro, std::ios::binary);
                file.write(reinterpret_cast<const char *>(altered.data()), altered.size());
                std::fprintf(stderr, "KTX mutation %u seed %u\n", iteration, seed);
            }
            int status = create(altered.data(), altered.size(), &info, &context);
            check((status == 0) == (context != nullptr), "Failure returned a live or missing owner.");
            e2d_ktx_destroy(context);
        }
        context = nullptr;
        check(create(nullptr, original.size(), &info, &context) != 0 && !context, "Null input accepted.");
        check(e2d_ktx_level(nullptr, 0, nullptr, 0, 0, 0) != 0, "Null level owner accepted.");
        e2d_ktx_destroy(nullptr);
        return 0;
    } catch (const std::exception &error) { std::fprintf(stderr, "%s\n", error.what()); return 1; }
}
