// Controlled native storage for exercising the runtime's actual frozen-segment API.
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <sys/mman.h>
#include <unistd.h>
extern "C" void *RhRegisterFrozenSegment(void*, size_t, size_t, size_t);
extern "C" void RhUpdateFrozenSegment(void*, uint8_t*, uint8_t*);
extern "C" void RhUnregisterFrozenSegment(void*);
struct Fixture { uint8_t *memory; size_t bytes, committed; void *segment; uint32_t capacity, count; };
[[noreturn]] static void fail(const char *message) {
    std::fprintf(stderr, "aot-frozen-fixture: %s\n", message); _exit(132);
}
static size_t used(uint32_t count) { return 8 + static_cast<size_t>(count) * 24; }
extern "C" void *e2d_frozen_create(uintptr_t type, uint32_t capacity, uint32_t count) {
    if (!type || !capacity || capacity > 100000 || count > capacity ||
        *reinterpret_cast<uint32_t*>(type + 4) != 24) fail("invalid frozen layout");
    auto page = static_cast<size_t>(sysconf(_SC_PAGESIZE));
    if (!page || (page & (page - 1))) fail("invalid page size");
    auto bytes = (used(capacity) + page - 1) & ~(page - 1);
    if (bytes < 2 * page) bytes = 2 * page;
    auto memory = static_cast<uint8_t*>(mmap(nullptr, bytes, PROT_READ | PROT_WRITE,
        MAP_PRIVATE | MAP_ANONYMOUS, -1, 0));
    if (memory == MAP_FAILED) return nullptr;
    auto fixture = static_cast<Fixture*>(std::malloc(sizeof(Fixture)));
    if (!fixture) { munmap(memory, bytes); return nullptr; }
    for (uint32_t i = 0; i < capacity; ++i) {
        auto object = memory + 8 + static_cast<size_t>(i) * 24;
        *reinterpret_cast<uintptr_t*>(object) = type;
        *reinterpret_cast<int64_t*>(object + 8) = 0x12345678LL + i;
    }
    auto segment = RhRegisterFrozenSegment(memory, used(count), bytes, bytes);
    if (!segment) { std::free(fixture); munmap(memory, bytes); return nullptr; }
    *fixture = Fixture{memory, bytes, bytes, segment, capacity, count};
    return fixture;
}
extern "C" void e2d_frozen_resize(Fixture *fixture, uint32_t count) {
    if (!fixture || count > fixture->capacity) fail("invalid frozen resize");
    if (used(count) > fixture->committed) fixture->committed = fixture->bytes;
    RhUpdateFrozenSegment(fixture->segment, fixture->memory + used(count), fixture->memory + fixture->committed);
    fixture->count = count;
}
extern "C" void e2d_frozen_commit(Fixture *fixture, uint32_t pages) {
    if (!fixture) fail("invalid frozen commit");
    auto committed = static_cast<size_t>(pages) * static_cast<size_t>(sysconf(_SC_PAGESIZE));
    if (committed < used(fixture->count) || committed > fixture->bytes) fail("invalid frozen commit");
    RhUpdateFrozenSegment(fixture->segment, fixture->memory + used(fixture->count), fixture->memory + committed);
    fixture->committed = committed;
}
extern "C" void *e2d_frozen_address(Fixture *fixture, uint32_t index) {
    if (!fixture || index >= fixture->count) fail("invalid frozen index");
    return fixture->memory + 8 + static_cast<size_t>(index) * 24;
}
extern "C" void e2d_frozen_destroy(Fixture *fixture) {
    if (!fixture) fail("invalid frozen destroy");
    RhUnregisterFrozenSegment(fixture->segment);
    if (munmap(fixture->memory, fixture->bytes)) fail("frozen unmap failed");
    std::free(fixture);
}
