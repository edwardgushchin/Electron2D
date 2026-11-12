#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d12.h>
#include <dxgi1_6.h>
#include <wrl/client.h>
#include <cstdint>

using Microsoft::WRL::ComPtr;

struct Resources
{
    uint64_t factory, adapter, device, queue, image, view, sampler, buffer, render, compute;
};

template <class T>
static T* Object(uint64_t bits)
{
    return reinterpret_cast<T*>(static_cast<uintptr_t>(bits));
}

static bool HasDevice(ID3D12DeviceChild* child, ID3D12Device* device)
{
    ComPtr<ID3D12Device> owner;
    return SUCCEEDED(child->GetDevice(IID_PPV_ARGS(&owner))) && owner.Get() == device;
}

static bool CopyDescriptor(ID3D12Device* device, uint64_t descriptor, D3D12_DESCRIPTOR_HEAP_TYPE type)
{
    D3D12_DESCRIPTOR_HEAP_DESC description{};
    description.Type = type;
    description.NumDescriptors = 1;
    ComPtr<ID3D12DescriptorHeap> heap;
    if (FAILED(device->CreateDescriptorHeap(&description, IID_PPV_ARGS(&heap))))
        return false;
    device->CopyDescriptorsSimple(1, heap->GetCPUDescriptorHandleForHeapStart(),
        D3D12_CPU_DESCRIPTOR_HANDLE{ static_cast<SIZE_T>(descriptor) }, type);
    return true;
}

extern "C" __declspec(dllexport) int __cdecl VerifyDriverResources(const Resources& handles)
{
    auto* factory = Object<IDXGIFactory4>(handles.factory);
    auto* adapter = Object<IDXGIAdapter1>(handles.adapter);
    auto* device = Object<ID3D12Device>(handles.device);
    auto* queue = Object<ID3D12CommandQueue>(handles.queue);
    auto* image = Object<ID3D12Resource>(handles.image);
    auto* buffer = Object<ID3D12Resource>(handles.buffer);
    auto* render = Object<ID3D12PipelineState>(handles.render);
    auto* compute = Object<ID3D12PipelineState>(handles.compute);
    if (!factory || !adapter || !device || !queue || !image || !buffer || !render || !compute ||
        !handles.view || !handles.sampler) return 1;

    DXGI_ADAPTER_DESC1 info{}, enumeratedInfo{};
    const auto luid = device->GetAdapterLuid();
    ComPtr<IDXGIAdapter1> enumerated;
    if (FAILED(adapter->GetDesc1(&info)) ||
        info.AdapterLuid.LowPart != luid.LowPart || info.AdapterLuid.HighPart != luid.HighPart ||
        FAILED(factory->EnumAdapterByLuid(luid, IID_PPV_ARGS(&enumerated))) ||
        FAILED(enumerated->GetDesc1(&enumeratedInfo)) ||
        enumeratedInfo.AdapterLuid.LowPart != luid.LowPart || enumeratedInfo.AdapterLuid.HighPart != luid.HighPart ||
        info.VendorId != enumeratedInfo.VendorId || info.DeviceId != enumeratedInfo.DeviceId) return 2;

    if (!HasDevice(queue, device) || queue->GetDesc().Type != D3D12_COMMAND_LIST_TYPE_DIRECT ||
        !HasDevice(image, device) || !HasDevice(buffer, device) ||
        !HasDevice(render, device) || !HasDevice(compute, device) || render == compute) return 3;

    const auto imageInfo = image->GetDesc();
    const auto bufferInfo = buffer->GetDesc();
    if (imageInfo.Dimension != D3D12_RESOURCE_DIMENSION_TEXTURE2D || imageInfo.Width != 8 ||
        imageInfo.Height != 8 || imageInfo.DepthOrArraySize != 1 || imageInfo.MipLevels != 1 ||
        imageInfo.Format != DXGI_FORMAT_R8G8B8A8_UNORM || imageInfo.SampleDesc.Count != 1 ||
        bufferInfo.Dimension != D3D12_RESOURCE_DIMENSION_BUFFER || bufferInfo.Width < 48) return 4;

    if (!CopyDescriptor(device, handles.view, D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV) ||
        !CopyDescriptor(device, handles.sampler, D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER)) return 5;

    ComPtr<ID3D12Fence> fence;
    if (FAILED(device->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(&fence)))) return 6;
    const HANDLE event = CreateEventW(nullptr, FALSE, FALSE, nullptr);
    if (!event) return 6;
    const bool completed = SUCCEEDED(queue->Signal(fence.Get(), 1)) &&
        SUCCEEDED(fence->SetEventOnCompletion(1, event)) &&
        WaitForSingleObject(event, 5000) == WAIT_OBJECT_0 && fence->GetCompletedValue() == 1;
    CloseHandle(event);
    return completed && SUCCEEDED(device->GetDeviceRemovedReason()) ? 0 : 6;
}

extern "C" __declspec(dllexport) int __cdecl InspectTexture(uint64_t bits, uint32_t width, uint32_t height, uint16_t layers)
{
    auto* resource = Object<ID3D12Resource>(bits);
    if (!resource) return 1;
    const auto desc = resource->GetDesc();
    return desc.Dimension == D3D12_RESOURCE_DIMENSION_TEXTURE2D && desc.Width == width &&
        desc.Height == height && desc.DepthOrArraySize == layers && desc.MipLevels &&
        desc.Format != DXGI_FORMAT_UNKNOWN && desc.SampleDesc.Count == 1 ? 0 : 2;
}
