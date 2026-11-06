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
#include <cstring>
using namespace winrt;
using namespace winrt::Windows::Graphics::Capture;
using namespace winrt::Windows::Graphics::DirectX;
using namespace winrt::Windows::Graphics::DirectX::Direct3D11;
static LRESULT CALLBACK WndProc(HWND w, UINT m, WPARAM p, LPARAM l) {
    if (m == WM_PAINT) { PAINTSTRUCT ps; auto dc=BeginPaint(w,&ps); RECT r; GetClientRect(w,&r); auto b=CreateSolidBrush(RGB(255,0,0)); FillRect(dc,&r,b); DeleteObject(b); EndPaint(w,&ps); return 0; }
    return DefWindowProcW(w,m,p,l);
}
static void pump() { MSG m; while(PeekMessageW(&m,nullptr,0,0,PM_REMOVE)) { TranslateMessage(&m); DispatchMessageW(&m); } }
static int capture(HWND window, const char* name) {
    try {
        auto factory=get_activation_factory<GraphicsCaptureItem,IGraphicsCaptureItemInterop>();
        GraphicsCaptureItem item{nullptr}; check_hresult(factory->CreateForWindow(window,guid_of<GraphicsCaptureItem>(),put_abi(item)));
        com_ptr<ID3D11Device> device; com_ptr<ID3D11DeviceContext> context;
        check_hresult(D3D11CreateDevice(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,D3D11_CREATE_DEVICE_BGRA_SUPPORT,nullptr,0,D3D11_SDK_VERSION,device.put(),nullptr,context.put()));
        auto dxgi=device.as<IDXGIDevice>(); com_ptr<IInspectable> inspectable;
        check_hresult(CreateDirect3D11DeviceFromDXGIDevice(dxgi.get(),inspectable.put()));
        auto pool=Direct3D11CaptureFramePool::CreateFreeThreaded(inspectable.as<IDirect3DDevice>(),DirectXPixelFormat::B8G8R8A8UIntNormalized,2,item.Size());
        auto session=pool.CreateCaptureSession(item); session.IsCursorCaptureEnabled(false); session.StartCapture();
        auto deadline=GetTickCount64()+2500; int result=0;
        while(GetTickCount64()<deadline) {
            pump(); auto frame=pool.TryGetNextFrame();
            if(frame) {
                auto access=frame.Surface().as<::Windows::Graphics::DirectX::Direct3D11::IDirect3DDxgiInterfaceAccess>();
                com_ptr<ID3D11Texture2D> texture; check_hresult(access->GetInterface(__uuidof(ID3D11Texture2D),texture.put_void()));
                D3D11_TEXTURE2D_DESC desc; texture->GetDesc(&desc); desc.Usage=D3D11_USAGE_STAGING; desc.BindFlags=0; desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ; desc.MiscFlags=0;
                com_ptr<ID3D11Texture2D> staging; check_hresult(device->CreateTexture2D(&desc,nullptr,staging.put())); context->CopyResource(staging.get(),texture.get());
                D3D11_MAPPED_SUBRESOURCE map; check_hresult(context->Map(staging.get(),0,D3D11_MAP_READ,0,&map));
                unsigned long long pixels=0,opaque=0,marked=0;
                for(unsigned y=0;y<desc.Height;y++) { auto row=(unsigned char*)map.pData+y*map.RowPitch; for(unsigned x=0;x<desc.Width;x++) { auto px=row+4*x; pixels++; if(px[3])opaque++; if(px[0]==0&&px[1]==0&&px[2]>=250)marked++; } }
                context->Unmap(staging.get(),0); std::printf("%s frame=%ux%u pixels=%llu opaque=%llu marked=%llu\n",name,desc.Width,desc.Height,pixels,opaque,marked); result=marked?1:2; break;
            }
            Sleep(10);
        }
        session.Close(); pool.Close(); if(!result)std::printf("%s no-frame\n",name); return result;
    } catch(hresult_error const& e) { std::printf("%s HRESULT=%08x %ls\n",name,(unsigned)e.code().value,e.message().c_str()); return e.code().value==E_ACCESSDENIED?-2:-1; }
}
int main(int argc,char** argv) {
    init_apartment(apartment_type::multi_threaded);
    HWND window=nullptr;
    if(argc>1) window=(HWND)_strtoui64(argv[1],nullptr,0);
    else {
        WNDCLASSW wc{}; wc.lpfnWndProc=WndProc; wc.hInstance=GetModuleHandleW(nullptr); wc.lpszClassName=L"E2DCaptureProbe"; RegisterClassW(&wc);
        window=CreateWindowW(wc.lpszClassName,L"Electron2D owned capture probe",WS_OVERLAPPEDWINDOW|WS_VISIBLE,200,200,400,300,nullptr,nullptr,wc.hInstance,nullptr); UpdateWindow(window); pump(); Sleep(300);
    }
    if(argc>1) { auto result=capture(window,"external"); bool excluded=argc>2&&std::strcmp(argv[2],"excluded")==0; return excluded?(result==0||result==-2?0:1):(result==1?0:1); }
    int baseline=capture(window,"baseline"); SetLastError(0); auto set=SetWindowDisplayAffinity(window,0x11); auto error=GetLastError(); DWORD affinity=0; GetWindowDisplayAffinity(window,&affinity); std::printf("set=%d error=%lu affinity=%lu\n",set,error,affinity);
    int excluded=capture(window,"excluded"); SetWindowDisplayAffinity(window,0); int restored=capture(window,"restored"); DestroyWindow(window);
    return baseline==1&&set&&affinity==0x11&&(excluded==0||excluded==-2)&&restored==1?0:1;
}
