#include <windows.h>
#include <d3d12.h>
#include <d3d12sdklayers.h>
#include <dxgi1_4.h>
#include <wrl/client.h>

#include <cstdint>
#include <cstdio>
#include <vector>

using Microsoft::WRL::ComPtr;

static HRESULT Upload(ID3D12Device* device, ID3D12Resource* texture, const D3D12_RESOURCE_DESC& description,
    bool sampled = false, UINT phase = 0, bool rewrite = false)
{
    const UINT count = description.DepthOrArraySize * description.MipLevels;
    std::vector<D3D12_PLACED_SUBRESOURCE_FOOTPRINT> footprints(count);
    UINT64 bytes;
    device->GetCopyableFootprints(&description, 0, count, 0, footprints.data(), nullptr, nullptr, &bytes);
    D3D12_RESOURCE_DESC buffer{};
    buffer.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
    buffer.Width = bytes;
    buffer.Height = 1;
    buffer.DepthOrArraySize = 1;
    buffer.MipLevels = 1;
    buffer.SampleDesc.Count = 1;
    buffer.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
    const D3D12_HEAP_PROPERTIES uploadHeap{ D3D12_HEAP_TYPE_UPLOAD, D3D12_CPU_PAGE_PROPERTY_UNKNOWN,
        D3D12_MEMORY_POOL_UNKNOWN, 1, 1 };
    ComPtr<ID3D12Resource> upload;
    HRESULT result = device->CreateCommittedResource(&uploadHeap, D3D12_HEAP_FLAG_NONE, &buffer,
        D3D12_RESOURCE_STATE_GENERIC_READ, nullptr, IID_PPV_ARGS(&upload));
    if (FAILED(result)) return result;

    void* mapped;
    const D3D12_RANGE noRead{ 0, 0 };
    if (FAILED(result = upload->Map(0, &noRead, &mapped))) return result;
    constexpr std::uint32_t colors[] = { 0xff0000ff, 0xff00ff00, 0xffff0000, 0xff00ffff };
    for (UINT index = 0; index < count; ++index)
    {
        const auto& footprint = footprints[index];
        const auto color = colors[(phase + index % description.MipLevels + 2 * (index / description.MipLevels)) % 4];
        for (UINT y = 0; y < footprint.Footprint.Height; ++y)
        {
            auto row = reinterpret_cast<std::uint32_t*>(static_cast<char*>(mapped) + footprint.Offset +
                static_cast<size_t>(y) * footprint.Footprint.RowPitch);
            for (UINT x = 0; x < footprint.Footprint.Width; ++x) row[x] = color;
            if (!sampled && y + 1 == description.Height)
            {
                row[description.Width - 2] = 0x002d11ee;
                row[description.Width - 1] = 0x8000ff00;
            }
        }
    }
    upload->Unmap(0, nullptr);

    ComPtr<ID3D12CommandQueue> queue;
    ComPtr<ID3D12CommandAllocator> allocator;
    ComPtr<ID3D12GraphicsCommandList> list;
    ComPtr<ID3D12Fence> fence;
    const D3D12_COMMAND_QUEUE_DESC queueDescription{};
    if (FAILED(result = device->CreateCommandQueue(&queueDescription, IID_PPV_ARGS(&queue))) ||
        FAILED(result = device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_PPV_ARGS(&allocator))) ||
        FAILED(result = device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, allocator.Get(), nullptr, IID_PPV_ARGS(&list))) ||
        FAILED(result = device->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(&fence))))
        return result;

    D3D12_TEXTURE_COPY_LOCATION source{};
    source.pResource = upload.Get();
    source.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT;
    D3D12_TEXTURE_COPY_LOCATION destination{};
    destination.pResource = texture;
    destination.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
    D3D12_RESOURCE_BARRIER barrier{};
    barrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
    barrier.Transition.pResource = texture;
    barrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
    barrier.Transition.StateBefore = D3D12_RESOURCE_STATE_ALL_SHADER_RESOURCE;
    barrier.Transition.StateAfter = D3D12_RESOURCE_STATE_COPY_DEST;
    if (rewrite) list->ResourceBarrier(1, &barrier);
    for (UINT index = 0; index < count; ++index)
    {
        source.PlacedFootprint = footprints[index];
        destination.SubresourceIndex = index;
        list->CopyTextureRegion(&destination, 0, 0, 0, &source, nullptr);
    }
    if (sampled)
    {
        barrier.Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_DEST;
        barrier.Transition.StateAfter = D3D12_RESOURCE_STATE_ALL_SHADER_RESOURCE;
        list->ResourceBarrier(1, &barrier);
    }
    if (FAILED(result = list->Close())) return result;
    ID3D12CommandList* commands[] = { list.Get() };
    queue->ExecuteCommandLists(1, commands);
    if (FAILED(result = queue->Signal(fence.Get(), 1))) return result;
    // The null event makes this call wait for the producer fence. No commands follow the handoff.
    return fence->SetEventOnCompletion(1, nullptr);
}

extern "C" __declspec(dllexport) HRESULT __cdecl CreateSample(ID3D12Resource* reference, UINT format,
    UINT layers, BOOL foreignDevice, ID3D12Resource** output)
{
    *output = nullptr;
    ComPtr<ID3D12Device> device;
    HRESULT result;
    if (foreignDevice)
    {
        ComPtr<IDXGIFactory4> factory;
        ComPtr<IDXGIAdapter> warp;
        if (FAILED(result = CreateDXGIFactory2(0, IID_PPV_ARGS(&factory))) ||
            FAILED(result = factory->EnumWarpAdapter(IID_PPV_ARGS(&warp))) ||
            FAILED(result = D3D12CreateDevice(warp.Get(), D3D_FEATURE_LEVEL_11_0, IID_PPV_ARGS(&device))))
            return result;
    }
    else if (FAILED(result = reference->GetDevice(IID_PPV_ARGS(&device)))) return result;

    D3D12_RESOURCE_DESC description{};
    description.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
    description.Width = 7;
    description.Height = 3;
    description.DepthOrArraySize = static_cast<UINT16>(layers);
    description.MipLevels = 1;
    description.Format = static_cast<DXGI_FORMAT>(format);
    description.SampleDesc.Count = 1;
    const D3D12_HEAP_PROPERTIES heap{ D3D12_HEAP_TYPE_DEFAULT, D3D12_CPU_PAGE_PROPERTY_UNKNOWN,
        D3D12_MEMORY_POOL_UNKNOWN, 1, 1 };
    ComPtr<ID3D12Resource> texture;
    if (FAILED(result = device->CreateCommittedResource(&heap, D3D12_HEAP_FLAG_NONE, &description,
        D3D12_RESOURCE_STATE_COPY_DEST, nullptr, IID_PPV_ARGS(&texture)))) return result;
    if (!foreignDevice && layers == 1 && (format == DXGI_FORMAT_R8G8B8A8_UNORM || format == DXGI_FORMAT_B8G8R8A8_UNORM))
        if (FAILED(result = Upload(device.Get(), texture.Get(), description))) return result;
    *output = texture.Detach();
    return S_OK;
}

extern "C" __declspec(dllexport) HRESULT __cdecl CreateGPUSample(ID3D12Device* device, UINT layers,
    UINT levels, UINT options, ID3D12Resource** output)
{
    *output = nullptr;
    D3D12_RESOURCE_DESC description{};
    description.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
    description.Width = 7;
    description.Height = 3;
    description.DepthOrArraySize = static_cast<UINT16>(layers);
    description.MipLevels = static_cast<UINT16>(levels);
    description.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    description.SampleDesc.Count = 1;
    const D3D12_HEAP_PROPERTIES heap{ D3D12_HEAP_TYPE_DEFAULT, D3D12_CPU_PAGE_PROPERTY_UNKNOWN,
        D3D12_MEMORY_POOL_UNKNOWN, 1, 1 };
    ComPtr<ID3D12Resource> texture;
    if (options & 2) description.Flags = D3D12_RESOURCE_FLAG_ALLOW_SIMULTANEOUS_ACCESS;
    HRESULT result = device->CreateCommittedResource(&heap, options & 1 ? D3D12_HEAP_FLAG_SHARED : D3D12_HEAP_FLAG_NONE,
        &description, D3D12_RESOURCE_STATE_COPY_DEST, nullptr, IID_PPV_ARGS(&texture));
    if (FAILED(result)) return result;
    if (FAILED(result = Upload(device, texture.Get(), description, true))) return result;
    *output = texture.Detach();
    return S_OK;
}

extern "C" __declspec(dllexport) HRESULT __cdecl RewriteGPUSample(ID3D12Resource* texture, UINT phase)
{
    ComPtr<ID3D12Device> device;
    const HRESULT result = texture->GetDevice(IID_PPV_ARGS(&device));
    return FAILED(result) ? result : Upload(device.Get(), texture, texture->GetDesc(), true, phase, true);
}

extern "C" __declspec(dllexport) HRESULT __cdecl EnableDebug()
{
    ComPtr<ID3D12Debug> debug;
    const auto result = D3D12GetDebugInterface(IID_PPV_ARGS(&debug));
    if (SUCCEEDED(result)) debug->EnableDebugLayer();
    return result;
}

extern "C" __declspec(dllexport) HRESULT __cdecl CheckDebug(ID3D12Resource* reference)
{
    ComPtr<ID3D12InfoQueue> messages;
    const auto result = reference->GetDevice(IID_PPV_ARGS(&messages));
    if (FAILED(result)) return result;
    HRESULT status = S_OK;
    for (UINT64 i = 0; i < messages->GetNumStoredMessagesAllowedByRetrievalFilter(); ++i)
    {
        SIZE_T size = 0;
        if (FAILED(messages->GetMessage(i, nullptr, &size))) return E_FAIL;
        std::vector<unsigned char> bytes(size);
        auto message = reinterpret_cast<D3D12_MESSAGE*>(bytes.data());
        if (FAILED(messages->GetMessage(i, message, &size))) return E_FAIL;
        if (message->Severity == D3D12_MESSAGE_SEVERITY_ERROR || message->Severity == D3D12_MESSAGE_SEVERITY_CORRUPTION)
        {
            std::fprintf(stderr, "%s\n", message->pDescription);
            status = E_FAIL;
        }
    }
    return status;
}
