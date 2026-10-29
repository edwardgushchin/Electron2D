#include "meshoptimizer.h"
#include "../enet-native/memory.h"
#include <algorithm>
#include <cmath>
#include <cstdio>
#include <cstdlib>
#include <new>
#include <sstream>
#include <thread>
#include <vector>
extern "C" int e2d_mesh_memory(void *(*)(size_t), void (*)(void *));
extern "C" size_t e2d_mesh_simplify(unsigned *, const unsigned *, size_t, const float *, size_t, const float *, const float *, size_t, size_t, unsigned, float *);
extern "C" void e2d_test_arm(size_t);
extern "C" size_t e2d_test_attempts();
static void require(bool value, const char *message) { if (!value) { std::fprintf(stderr, "%s\n", message); std::exit(2); } }
static void *allocate(size_t size) { int32_t error; return e2d_memory_allocate(size, 0, 0, &error); }

static std::string pipeline(int profile) {
    unsigned cells = profile / 2 == 0 ? 6 : profile / 2 == 1 ? 8 : 16;
    bool deformable = profile % 2 != 0;
    size_t vertices = (cells + 1) * (cells + 1);
    std::vector<float> positions(vertices * 3), attributes(vertices * 6, 1);
    std::vector<unsigned> current;
    for (unsigned y = 0; y <= cells; ++y) for (unsigned x = 0; x <= cells; ++x) {
        unsigned i = y * (cells + 1) + x;
        positions[i * 3] = x * 2.f; positions[i * 3 + 1] = y * 2.f;
        attributes[i * 6] = attributes[i * 6 + 1] = 0; attributes[i * 6 + 2] = -1;
    }
    for (unsigned y = 0; y < cells; ++y) for (unsigned x = 0; x < cells; ++x) {
        unsigned a = y * (cells + 1) + x;
        for (unsigned i : {a,a+1,a+cells+2,a,a+cells+2,a+cells+1}) current.push_back(i);
    }
    float weights[6] = {1,1,1,1,1,1}, error = 0;
    bool prune = true, first = true;
    std::ostringstream levels; levels << '[';
    while (current.size() > 24) {
        unsigned target = std::max(unsigned(current.size()/3/2*3), 12u);
        std::vector<unsigned> next(current.size()); float step = 0;
        unsigned options = meshopt_SimplifySparse | meshopt_SimplifyLockBorder | (prune ? meshopt_SimplifyPrune : 0) | (deformable ? meshopt_SimplifyRegularize : 0);
        size_t size = e2d_mesh_simplify(next.data(), current.data(), current.size(), positions.data(), vertices, attributes.data(), weights, 6, target, options, &step);
        if (size == size_t(-1)) { require(std::isnan(step), "Failure did not publish its error sentinel"); throw std::bad_alloc(); }
        require(size <= next.size() && size % 3 == 0 && std::isfinite(step), "Malformed simplifier result");
        if (!size && prune) { prune = false; continue; }
        error = std::max(error * 1.5f, step);
        if (!size || size >= current.size() * .75f || error > 1.f) break;
        next.resize(size); current = next;
        if (!first) levels << ','; first = false; levels << '[';
        for (size_t i = 0; i < current.size(); ++i) { if(i) levels << ','; levels << current[i]; }
        levels << ']';
    }
    levels << ']'; return levels.str();
}

int main(int argc, char **argv) {
    require(argc == 3, "Expected profile and failed request");
    int profile = std::atoi(argv[1]); size_t fail_at = std::strtoull(argv[2], nullptr, 10);
    float error = 0;
    require(e2d_mesh_simplify(nullptr, nullptr, 0, nullptr, 0, nullptr, nullptr, 0, 0, 0, &error) == size_t(-1) && std::isnan(error), "Unprepared simplifier accepted use");
    require(e2d_mesh_memory(nullptr, e2d_memory_release) != 0, "Incomplete owner accepted");
    std::thread workers[4];
    for (auto &worker : workers) worker = std::thread([] { for (int i = 0; i < 1000; ++i) require(!e2d_mesh_memory(allocate, e2d_memory_release), "Concurrent registration failed"); });
    for (auto &worker : workers) worker.join();
    require(e2d_mesh_memory(std::malloc, std::free) != 0, "Replacement owner accepted");
    E2DMemorySnapshot before, after; e2d_memory_snapshot(&before); e2d_test_arm(fail_at);
    bool survived = true;
    try { pipeline(profile); } catch (const std::bad_alloc&) { survived = false; }
    size_t attempts = e2d_test_attempts(); e2d_memory_snapshot(&after);
    require(!after.nativeCurrent && after.allocated == after.retired, "Native simplification storage leaked");
    require(after.failures - before.failures == (fail_at && fail_at <= attempts ? 1u : 0u), "Failure was not observed");
    require(fail_at || survived, "Healthy simplification failed");
    e2d_test_arm(0); auto levels = pipeline(profile); e2d_memory_snapshot(&after);
    require(!after.nativeCurrent && after.nativePeak > 0, "Retry leaked or lost peak");
    std::printf("{\"profile\":%d,\"failAt\":%zu,\"attempts\":%zu,\"survived\":%s,\"nativePeak\":%llu,\"levels\":%s}\n", profile, fail_at, attempts, survived ? "true" : "false", (unsigned long long)after.nativePeak, levels.c_str());
}
