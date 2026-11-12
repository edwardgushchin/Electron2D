#include <d3d11.h>
#include <d3d12.h>
#include <dxcore.h>
#include <dxgi1_4.h>
#include <wrl/client.h>
#include <cstdint>

using Microsoft::WRL::ComPtr;

struct AdapterIdentity
{
    wchar_t name[128];
    char driverName[512];
    uint64_t driverVersion;
    LUID luid;
    UINT vendor, revision, type, level;
};

extern "C" __declspec(dllexport) int e2d_test_direct3d_adapter(void* device, UINT api, AdapterIdentity* identity)
{
    if (!device || !identity || (api != 11 && api != 12)) return 0;
    *identity = {};
    ComPtr<IDXGIAdapter1> adapter;
    if (api == 11) {
        auto* d3d11 = static_cast<ID3D11Device*>(device);
        ComPtr<IDXGIDevice> dxgi;
        ComPtr<IDXGIAdapter> base;
        if (FAILED(d3d11->QueryInterface(IID_PPV_ARGS(&dxgi))) ||
            FAILED(dxgi->GetAdapter(&base)) ||
            FAILED(base.As(&adapter))) return 0;
        identity->level = d3d11->GetFeatureLevel();
    } else {
        auto* d3d12 = static_cast<ID3D12Device*>(device);
        ComPtr<IDXGIFactory4> factory;
        if (FAILED(CreateDXGIFactory1(IID_PPV_ARGS(&factory))) ||
            FAILED(factory->EnumAdapterByLuid(d3d12->GetAdapterLuid(), IID_PPV_ARGS(&adapter)))) return 0;

        const D3D_FEATURE_LEVEL requested[] = {
            D3D_FEATURE_LEVEL_12_2, D3D_FEATURE_LEVEL_12_1, D3D_FEATURE_LEVEL_12_0,
            D3D_FEATURE_LEVEL_11_1, D3D_FEATURE_LEVEL_11_0,
        };
        D3D12_FEATURE_DATA_FEATURE_LEVELS levels{ ARRAYSIZE(requested), requested, D3D_FEATURE_LEVEL_1_0_CORE };
        if (FAILED(d3d12->CheckFeatureSupport(D3D12_FEATURE_FEATURE_LEVELS, &levels, sizeof(levels)))) return 0;
        identity->level = levels.MaxSupportedFeatureLevel;
    }

    DXGI_ADAPTER_DESC1 desc{};
    if (FAILED(adapter->GetDesc1(&desc))) return 0;
    wcscpy_s(identity->name, desc.Description);
    identity->luid = desc.AdapterLuid;
    identity->vendor = desc.VendorId;
    identity->revision = desc.Revision;
    identity->type = (desc.Flags & DXGI_ADAPTER_FLAG_SOFTWARE) ? 4u : (desc.DedicatedVideoMemory ? 2u : 1u);

    ComPtr<IDXCoreAdapterFactory> core;
    ComPtr<IDXCoreAdapter> coreAdapter;
    if (FAILED(DXCoreCreateAdapterFactory(IID_PPV_ARGS(&core))) ||
        FAILED(core->GetAdapterByLuid(desc.AdapterLuid, IID_PPV_ARGS(&coreAdapter))) ||
        FAILED(coreAdapter->GetProperty(DXCoreAdapterProperty::DriverVersion, &identity->driverVersion))) return 0;

    size_t bytes = 0;
    if (FAILED(coreAdapter->GetPropertySize(DXCoreAdapterProperty::DriverDescription, &bytes)) ||
        bytes == 0 || bytes > sizeof(identity->driverName) ||
        FAILED(coreAdapter->GetProperty(DXCoreAdapterProperty::DriverDescription, bytes, identity->driverName))) return 0;
    return 1;
}
