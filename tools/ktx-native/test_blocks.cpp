#include "bridge.h"
#include <algorithm>
#include <atomic>
#include <cstring>
#include <fstream>
#include <iterator>
#include <stdexcept>
#include <thread>
#include <vector>
#include <cstdio>

static void check(bool condition, const char *message) { if (!condition) throw std::runtime_error(message); }
static std::vector<uint8_t> read(const char *path) {
    std::ifstream file(path, std::ios::binary); check(file.good(), "Fixture could not be opened.");
    return std::vector<uint8_t>(std::istreambuf_iterator<char>(file), std::istreambuf_iterator<char>());
}

int main(int argc, char **argv) {
    try {
        check(argc == 4, "Expected format, encoded pixels and oracle pixels.");
        uint32_t format = static_cast<uint32_t>(std::stoul(argv[1]));
        auto source = read(argv[2]), expected = read(argv[3]);
        uint32_t block = format >= 10 ? 8 : 4, stride = format == 5 || format == 7 ? 16 : format == 9 || format == 11 ? 8 : 4;
        uint32_t block_bytes = format >= 8 || format == 2 || format == 6 || format == 7 ? 16 : 8;
        size_t encoded_offset = 0, decoded_offset = 0;
        for (uint32_t width = 7, height = 5;; width = std::max(1u, width / 2), height = std::max(1u, height / 2)) {
            size_t encoded_size = ((width + block - 1) / block) * ((height + block - 1) / block) * block_bytes;
            size_t decoded_size = width * height * stride;
            check(encoded_offset + encoded_size <= source.size() && decoded_offset + decoded_size <= expected.size(), "Fixture layout overflow.");
            size_t prefix = format == 9 || format == 11 ? 2 : 1;
            std::vector<uint8_t> input(encoded_size + 2, 0xa5), output(decoded_size + prefix + 1, 0x5a);
            std::copy_n(source.data() + encoded_offset, encoded_size, input.data() + 1);
            const auto original = input;
            if (prefix == 2) check(e2d_block_decode(format, width, height, input.data() + 1, encoded_size, output.data() + 1, decoded_size) != 0, "Misaligned half output accepted.");
            auto decode = [&](const uint8_t *data, uint64_t size, uint8_t *pixels, uint64_t capacity) {
                return e2d_block_decode(format, width, height, data, size, pixels, capacity);
            };
            check(decode(input.data() + 1, encoded_size, output.data() + prefix, decoded_size) == 0, "Valid unaligned decode failed.");
            check(std::memcmp(output.data() + prefix, expected.data() + decoded_offset, decoded_size) == 0, "Independent pixel oracle mismatch.");
            check(input == original && output.front() == 0x5a && output[prefix - 1] == 0x5a && output.back() == 0x5a, "Borrowed input or output guards changed.");
            for (size_t size = 0; size < encoded_size; ++size)
                check(decode(input.data() + 1, size, output.data() + prefix, decoded_size) != 0, "Truncated block layout accepted.");
            check(decode(input.data() + 1, encoded_size + 1, output.data() + prefix, decoded_size) != 0, "Trailing block bytes accepted.");
            check(decode(input.data() + 1, encoded_size, output.data() + prefix, decoded_size - 1) != 0, "Short output accepted.");
            check(decode(input.data() + 1, encoded_size, output.data() + prefix, decoded_size + 1) != 0, "Oversized output accepted.");
            check(decode(nullptr, encoded_size, output.data() + prefix, decoded_size) != 0 && decode(input.data() + 1, encoded_size, nullptr, decoded_size) != 0, "Null buffer accepted.");
            check(e2d_block_decode(format, 0, height, input.data() + 1, encoded_size, output.data() + prefix, decoded_size) != 0, "Zero extent accepted.");
            check(e2d_block_decode(format, UINT32_MAX, UINT32_MAX, input.data() + 1, encoded_size, output.data() + prefix, decoded_size) != 0, "Extent overflow accepted.");
            check(e2d_block_decode(12, width, height, input.data() + 1, encoded_size, output.data() + prefix, decoded_size) != 0, "Unknown codec accepted.");
            uint32_t random = 0x51adf963;
            for (unsigned iteration = 0; iteration < 256; ++iteration) {
                input = original;
                for (unsigned change = 0; change < 4; ++change) {
                    random = random * 1664525u + 1013904223u;
                    input[1 + random % encoded_size] ^= static_cast<uint8_t>((random >> 16) | 1);
                }
                const auto borrowed = input;
                int status = decode(input.data() + 1, encoded_size, output.data() + prefix, decoded_size);
                check(status == 0 || status == -1, "Unexpected block mutation result.");
                check(input == borrowed && output.front() == 0x5a && output[prefix - 1] == 0x5a && output.back() == 0x5a, "Mutation crossed a buffer boundary.");
            }
            std::atomic<bool> passed{true}; std::vector<std::thread> threads;
            for (unsigned worker = 0; worker < 8; ++worker) threads.emplace_back([&] {
                for (unsigned repeat = 0; repeat < 4; ++repeat) {
                    std::vector<uint8_t> pixels(decoded_size);
                    if (decode(original.data() + 1, encoded_size, pixels.data(), decoded_size) != 0 ||
                        std::memcmp(pixels.data(), expected.data() + decoded_offset, decoded_size) != 0) passed = false;
                }
            });
            for (auto &thread : threads) thread.join();
            check(passed, "Concurrent decoder owners disagree.");
            encoded_offset += encoded_size; decoded_offset += decoded_size;
            if (width == 1 && height == 1) break;
        }
        check(encoded_offset == source.size() && decoded_offset == expected.size(), "Trailing fixture data.");
        return 0;
    } catch (const std::exception &error) { std::fprintf(stderr, "%s\n", error.what()); return 1; }
}
