#include <windows.h>
#include <d3d11.h>
#include <dxgi1_2.h>
#include <windows.graphics.capture.interop.h>
#include <windows.graphics.directx.direct3d11.interop.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Graphics.Capture.h>
#include <winrt/Windows.Graphics.DirectX.h>
#include <winrt/Windows.Graphics.DirectX.Direct3D11.h>

#include <cstdio>
#include <cstdlib>

using namespace winrt;
using namespace winrt::Windows::Graphics::Capture;
using namespace winrt::Windows::Graphics::DirectX;
using namespace winrt::Windows::Graphics::DirectX::Direct3D11;

static bool MatchesPattern(const D3D11_MAPPED_SUBRESOURCE& image, unsigned width, unsigned height,
    const POINT (&markers)[3], const RECT& monitor, unsigned (&samples)[3])
{
    constexpr unsigned expected[] = { 0xFF0000, 0x00FF00, 0x0000FF };
    constexpr int radius = 8;
    bool matches = true;
    for (int marker = 0; marker < 3; ++marker)
    {
        const auto x = markers[marker].x - monitor.left;
        const auto y = markers[marker].y - monitor.top;
        if (x < radius || y < radius || x + radius >= static_cast<long>(width) ||
            y + radius >= static_cast<long>(height))
        {
            matches = false;
            continue;
        }

        for (int dy = -radius; dy <= radius; ++dy)
        {
            for (int dx = -radius; dx <= radius; ++dx)
            {
                const auto pixel = static_cast<const unsigned char*>(image.pData) +
                    (y + dy) * image.RowPitch + 4 * (x + dx);
                const unsigned rgb = (pixel[2] << 16) | (pixel[1] << 8) | pixel[0];
                if (dx == 0 && dy == 0)
                    samples[marker] = rgb;
                matches &= rgb == expected[marker];
            }
        }
    }
    return matches;
}

static int CaptureMonitor(HWND window)
{
    MONITORINFO info{ sizeof(info) };
    const auto monitor = MonitorFromWindow(window, MONITOR_DEFAULTTONULL);
    if (!monitor || !GetMonitorInfoW(monitor, &info))
        return 2;
    POINT markers[] = { {120, 96}, {260, 96}, {120, 205} };
    for (auto& point : markers)
        if (!ClientToScreen(window, &point))
            return 2;

    const auto hit = WindowFromPoint(markers[0]);
    wchar_t type[128]{};
    DWORD targetPid = 0, hitPid = 0;
    GetWindowThreadProcessId(window, &targetPid);
    if (hit)
    {
        GetClassNameW(hit, type, 128);
        GetWindowThreadProcessId(hit, &hitPid);
    }
    std::printf("monitor=(%ld,%ld)-(%ld,%ld) red-anchor=(%ld,%ld) hit_owned=%d hit_class=%ls\n",
        info.rcMonitor.left, info.rcMonitor.top, info.rcMonitor.right, info.rcMonitor.bottom,
        markers[0].x, markers[0].y, hitPid != 0 && targetPid == hitPid, type);

    auto factory = get_activation_factory<GraphicsCaptureItem, IGraphicsCaptureItemInterop>();
    GraphicsCaptureItem item{ nullptr };
    check_hresult(factory->CreateForMonitor(monitor, guid_of<GraphicsCaptureItem>(), put_abi(item)));

    com_ptr<ID3D11Device> device;
    com_ptr<ID3D11DeviceContext> context;
    check_hresult(D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, D3D11_CREATE_DEVICE_BGRA_SUPPORT,
        nullptr, 0, D3D11_SDK_VERSION, device.put(), nullptr, context.put()));
    auto dxgi = device.as<IDXGIDevice>();
    com_ptr<IInspectable> inspectable;
    check_hresult(CreateDirect3D11DeviceFromDXGIDevice(dxgi.get(), inspectable.put()));
    auto pool = Direct3D11CaptureFramePool::CreateFreeThreaded(inspectable.as<IDirect3DDevice>(),
        DirectXPixelFormat::B8G8R8A8UIntNormalized, 2, item.Size());
    auto session = pool.CreateCaptureSession(item);
    session.IsCursorCaptureEnabled(false);
    session.StartCapture();

    const auto deadline = GetTickCount64() + 4500;
    unsigned samples[3]{};
    unsigned frames = 0;
    while (GetTickCount64() < deadline)
    {
        auto frame = pool.TryGetNextFrame();
        if (!frame)
        {
            Sleep(10);
            continue;
        }

        auto access = frame.Surface().as<::Windows::Graphics::DirectX::Direct3D11::IDirect3DDxgiInterfaceAccess>();
        com_ptr<ID3D11Texture2D> texture;
        check_hresult(access->GetInterface(__uuidof(ID3D11Texture2D), texture.put_void()));
        D3D11_TEXTURE2D_DESC description{};
        texture->GetDesc(&description);
        description.Usage = D3D11_USAGE_STAGING;
        description.BindFlags = 0;
        description.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
        description.MiscFlags = 0;
        com_ptr<ID3D11Texture2D> staging;
        check_hresult(device->CreateTexture2D(&description, nullptr, staging.put()));
        context->CopyResource(staging.get(), texture.get());
        D3D11_MAPPED_SUBRESOURCE pixels{};
        check_hresult(context->Map(staging.get(), 0, D3D11_MAP_READ, 0, &pixels));
        const auto matches = MatchesPattern(pixels, description.Width, description.Height, markers, info.rcMonitor, samples);
        context->Unmap(staging.get(), 0);
        ++frames;
        if (matches)
        {
            std::printf("WGC CreateForMonitor frame=%ux%u verified=%u samples=%06X,%06X,%06X matched_pixels=867\n",
                description.Width, description.Height, frames, samples[0], samples[1], samples[2]);
            return 0;
        }
    }

    std::printf("WGC CreateForMonitor no-pattern frames=%u samples=%06X,%06X,%06X\n",
        frames, samples[0], samples[1], samples[2]);
    return 1;
}

int main(int argc, char** argv)
{
    if (argc != 2)
        return 2;
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
    init_apartment(apartment_type::multi_threaded);
    const auto window = reinterpret_cast<HWND>(_strtoui64(argv[1], nullptr, 0));
    if (!IsWindow(window))
        return 2;
    try { return CaptureMonitor(window); }
    catch (const hresult_error& error)
    {
        std::printf("WGC CreateForMonitor HRESULT=%08x\n", static_cast<unsigned>(error.code().value));
        return 2;
    }
}
