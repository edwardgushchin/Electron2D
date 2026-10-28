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
        'src/coreclr/nativeaot/Runtime/gcenv.ee.cpp': '34a4106345051e0c58ed1a03dc124f64c66ce6037c6bea1ea2b5fd1bcae59550',
        'src/coreclr/gc/gc.cpp': '4d92ead47fc1472ea47f4c2c0b8d9246def3f49ac427f374703d1aadfe74de8c'}
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

    fast, helpers, collector, heap = pins
    replace(fast, '#include "AsmMacros_Shared.h"', '''#include "AsmMacros_Shared.h"
.extern e2d_aot_allocated
.extern e2d_aot_begin_fast

// Preserve every volatile GPR used by the surrounding fast allocation assembly.
LEAF_ENTRY E2DMemoryBeginFast, _TEXT
        push_nonvol_reg rax
        push_nonvol_reg rcx
        push_nonvol_reg rdx
        push_nonvol_reg rdi
        push_nonvol_reg rsi
        push_nonvol_reg r8
        push_nonvol_reg r9
        push_nonvol_reg r10
        push_nonvol_reg r11
        call EXTERNAL_C_FUNC(e2d_aot_begin_fast)
        pop_nonvol_reg r11
        pop_nonvol_reg r10
        pop_nonvol_reg r9
        pop_nonvol_reg r8
        pop_nonvol_reg rsi
        pop_nonvol_reg rdi
        pop_nonvol_reg rdx
        pop_nonvol_reg rcx
        pop_nonvol_reg rax
        ret
LEAF_END E2DMemoryBeginFast, _TEXT
''')
    replace(fast, '''        // Calculate the new alloc pointer to account for the allocation.
        add         rdx, rsi''', '''        call        C_FUNC(E2DMemoryBeginFast)
        // Calculate the new alloc pointer to account for the allocation.
        add         rdx, rsi''')
    replace(fast, '''        add         r13, rax
        mov         [rax + OFFSETOF__Object__m_pEEType], rbx''', '''        call        C_FUNC(E2DMemoryBeginFast)
        add         r13, rax
        mov         [rax + OFFSETOF__Object__m_pEEType], rbx''')
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
    replace(heap, '#include "gceventstatus.h"', '''#include "gceventstatus.h"
extern "C" bool e2d_aot_frozen_reject();
extern "C" void e2d_aot_frozen_change(unsigned, void*, uintptr_t, uintptr_t, uintptr_t, uintptr_t);''')
    replace(heap, '''    enter_spin_lock (&gc_heap::gc_lock);

    if (!gc_heap::seg_table->ensure_space_for_insert ()''', '''    enter_spin_lock (&gc_heap::gc_lock);

    if (e2d_aot_frozen_reject())
    {
        leave_spin_lock (&gc_heap::gc_lock);
        return FALSE;
    }
    if (!gc_heap::seg_table->ensure_space_for_insert ()''')
    replace(heap, '''    FIRE_EVENT(GCCreateSegment_V1, heap_segment_mem(seg), (size_t)(heap_segment_reserved (seg) - heap_segment_mem(seg)), gc_etw_segment_read_only_heap);

    leave_spin_lock (&gc_heap::gc_lock);
    return TRUE;
}

void gc_heap::update_ro_segment''', '''    // Publish while gc_lock still protects the registry from collection and competing changes.
    e2d_aot_frozen_change(1, seg, (uintptr_t)heap_segment_mem(seg), (uintptr_t)heap_segment_allocated(seg),
        (uintptr_t)heap_segment_committed(seg), (uintptr_t)heap_segment_reserved(seg));
    FIRE_EVENT(GCCreateSegment_V1, heap_segment_mem(seg), (size_t)(heap_segment_reserved (seg) - heap_segment_mem(seg)), gc_etw_segment_read_only_heap);
    leave_spin_lock (&gc_heap::gc_lock);
    return TRUE;
}

void gc_heap::update_ro_segment''')
    replace(heap, '''    heap_segment_committed (seg) = committed;

    leave_spin_lock (&gc_heap::gc_lock);
}

// No one is calling''', '''    heap_segment_committed (seg) = committed;

    e2d_aot_frozen_change(2, seg, (uintptr_t)heap_segment_mem(seg), (uintptr_t)allocated,
        (uintptr_t)committed, (uintptr_t)heap_segment_reserved(seg));
    leave_spin_lock (&gc_heap::gc_lock);
}

// No one is calling''')
    replace(heap, '''        generation_start_segment (gen2) = heap_segment_next (curr_seg);

    leave_spin_lock (&gc_heap::gc_lock);''', '''        generation_start_segment (gen2) = heap_segment_next (curr_seg);

    e2d_aot_frozen_change(3, seg, (uintptr_t)heap_segment_mem(seg), (uintptr_t)heap_segment_allocated(seg),
        (uintptr_t)heap_segment_committed(seg), (uintptr_t)heap_segment_reserved(seg));
    leave_spin_lock (&gc_heap::gc_lock);''')
    diff = ''.join(''.join(difflib.unified_diff(originals[name].splitlines(True), changed[name].splitlines(True),
                                             fromfile='a/' + name, tofile='b/' + name)) for name in pins)
    output.write_text(diff)
    for name in pins:
        (root / name).write_text(changed[name])
    print('Instrumented successful allocations, GC retirement and frozen registry lifetime in four pinned source files.')


if __name__ == '__main__':
    patch(Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve())
