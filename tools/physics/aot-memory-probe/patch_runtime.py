#!/usr/bin/env python3
"""Instrument an extracted v10.0.1 runtime copy; never patch installed SDK archives."""
import difflib
import hashlib
from pathlib import Path
import sys


def patch(root, output):
    pins = {
        'src/coreclr/runtime/amd64/AllocFast.S': '0e4fc5b3fe604baa8ccad94ade11a9b1251e06e4d2b48761e932291191ed8c25',
        'src/coreclr/nativeaot/Runtime/GCHelpers.cpp': '7c21403b38e035b712384917f198506cd15e15fbae0b18a15ea5ed7129abd226',
        'src/coreclr/nativeaot/Runtime/gcenv.ee.cpp': '34a4106345051e0c58ed1a03dc124f64c66ce6037c6bea1ea2b5fd1bcae59550'}
    originals = {}
    for name, expected in pins.items():
        path = root / name
        if hashlib.sha256(path.read_bytes()).hexdigest() != expected:
            raise ValueError('Use an unmodified v10.0.1 source copy: ' + name)
        originals[name] = path.read_text()
    changed = dict(originals)

    def replace(name, old, new):
        if changed[name].count(old) != 1:
            raise ValueError('Unexpected patch anchor: ' + name)
        changed[name] = changed[name].replace(old, new)

    fast, helpers, collector = pins
    replace(fast, '#include "AsmMacros_Shared.h"', '#include "AsmMacros_Shared.h"\n.extern e2d_aot_allocated')
    replace(fast, '        mov         rax, rsi\n\n        .cfi_remember_state', '''        // No GC or managed calls in this observer; allocation helpers use the standard caller-trash ABI.
        mov         rdi, rsi
        mov         rsi, rdx
        sub         rsi, rdi
        mov         rbx, rdi
        mov         edx, 1
        call        EXTERNAL_C_FUNC(e2d_aot_allocated)
        mov         rax, rbx

        .cfi_remember_state''')
    replace(fast, '''        mov         [rcx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], r13

        .cfi_remember_state''', '''        mov         [rcx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], r13

        mov         rdi, rax
        mov         rsi, r13
        sub         rsi, rax
        mov         rbx, rax
        mov         edx, 2
        call        EXTERNAL_C_FUNC(e2d_aot_allocated)
        mov         rax, rbx

        .cfi_remember_state''')
    replace(helpers, 'static Object* GcAllocInternal(', 'extern "C" void e2d_aot_allocated(void*, size_t, uint32_t);\nextern "C" bool e2d_aot_reject(void*, uintptr_t);\n\nstatic Object* GcAllocInternal(')
    replace(helpers, '    Object* pObject = GCHeapUtilities::GetGCHeap()->Alloc(pAllocContext, cbSize, uFlags);', '''    if (e2d_aot_reject(pEEType, numElements))
        return NULL;
    Object* pObject = GCHeapUtilities::GetGCHeap()->Alloc(pAllocContext, cbSize, uFlags);''')
    replace(helpers, '''    return pObject;
}

// Allocate an object on the GC heap.''', '''    e2d_aot_allocated(pObject, cbSize, 3);
    return pObject;
}

// Allocate an object on the GC heap.''')
    replace(collector, '''void GCToEEInterface::GcDone(int condemned)
{
''', '''extern "C" void e2d_aot_gc_begin(bool);
extern "C" void e2d_aot_gc_survivor(void*, size_t, bool);
extern "C" void e2d_aot_gc_end();
static bool E2DMemorySurvivor(Object* object, void*)
{
    e2d_aot_gc_survivor(object, object->GetSize(), GCHeapUtilities::GetGCHeap()->IsInFrozenSegment(object));
    return true;
}
void GCToEEInterface::GcDone(int condemned)
{
    auto heap = GCHeapUtilities::GetGCHeap();
    e2d_aot_gc_begin(!heap->IsConcurrentGCEnabled() && ThreadStore::GetSuspendingThread() != nullptr);
    heap->DiagWalkHeap(E2DMemorySurvivor, nullptr, heap->GetMaxGeneration(), true);
    e2d_aot_gc_end();
''')
    diff = ''.join(''.join(difflib.unified_diff(originals[name].splitlines(True), changed[name].splitlines(True),
                                             fromfile='a/' + name, tofile='b/' + name)) for name in pins)
    output.write_text(diff)
    for name in pins:
        (root / name).write_text(changed[name])
    print('Instrumented successful fast/common allocations and stopped-EE GC retirement in three pinned source files.')


if __name__ == '__main__':
    patch(Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve())
