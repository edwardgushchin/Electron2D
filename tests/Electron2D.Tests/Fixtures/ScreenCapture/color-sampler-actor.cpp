#include <windows.h>
#include <cstdint>
#include <cstdio>
#include <cstdlib>

namespace
{
    COLORREF color = RGB(0, 255, 0);
    unsigned clicks = 0;

    LRESULT CALLBACK ActorProc(HWND window, UINT message, WPARAM wparam, LPARAM lparam)
    {
        switch (message)
        {
        case WM_PAINT:
        {
            PAINTSTRUCT paint{};
            auto dc = BeginPaint(window, &paint);
            RECT area{};
            GetClientRect(window, &area);
            const auto brush = CreateSolidBrush(color);
            FillRect(dc, &area, brush);
            DeleteObject(brush);
            area.right = 70;
            SetDCBrushColor(dc, RGB(255, 0, 0));
            FillRect(dc, &area, static_cast<HBRUSH>(GetStockObject(DC_BRUSH)));
            EndPaint(window, &paint);
            return 0;
        }
        case WM_APP + 1:
            color = static_cast<COLORREF>(wparam);
            InvalidateRect(window, nullptr, FALSE);
            UpdateWindow(window);
            return 1;
        case WM_APP + 2:
            return clicks;
        case WM_LBUTTONDOWN: case WM_LBUTTONUP: case WM_RBUTTONDOWN: case WM_RBUTTONUP:
            ++clicks;
            return 0;
        case WM_DESTROY:
            PostQuitMessage(0);
            return 0;
        default:
            return DefWindowProcW(window, message, wparam, lparam);
        }
    }
}

int main(int argc, char** argv)
{
    if (argc != 3) return 2;
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
    WNDCLASSW type{};
    type.hInstance = GetModuleHandleW(nullptr);
    type.lpszClassName = L"Electron2DColorSamplerActor";
    type.lpfnWndProc = ActorProc;
    if (!RegisterClassW(&type)) return 2;

    const auto window = CreateWindowExW(WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE,
        type.lpszClassName, L"Color sampling target", WS_POPUP, std::atoi(argv[1]), std::atoi(argv[2]),
        240, 160, nullptr, nullptr, type.hInstance, nullptr);
    if (!window) return 2;
    ShowWindow(window, SW_SHOWNOACTIVATE);
    UpdateWindow(window);
    std::printf("%llu\n", static_cast<unsigned long long>(reinterpret_cast<std::uintptr_t>(window)));
    std::fflush(stdout);

    MSG message{};
    while (GetMessageW(&message, nullptr, 0, 0) > 0)
    {
        TranslateMessage(&message);
        DispatchMessageW(&message);
    }
    return 0;
}
