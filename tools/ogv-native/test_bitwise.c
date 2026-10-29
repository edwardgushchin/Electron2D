#include "bridge.h"
#include "memory.h"
#include "../enet-native/memory.h"
#include <ogg/ogg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
extern void e2d_test_arm(size_t);
extern size_t e2d_test_attempts(void);
static int msb;
static unsigned char input[8193];
static void check(int value, const char *message) { if (!value) { fprintf(stderr, "%s\n", message); exit(1); } }
static void write_bits(oggpack_buffer *b, unsigned long value, int bits) {
    if(msb)oggpackB_write(b,value,bits);else oggpack_write(b,value,bits);
}
static uint32_t read_bits(oggpack_buffer *b, int bits) {
    return (uint32_t)(msb?oggpackB_read(b,bits):oggpack_read(b,bits));
}
static void reader(oggpack_buffer *read, oggpack_buffer *write) {
    if(msb)oggpackB_readinit(read,write->buffer,(int)oggpack_bytes(write));
    else oggpack_readinit(read,write->buffer,(int)oggpack_bytes(write));
}
static int exercise(void) {
    oggpack_buffer b = {0}, read;
    if(msb)oggpackB_writeinit(&b);else oggpack_writeinit(&b);
    for(int i=0;i<2048;i++) {
        int bits=i%33; uint32_t value=0x98765432U^(uint32_t)i;
        write_bits(&b,value,bits);
        if(oggpack_writecheck(&b))goto failed;
    }
    reader(&read,&b);
    for(int i=0;i<2048;i++) {
        int bits=i%33; uint32_t mask=bits==32?UINT32_MAX:bits?(1U<<bits)-1:0;
        check(read_bits(&read,bits)==((0x98765432U^(uint32_t)i)&mask),"Scalar bit round-trip changed");
    }
    E2DMemorySnapshot before,after; e2d_memory_snapshot(&before);
    check(before.nativeCurrent==(uint64_t)b.storage,"Bit-buffer requested capacity is uncounted");
    for(int i=0;i<1000;i++) { oggpack_reset(&b); write_bits(&b,(unsigned)i,32); }
    e2d_memory_snapshot(&after);
    check(before.allocations==after.allocations && before.resizes==after.resizes && before.frees==after.frees,"Prepared bit writer allocated");
    oggpack_writeclear(&b);
    for(int alignment=0;alignment<2;alignment++) {
        if(msb)oggpackB_writeinit(&b);else oggpack_writeinit(&b);
        if(oggpack_writecheck(&b))goto failed;
        if(alignment)write_bits(&b,5,3);
        if(msb)oggpackB_writecopy(&b,input,8192*8+3);else oggpack_writecopy(&b,input,8192*8+3);
        if(oggpack_writecheck(&b))goto failed;
        if(msb)oggpackB_writealign(&b);else oggpack_writealign(&b);
        reader(&read,&b);
        if(alignment)check(read_bits(&read,3)==5,"Unaligned prefix changed");
        for(int i=0;i<8192;i++)check(read_bits(&read,8)==input[i],"Copied octet changed");
        check(read_bits(&read,3)==(msb?input[8192]>>5:input[8192]&7),"Copied partial octet changed");
        check(read_bits(&read,alignment?2:5)==0,"Alignment bits are not zero");
        reader(&read,&b);uint32_t prefix=read_bits(&read,7);
        if(msb)oggpackB_writetrunc(&b,7);else oggpack_writetrunc(&b,7);
        write_bits(&b,0x76543,19);reader(&read,&b);
        check(read_bits(&read,7)==prefix && read_bits(&read,19)==0x76543,"Truncate/append changed bit data");
        oggpack_writeclear(&b);
    }
    return 0;
failed:
    check(!b.buffer && !b.ptr && !b.storage,"Refused bit buffer is not invalidated");
    oggpack_writeclear(&b);return -1;
}
int main(int argc,char **argv) {
    check(argc==3 && (!strcmp(argv[1],"lsb")||!strcmp(argv[1],"msb")),"Expected lsb/msb and failure position");
    msb=!strcmp(argv[1],"msb");
    for(size_t i=0;i<sizeof(input);i++)input[i]=(unsigned char)(i*71U+i/3);
    E2DMemoryFunctions memory;e2d_memory_functions(&memory);check(!e2d_ogv_memory(&memory),"Registration failed");
    size_t position=(size_t)strtoull(argv[2],NULL,10);e2d_test_arm(position);
    uint64_t epoch=e2d_ogv_failure_epoch();int status=exercise();size_t attempts=e2d_test_attempts();
    E2DMemorySnapshot state;e2d_memory_snapshot(&state);
    check(status==(position?-1:0) && state.failures==!!position,"Bit allocation refusal was not reported");
    check((e2d_ogv_failure_epoch()!=epoch)==!!position,"Bit refusal epoch is absent");
    check(!state.nativeCurrent && state.allocated==state.retired,"Bit storage did not drain");
    e2d_test_arm(0);check(!exercise(),"Bit writer retry failed");e2d_memory_snapshot(&state);
    check(!state.nativeCurrent && state.allocated==state.retired,"Bit retry did not drain");
    printf("{\"role\":\"%s\",\"attempts\":%zu,\"failure\":%zu,\"peak\":%llu}\n",argv[1],attempts,position,(unsigned long long)state.nativePeak);
    return 0;
}
