.text
.globl __wrap_RhpNewFast
.type __wrap_RhpNewFast,@function
__wrap_RhpNewFast:
    lock incq e2d_fast_entries(%rip)
    jmp __real_RhpNewFast
.size __wrap_RhpNewFast, .-__wrap_RhpNewFast
.hidden e2d_fast_entries
.section .note.GNU-stack,"",@progbits
