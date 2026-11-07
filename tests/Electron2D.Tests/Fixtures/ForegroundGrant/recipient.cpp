#include <windows.h>
#include <cstdint>
#include <iostream>
#include <string>

int main(int argc, char** argv)
{
    if (argc != 3) return 2;
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

    const auto window = CreateWindowExW(WS_EX_TOOLWINDOW | WS_EX_TOPMOST, L"STATIC",
        L"Electron2D foreground recipient", WS_OVERLAPPEDWINDOW, std::stoi(argv[1]), std::stoi(argv[2]),
        280, 160, nullptr, nullptr, GetModuleHandleW(nullptr), nullptr);
    if (!window) return 2;
    ShowWindow(window, SW_SHOWNOACTIVATE);
    std::cout << reinterpret_cast<std::uintptr_t>(window) << std::endl;

    const auto commands = GetStdHandle(STD_INPUT_HANDLE);
    bool running = true;
    while (running && IsWindow(window))
    {
        MSG message{};
        while (PeekMessageW(&message, nullptr, 0, 0, PM_REMOVE))
        {
            if (message.message == WM_QUIT) { running = false; break; }
            TranslateMessage(&message);
            DispatchMessageW(&message);
        }

        DWORD available = 0;
        if (!PeekNamedPipe(commands, nullptr, 0, nullptr, &available, nullptr)) break;
        if (available != 0)
        {
            std::string command;
            if (!std::getline(std::cin, command) || command != "activate") break;
            std::cout << (SetForegroundWindow(window) != FALSE ? 1 : 0) << std::endl;
        }
        else
            MsgWaitForMultipleObjects(0, nullptr, FALSE, 10, QS_ALLINPUT);
    }

    DestroyWindow(window);
    return 0;
}
