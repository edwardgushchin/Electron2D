#!/usr/bin/env python3
"""Instrument a separate pinned CoreCLR copy, retaining the shared GC commit protocol."""
import difflib
import hashlib
import importlib.util
from pathlib import Path
import sys


def patch(root, output):
    pins = {
        'src/coreclr/gc/gc.cpp': '4d92ead47fc1472ea47f4c2c0b8d9246def3f49ac427f374703d1aadfe74de8c',
        'src/coreclr/vm/gchelpers.cpp': 'db21a2367bf0528bb7ddc1e68a0b73e238e5844e4a2280eee729f317f8b6889f',
        'src/coreclr/vm/gcenv.ee.cpp': '3207e7ca63b1c560b33293f0ca6390d6387aea76b80d6434f4f8c7e41f8128c9',
        'src/coreclr/dlls/mscoree/coreclr/CMakeLists.txt': 'b08679bd8510e0be1edc67885bcaf3523c4c45f1741ea633a6db5a8e2c8821e3',
        'src/coreclr/dlls/mscoree/mscorwks_unixexports.src': '143c94e504f057425644da68f177c4c7acf56ff2e195d19d5660947c5f7aa3b7'}
    original = {}
    for name, digest in pins.items():
        if hashlib.sha256((root / name).read_bytes()).hexdigest() != digest:
            raise ValueError('Use an unmodified v10.0.1 source copy: ' + name)
        original[name] = (root / name).read_text()
    changed = dict(original)

    def replace(name, old, new):
        if changed[name].count(old) != 1:
            raise ValueError('Unexpected CoreCLR patch anchor: ' + name)
        changed[name] = changed[name].replace(old, new)

    heap, helpers, collector, cmake, exports = pins
    spec = importlib.util.spec_from_file_location('shared_gc_patch', Path(__file__).parent.parent / 'aot-memory-probe/patch_runtime.py')
    shared = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(shared)
    changed[heap] = shared.patch_heap(original[heap], 'e2d_aot_')
    replace(helpers, '#include "frozenobjectheap.h"', '''#include "frozenobjectheap.h"
extern "C" void e2d_clr_prepare(size_t, void*, uintptr_t, bool);
extern "C" void e2d_clr_aborted();
extern "C" void e2d_clr_completed(void*, size_t);
extern "C" bool e2d_aot_reject(void*, uintptr_t);''')
    replace(helpers, '    retVal = GCHeapUtilities::GetGCHeap()->Alloc(pAllocContext, size, flags);', '''    auto e2dType = GetThread()->GetTHAllocContextObj();
    auto e2dMT = e2dType.IsNull() ? nullptr : e2dType.AsMethodTable();
    uintptr_t e2dElements = e2dMT && e2dMT->HasComponentSize() && size >= e2dMT->GetBaseSize()
        ? (size - e2dMT->GetBaseSize()) / e2dMT->GetComponentSize() : 0;
    e2d_clr_prepare(size, e2dMT, e2dElements, GCHeapUtilities::GetGCHeap()->IsConcurrentGCEnabled());
    retVal = GCHeapUtilities::GetGCHeap()->Alloc(pAllocContext, size, flags);
    if (!retVal) e2d_clr_aborted();''')
    replace(helpers, '    _ASSERTE(orObject->HasEmptySyncBlockInfo());', '''    _ASSERTE(orObject->HasEmptySyncBlockInfo());
    if (!GCHeapUtilities::GetGCHeap()->IsInFrozenSegment(orObject))
        e2d_clr_completed(orObject, static_cast<Object*>(orObject)->GetSize());''')
    replace(helpers, '    if ((SIZE_T)cElements > MaxArrayLength())\n        ThrowOutOfMemoryDimensionsExceeded();\n\n    // Allocate the space from the GC heap\n    SIZE_T componentSize', '''    if ((SIZE_T)cElements > MaxArrayLength())
        ThrowOutOfMemoryDimensionsExceeded();
    if (e2d_aot_reject(pArrayMT, cElements)) ThrowOutOfMemory();
    // Allocate the space from the GC heap
    SIZE_T componentSize''')
    replace(collector, 'void GCToEEInterface::GcDone(int condemned)\n{', '''extern "C" void e2d_aot_gc_begin(bool);
extern "C" void e2d_aot_gc_survivor(void*, size_t, bool);
extern "C" void e2d_aot_gc_end();
static bool E2DMemorySurvivor(Object* object, void*)
{
    if (object->GetMethodTable() != g_pFreeObjectMethodTable)
        e2d_aot_gc_survivor(object, object->GetSize(), GCHeapUtilities::GetGCHeap()->IsInFrozenSegment(object));
    return true;
}
void GCToEEInterface::GcDone(int condemned)
{
    auto heap = GCHeapUtilities::GetGCHeap();
    e2d_aot_gc_begin(!heap->IsConcurrentGCEnabled() && ThreadSuspend::SysIsSuspended());
    heap->DiagWalkHeap(E2DMemorySurvivor, nullptr, heap->GetMaxGeneration(), true);
    e2d_aot_gc_end();''')
    changed[collector] += '''
// Private diagnostic access to the same GC registry exercised by runtime image heaps.
extern "C" DLLEXPORT void* E2DRegisterFrozenSegment(void* memory, size_t allocated, size_t committed, size_t reserved)
{
    segment_info info;
    info.pvMem = memory; info.ibFirstObject = sizeof(ObjHeader);
    info.ibAllocated = allocated; info.ibCommit = committed; info.ibReserved = reserved;
    return GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&info);
}
extern "C" DLLEXPORT void E2DUpdateFrozenSegment(void* handle, uint8_t* allocated, uint8_t* committed)
{
    GCHeapUtilities::GetGCHeap()->UpdateFrozenSegment(static_cast<segment_handle>(handle), allocated, committed);
}
extern "C" DLLEXPORT void E2DUnregisterFrozenSegment(void* handle)
{
    GCHeapUtilities::GetGCHeap()->UnregisterFrozenSegment(static_cast<segment_handle>(handle));
}
'''
    changed[exports] += '\nE2DRegisterFrozenSegment\nE2DUpdateFrozenSegment\nE2DUnregisterFrozenSegment\n'
    changed[cmake] += '\ntarget_link_libraries(coreclr PRIVATE "${E2D_MEMORY_PRODUCER}")\n'
    output.write_text(''.join(''.join(difflib.unified_diff(original[n].splitlines(True), changed[n].splitlines(True),
                                                       fromfile='a/' + n, tofile='b/' + n)) for n in pins))
    for name in pins:
        (root / name).write_text(changed[name])
    print('Instrumented five pinned CoreCLR source/build files; installed runtime untouched.')


if __name__ == '__main__':
    patch(Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve())
