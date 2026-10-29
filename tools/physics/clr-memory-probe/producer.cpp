// The VM's typed publication outlives Alloc's stack frame; retain its native scope until then.
#include "../aot-memory-probe/producer.cpp"
extern "C" void e2d_clr_prepare(size_t size, void *type, uintptr_t elements, bool concurrent) {
    void *storage = std::malloc(sizeof(CommonScope));
    if (!storage) fail("CLR common scope storage exhausted");
    e2d_aot_common_prepare(storage, size, type, elements, concurrent);
}
extern "C" void e2d_clr_completed(void *object, size_t size) {
    auto storage = commonScope;
    e2d_aot_allocated(object, size, 3);
    std::free(storage);
}
extern "C" void e2d_clr_aborted() {
    auto storage = commonScope;
    e2d_aot_common_abort();
    std::free(storage);
}
