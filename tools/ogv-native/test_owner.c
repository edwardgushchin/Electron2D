#include "bridge.h"
#include "memory.h"
#include "../enet-native/memory.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
extern void e2d_test_arm(size_t);
extern size_t e2d_test_attempts(void);
static void check(int value,const char *message) {if(!value){fprintf(stderr,"%s\n",message);exit(1);}}
static int callback(void) {
    E2DOGVOwner *owner=e2d_ogv_context_zero(sizeof(*owner));if(!owner)return -1;
    E2DOGVScope scope;check(!e2d_ogv_scope_enter(&scope,owner),"Nested scope entry failed");
    if(setjmp(scope.jump)) {
        e2d_ogv_scope_leave(&scope);e2d_ogv_owner_clear(owner);e2d_ogv_release(owner);return -1;
    }
    e2d_ogv_allocate(53);
    e2d_ogv_scope_leave(&scope);e2d_ogv_owner_clear(owner);e2d_ogv_release(owner);return 0;
}
static int exercise(void) {
    int rounding=fegetround();
    E2DOGVOwner *owner=e2d_ogv_context_zero(sizeof(*owner));if(!owner)return -1;
    E2DOGVScope scope;check(!e2d_ogv_scope_enter(&scope,owner),"Scope entry failed");
    if(setjmp(scope.jump)) {
        check(owner->failed,"OOM did not poison its owner");
        e2d_ogv_scope_leave(&scope);check(fegetround()==rounding,"OOM changed the caller's floating environment");
        check(e2d_ogv_scope_enter(&scope,owner)==-2,"Poisoned owner accepted another call");
        e2d_ogv_owner_clear(owner);e2d_ogv_release(owner);return -1;
    }
    E2DOGVScope rejected;check(e2d_ogv_scope_enter(&rejected,owner)==-1,"Reentrant owner accepted");
    fesetround(FE_DOWNWARD);
    void *blocks[96];
    for(size_t i=0;i<96;i++){blocks[i]=e2d_ogv_zero(i+1,3);memset(blocks[i],(int)i,(i+1)*3);}
    for(size_t i=0;i<96;i+=2){e2d_ogv_release(blocks[i]);blocks[i]=NULL;}
    for(size_t i=1;i<96;i+=2){
        blocks[i]=e2d_ogv_resize(blocks[i],(i+1)*7);
        for(size_t j=0;j<(i+1)*3;j++)check(((unsigned char *)blocks[i])[j]==i,"Resize changed retained bytes");
    }
    for(size_t i=0;i<96;i++)if(blocks[i])e2d_ogv_release(blocks[i]);
    void *zero=e2d_ogv_allocate(0);zero=e2d_ogv_resize(zero,0);e2d_ogv_release(zero);
    check(!owner->count,"Owner hash did not remove all payloads");
    unsigned char *held=e2d_ogv_allocate(37);memset(held,0x55,37);
    E2DOGVScope *saved=e2d_ogv_scope_suspend();int status=callback();e2d_ogv_scope_resume(saved);
    check(!owner->failed && owner->active && owner->count==1,"Callback contaminated its caller's owner");
    for(int i=0;i<37;i++)check(held[i]==0x55,"Callback retired its caller's payload");
    e2d_ogv_release(held);
    fesetround(rounding);e2d_ogv_scope_leave(&scope);
    E2DMemorySnapshot state;e2d_memory_snapshot(&state);
    check(state.nativeCurrent==sizeof(*owner)+owner->capacity*sizeof(void *),"Owner metadata is not counted");
    e2d_ogv_owner_clear(owner);e2d_ogv_release(owner);return status;
}
int main(int argc,char **argv) {
    check(argc==2,"Expected allocation failure position");
    E2DMemoryFunctions memory;e2d_memory_functions(&memory);check(!e2d_ogv_memory(&memory),"Registration failed");
    size_t position=(size_t)strtoull(argv[1],NULL,10);e2d_test_arm(position);
    int status=exercise();size_t attempts=e2d_test_attempts();E2DMemorySnapshot state;e2d_memory_snapshot(&state);
    check(status==(position?-1:0) && state.failures==!!position,"Owner refusal was not reported");
    check(!state.nativeCurrent && state.allocated==state.retired,"Owner rollback did not drain");
    e2d_test_arm(0);check(!exercise(),"Owner retry failed");e2d_memory_snapshot(&state);
    check(!state.nativeCurrent && state.allocated==state.retired,"Owner retry did not drain");
    printf("{\"attempts\":%zu,\"failure\":%zu,\"peak\":%llu}\n",attempts,position,(unsigned long long)state.nativePeak);return 0;
}
