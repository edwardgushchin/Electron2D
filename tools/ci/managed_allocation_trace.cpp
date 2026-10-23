// The CLR owns these static COM objects for the profiling session.
// ICorProfilerInfo2 uses a versioned ABI; calibrate its allocation and stack slots before recording.
#include <atomic>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <mutex>
#include <unistd.h>
#if !defined(__APPLE__)
#include <sys/syscall.h>
#endif
#include <pthread.h>
static long threadID() {
#if defined(__APPLE__)
 uint64_t value=0; if(pthread_threadid_np(nullptr,&value)) _exit(133); return static_cast<long>(value);
#else
 return syscall(SYS_gettid);
#endif
}
struct GUID { uint32_t a; uint16_t b,c; uint8_t d[8]; };
static constexpr GUID unknown{0,0,0,{0xc0,0,0,0,0,0,0,0x46}},factoryID{1,0,0,{0xc0,0,0,0,0,0,0,0x46}};
static constexpr GUID callbackID{0x176fbed1,0xa55c,0x4796,{0x98,0xca,0xa9,0xda,0x0e,0xf8,0x83,0xe7}},callback2ID{0x8a8cc829,0xccf2,0x49fe,{0xbb,0xae,0x0f,0x02,0x22,0x28,0x07,0x1a}};
static constexpr GUID info2ID{0xcc0935cd,0xa518,0x487d,{0xb0,0xbb,0xa9,0x32,0x14,0xe6,0x54,0x78}};
static void *info; static FILE *output; static std::mutex mutex; static std::atomic<uint64_t> active; static thread_local bool nested;
static std::atomic<uint64_t> recursion; static void *callbacks[80]; struct Object { void **table; }; static Object profiler{callbacks};
static bool equal(const GUID *a,const GUID &b){return std::memcmp(a,&b,16)==0;}
template<class F> static F method(unsigned slot){return reinterpret_cast<F>((*static_cast<void***>(info))[slot]);}
static void flushOutput(){if(std::fflush(output)||std::ferror(output))_exit(134);}
static void cleanup(){if(output){std::fclose(output);output=nullptr;}if(info){method<uint32_t(*)(void*)>(2)(info);info=nullptr;}}
static int32_t noOperation(...){return 0;}
static uint32_t addRef(void*){return 2;} static uint32_t release(void*){return 1;}
static int32_t query(void*,const GUID *id,void **target){if(equal(id,unknown)||equal(id,callbackID)||equal(id,callback2ID)){*target=&profiler;return 0;}*target=nullptr;return static_cast<int32_t>(0x80004002);}
static void moduleName(uintptr_t module,char *name){
 char16_t wide[1024]{};uint32_t size=0;
 int32_t hr=method<int32_t(*)(void*,uintptr_t,void**,uint32_t,uint32_t*,char16_t*,uintptr_t*)>(20)(info,module,nullptr,1024,&size,wide,nullptr);
 if(hr<0){std::snprintf(name,1024,"module:%zx/hr:%x",module,static_cast<uint32_t>(hr));return;}
 unsigned i=0;for(;i<1023&&wide[i];i++)name[i]=wide[i]<128?static_cast<char>(wide[i]):'?';name[i]=0;
}
static int32_t frame(uintptr_t function,uintptr_t ip,uintptr_t,uint32_t,uint8_t*,void*){
 if(!function)return 0;uintptr_t cls=0,module=0;uint32_t token=0;
 int32_t hr=method<int32_t(*)(void*,uintptr_t,uintptr_t*,uintptr_t*,uint32_t*)>(15)(info,function,&cls,&module,&token);
 char name[1024]{};if(hr>=0)moduleName(module,name);
 std::fprintf(output,"FRAME\t%zx\t%zx\t%08x\t%x\t%s\n",function,ip,token,static_cast<uint32_t>(hr),name);return 0;
}
static int32_t allocated(void*,uintptr_t object,uintptr_t cls){
 uint64_t tag=active.load(std::memory_order_acquire);if(!tag)return 0;if(nested){recursion++;return 0;}nested=true;
 uint32_t size=0,token=0;uintptr_t module=0;
 int32_t sizeHR=method<int32_t(*)(void*,uintptr_t,uint32_t*)>(10)(info,object,&size);
 int32_t classHR=method<int32_t(*)(void*,uintptr_t,uintptr_t*,uint32_t*)>(14)(info,cls,&module,&token);
 char name[1024]{},thread[64]{};if(classHR>=0)moduleName(module,name);pthread_getname_np(pthread_self(),thread,sizeof(thread));
 {std::lock_guard<std::mutex> hold(mutex);
  std::fprintf(output,"ALLOC\t%lu\t%ld\t%u\t%zx\t%08x\t%x\t%x\t%s\t%s\n",tag,threadID(),size,cls,token,static_cast<uint32_t>(sizeHR),static_cast<uint32_t>(classHR),thread,name);
  int32_t hr=method<int32_t(*)(void*,uintptr_t,decltype(&frame),uint32_t,void*,uint8_t*,uint32_t)>(36)(info,0,frame,0,nullptr,nullptr,0);
  std::fprintf(output,"STACK_END\t%x\n",static_cast<uint32_t>(hr));flushOutput();
 }
 nested=false;return 0;
}
static int32_t initialize(void*,void *source){
 using Q=int32_t(*)(void*,const GUID*,void**);int32_t hr=reinterpret_cast<Q>((*static_cast<void***>(source))[0])(source,&info2ID,&info);if(hr<0)return hr;
 const char *path=std::getenv("E2D_ALLOCATION_TRACE");if(!path){cleanup();return static_cast<int32_t>(0x80004005);}char file[4096];if(std::snprintf(file,sizeof(file),"%s.%ld",path,static_cast<long>(getpid()))>=static_cast<int>(sizeof(file))){cleanup();return static_cast<int32_t>(0x80004005);}output=std::fopen(file,"wx");if(!output){cleanup();return static_cast<int32_t>(0x80004005);}
 hr=method<int32_t(*)(void*,uint32_t)>(16)(info,0x00800000|0x00000100|0x10000000);
 std::fprintf(output,"INITIALIZE\t%x\n",static_cast<uint32_t>(hr));flushOutput();if(hr<0)cleanup();return hr;
}
static int32_t shutdown(void*){active=0;std::lock_guard<std::mutex> hold(mutex);if(output){std::fprintf(output,"SHUTDOWN\trecursive:%lu\n",recursion.load());flushOutput();}cleanup();return 0;}
static void *factoryMethods[5];static Object factory{factoryMethods};
static int32_t factoryQuery(void*,const GUID *id,void **target){if(equal(id,unknown)||equal(id,factoryID)){*target=&factory;return 0;}*target=nullptr;return static_cast<int32_t>(0x80004002);}
static int32_t create(void*,void*,const GUID *id,void **target){for(auto &slot:callbacks)slot=reinterpret_cast<void*>(noOperation);callbacks[0]=reinterpret_cast<void*>(query);callbacks[1]=reinterpret_cast<void*>(addRef);callbacks[2]=reinterpret_cast<void*>(release);callbacks[3]=reinterpret_cast<void*>(initialize);callbacks[4]=reinterpret_cast<void*>(shutdown);callbacks[50]=reinterpret_cast<void*>(allocated);return query(nullptr,id,target);}
extern "C" __attribute__((visibility("default"))) int32_t DllGetClassObject(const GUID*,const GUID *id,void **target){factoryMethods[0]=reinterpret_cast<void*>(factoryQuery);factoryMethods[1]=reinterpret_cast<void*>(addRef);factoryMethods[2]=reinterpret_cast<void*>(release);factoryMethods[3]=reinterpret_cast<void*>(create);factoryMethods[4]=reinterpret_cast<void*>(noOperation);return factoryQuery(nullptr,id,target);}
extern "C" __attribute__((visibility("default"))) int32_t DllCanUnloadNow(){return 1;}
extern "C" __attribute__((visibility("default"))) void e2d_gc_trace_begin(uint64_t tag){if(!output||!info||!tag)_exit(132);std::lock_guard<std::mutex> hold(mutex);std::fprintf(output,"BEGIN\t%lu\t%ld\n",tag,threadID());flushOutput();active.store(tag,std::memory_order_release);}
extern "C" __attribute__((visibility("default"))) void e2d_gc_trace_end(){auto tag=active.exchange(0);std::lock_guard<std::mutex> hold(mutex);std::fprintf(output,"END\t%lu\t%ld\n",tag,threadID());flushOutput();}
extern "C" __attribute__((visibility("default"))) void e2d_gc_trace_test_unwritable_output(){
 if(!output||!info)_exit(132);
 output=std::freopen("/dev/null","r",output);if(!output)_exit(135);
 std::fputs("OUTPUT_FAILURE\n",output);flushOutput();_exit(135);
}
