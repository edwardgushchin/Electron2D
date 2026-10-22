#if !defined(__linux__) || !defined(__x86_64__)
#error This diagnostic requires Linux x64.
#endif
#include <cstdint>
#include <cstring>
static void *(*get_tls)(void*); static void *descriptor;
// Reject a different counter instruction sequence before reading its TLS context.
extern "C" int context_initialize(const uint8_t *code) {
 if(!code)return -1;
 for(unsigned i=0;i<5;i++) {
  if(code[0]==0xff&&code[1]==0x25) {int32_t delta;std::memcpy(&delta,code+2,4);std::memcpy(&code,code+6+delta,sizeof(code));}
  else if(code[0]==0xe9) {int32_t delta;std::memcpy(&delta,code+1,4);code+=5+delta;}
  else break;
 }
 const uint8_t head[]={0x55,0x48,0x89,0xe5,0x66,0x48,0x8d,0x3d};
 const uint8_t call[]={0x66,0x66,0x48,0xe8};
 const uint8_t tail[]={0x48,0x8b,0x48,0x20,0x48,0x03,0x48,0x18,0x48,0x2b,0x48,0x10,0x48,0x03,0x48,0x08,0x48,0x89,0xc8,0x5d,0xc3};
 if(std::memcmp(code,head,sizeof(head))||std::memcmp(code+12,call,sizeof(call))||std::memcmp(code+20,tail,sizeof(tail)))return -1;
 int32_t lea,relative_call;std::memcpy(&lea,code+8,4);std::memcpy(&relative_call,code+16,4);
 descriptor=const_cast<uint8_t*>(code+12+lea);get_tls=reinterpret_cast<void*(*)(void*)>(const_cast<uint8_t*>(code+20+relative_call));return 0;
}
extern "C" void context_snapshot(uint64_t *out) {
 auto *base=static_cast<uint8_t*>(get_tls(descriptor));out[0]=reinterpret_cast<uintptr_t>(base);
 std::memcpy(out+1,base+8,32);
}
