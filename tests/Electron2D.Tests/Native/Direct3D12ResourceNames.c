#define COBJMACROS
#include <initguid.h>
#include <d3d12.h>
#include <dxgi.h>
#include <wchar.h>

__declspec(dllexport) int e2d_test_direct3d12_name_matches(ID3D12Object *object, const wchar_t *expected)
{
    wchar_t name[128];
    UINT size = sizeof(name);
    HRESULT status = ID3D12Object_GetPrivateData(object, &WKPDID_D3DDebugObjectNameW, &size, name);
    if (!*expected && status == DXGI_ERROR_NOT_FOUND) return 1;
    size_t bytes = wcslen(expected) * sizeof(*expected);
    return SUCCEEDED(status) && (size == bytes || size == bytes + sizeof(*expected)) &&
        wmemcmp(name, expected, size / sizeof(*expected)) == 0;
}
