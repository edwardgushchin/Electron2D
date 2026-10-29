#include <cstdio>
#include <cstdlib>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <limits>
#include <mutex>
#include <string>
#include <vector>

extern "C" {
int e2d_text_memory(void *(*)(const void *, size_t), void *(*)(const void *, void *, size_t), void (*)(const void *, void *));
void *e2d_regex_create(const uint32_t *, size_t, int *, size_t *);
void e2d_regex_destroy(void *);
int e2d_regex_match(const void *, const uint32_t *, size_t, size_t, size_t *, size_t);
int e2d_regex_substitute(const void *, const uint32_t *, size_t, size_t, const uint32_t *, size_t, int, int, uint32_t *, size_t *);
}
static void require(bool value, const char *message) {
    if (!value) { std::fprintf(stderr, "%s\n", message); std::abort(); }
}
struct alignas(std::max_align_t) Block { size_t size; };
static std::mutex gate;
static size_t current, peak, calls, fail_at, failures;
static void *allocate(const void *, size_t size) {
    std::lock_guard<std::mutex> lock(gate);
    if (++calls == fail_at) { ++failures; return nullptr; }
    if (size > std::numeric_limits<size_t>::max() - sizeof(Block)) return nullptr;
    auto block = static_cast<Block *>(std::malloc(sizeof(Block) + size));
    if (!block) return nullptr;
    block->size = size; current += size; if (current > peak) peak = current;
    return block + 1;
}
static void release(const void *, void *value) {
    if (!value) return;
    std::lock_guard<std::mutex> lock(gate);
    auto block = static_cast<Block *>(value) - 1;
    require(current >= block->size, "Invalid allocation ownership");
    current -= block->size; std::free(block);
}
static void *resize(const void *, void *, size_t) { std::abort(); }
static const uint32_t *data(const std::u32string &s) { return reinterpret_cast<const uint32_t *>(s.c_str()); }

int main(int argc, char **argv) {
    if (argc != 3) return 1;
    const std::string mode = argv[1];
    const uint32_t letter = 'a'; int unprepared = 0; size_t unprepared_offset = SIZE_MAX;
    require(!e2d_regex_create(&letter, 1, &unprepared, &unprepared_offset) && unprepared == -48 && unprepared_offset == 0,
        "Unregistered compilation must fail without falling back to system allocation");
    require(e2d_text_memory(allocate, resize, release) == 0, "Callback registration failed");
    const std::u32string pattern = mode == "budget" ? U"(*LIMIT_MATCH=1000)(*NO_START_OPT)(a|aa)+$" : U"(?<item>a+)";
    const std::u32string subject = mode == "nomatch" ? U"!" : mode == "budget" ? std::u32string(64, U'a') + U'!' : std::u32string(1024, U'a');
    std::vector<uint32_t> output(4096); size_t positions[8], offset = 0; int error = 0;
    void *code = nullptr;
    if (mode != "compile" && mode != "invalid") {
        code = e2d_regex_create(data(pattern), pattern.size(), &error, &offset);
        require(code && current > 0, "Compiled program bypassed the allocator");
    }
    const size_t baseline = current;
    calls = failures = 0; peak = current; fail_at = std::strtoull(argv[2], nullptr, 10);
    int result = 0; size_t length = mode == "size" ? 1 : output.size();
    if (mode == "compile" || mode == "invalid") {
        const auto input = mode == "invalid" ? std::u32string(U"(") : pattern;
        code = e2d_regex_create(data(input), input.size(), &error, &offset);
        if (failures) require(!code && (error == -48 || error == 121), "Compilation allocation failure was not reported");
        else require(mode == "invalid" ? !code && error > 0 : code != nullptr, "Compilation outcome changed");
    } else if (mode == "match" || mode == "nomatch" || mode == "budget") {
        result = e2d_regex_match(code, data(subject), subject.size(), 0, positions, 8);
        require(failures ? result == -48 : mode == "nomatch" ? result == -1 : mode == "budget" ? result == -47 : result > 0,
            "Matching allocation/work failure was hidden or misreported");
    } else {
        const std::u32string replacement = mode == "replacement" ? U"${" : U"${item}$$";
        result = e2d_regex_substitute(code, data(subject), subject.size(), 0, data(replacement), replacement.size(), 1, 5000, output.data(), &length);
        if (failures) require(result == -48, "Substitution allocation failure was hidden");
        else if (mode == "size") require(result == -48 && length > 1, "Required output sizing changed");
        else if (mode == "replacement") require(result < 0, "Invalid replacement was accepted");
        else require(mode == "substitute" && result == 1 && length == subject.size() + 1 && output[length - 1] == '$', "Substitution output changed");
    }
    const size_t phase_calls = calls;
    require(!fail_at || (calls >= fail_at && failures == 1), "Requested failure position was not exercised");
    fail_at = 0;
    if (mode != "compile" && mode != "invalid") {
        require(current == baseline, "Temporary native operation storage leaked");
        const std::u32string retry = U"aaa";
        require(e2d_regex_match(code, data(retry), retry.size(), 0, positions, 8) > 0, "Program failed to recover after native operation error");
        require(current == baseline, "Successful retry retained temporary storage");
    }
    if (code) e2d_regex_destroy(code);
    require(current == 0, "Compiled program or failed initialization leaked payload");
    std::printf("{\"Calls\":%zu,\"Peak\":%zu,\"Failures\":%zu,\"Current\":%zu}\n", phase_calls, peak, failures, current);
}
