/* Test-only closure error after the actual production teardown. */
#define e2d_rtc_close e2d_rtc_actual_close
#include "bridge.cpp"
#undef e2d_rtc_close
API int e2d_rtc_close(void* handle) {
    auto result=e2d_rtc_actual_close(handle);
    if(result==0){error="Injected native close failure";return -1;}
    return result;
}
