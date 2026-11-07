#include <windows.h>
#include <UIAutomation.h>
#include <wrl/client.h>
#include <cstdio>
#include <stdexcept>
#include <string>

using Microsoft::WRL::ComPtr;

struct Target
{
    DWORD pid = 0;
    const wchar_t* title;
    HWND dialog = nullptr;
};

static BOOL CALLBACK Locate(HWND window, LPARAM data)
{
    auto& target = *reinterpret_cast<Target*>(data);
    DWORD pid = 0;
    GetWindowThreadProcessId(window, &pid);
    if (pid != target.pid || !IsWindowVisible(window))
        return TRUE;
    wchar_t title[256]{}, kind[32]{};
    GetWindowTextW(window, title, 256);
    GetClassNameW(window, kind, 32);
    if (std::wstring_view(title) != target.title || std::wstring_view(kind) != L"#32770")
        return TRUE;
    target.dialog = window;
    return FALSE;
}

static void Require(bool condition, const char* detail)
{
    if (!condition)
        throw std::runtime_error(detail);
}

static void Check(HRESULT result)
{
    if (FAILED(result))
    {
        std::fprintf(stderr, "UI Automation HRESULT 0x%08lx\n", static_cast<unsigned long>(result));
        throw std::runtime_error("UI Automation failed");
    }
}

static std::wstring ReadControl(HWND control)
{
    DWORD_PTR length = 0, copied = 0;
    Require(control && SendMessageTimeoutW(control, WM_GETTEXTLENGTH, 0, 0, SMTO_ABORTIFHUNG, 3000, &length),
        "Could not measure owned control");
    Require(length < 200000, "Unexpected fixture text length");
    std::wstring text(length + 1, L'\0');
    Require(SendMessageTimeoutW(control, WM_GETTEXT, text.size(), reinterpret_cast<LPARAM>(text.data()),
        SMTO_ABORTIFHUNG, 3000, &copied) && copied == length, "Could not read owned control");
    text.resize(length);
    return text;
}

static void Invoke(IUIAutomation* automation, HWND control)
{
    ComPtr<IUIAutomationElement> element;
    Check(automation->ElementFromHandle(control, &element));
    ComPtr<IUIAutomationInvokePattern> action;
    Check(element->GetCurrentPatternAs(UIA_InvokePatternId, IID_PPV_ARGS(&action)));
    Check(action->Invoke());
}

int wmain(int argc, wchar_t** argv)
{
    if (argc != 10)
        return 2;
    Target target{wcstoul(argv[1], nullptr, 10), argv[2]};
    const std::wstring_view action = argv[3];
    const auto deadline = GetTickCount64() + 15000;
    try
    {
        while (GetTickCount64() < deadline)
        {
            EnumWindows(Locate, reinterpret_cast<LPARAM>(&target));
            if (target.dialog && (action == L"file-cancel" ||
                GetPropW(target.dialog, L"Electron2D.WindowsTextInputFixture.Ready")))
                break;
            Sleep(20);
        }
        const auto dialog = target.dialog;
        Require(dialog && IsWindow(dialog), "The exact owned dialog did not appear");
        const auto parent = GetWindow(dialog, GW_OWNER);
        Require(parent && !IsWindowEnabled(parent), "The native parent is not disabled by the modal dialog");
        if (action == L"file-cancel")
        {
            Require(PostMessageW(dialog, WM_CLOSE, 0, 0), "Could not close the owned file chooser");
            return 0;
        }
        Require(GetPropW(dialog, L"Electron2D.WindowsTextInputFixture.Ready") != nullptr, "Owner guards did not execute");

        const auto input = GetDlgItem(dialog, 100);
        std::wstring initial = argv[4];
        if (initial == L"@long-input")
            initial = std::wstring(70000, L'x') + L"\U0001f680";
        std::wstring description = argv[5];
        if (description == L"@long-description")
            description = L"<plain> & text\r\n" + std::wstring(4000, L'a') + L"\r\nlast line";
        Require(ReadControl(input) == initial, "Initial input differs");
        Require(description.empty() ? GetDlgItem(dialog, 101) == nullptr : ReadControl(GetDlgItem(dialog, 101)) == description,
            "Plain description differs");
        Require(ReadControl(GetDlgItem(dialog, 102)) == argv[7] &&
                ReadControl(GetDlgItem(dialog, IDOK)) == argv[8] &&
                ReadControl(GetDlgItem(dialog, IDCANCEL)) == argv[9], "Localized labels differ");
        GUITHREADINFO gui{sizeof(gui)};
        Require(GetGUIThreadInfo(GetWindowThreadProcessId(dialog, nullptr), &gui) && gui.hwndFocus == input,
            "Initial focus does not belong to the edit");
        if (initial.size() < 65536)
        {
            DWORD_PTR selection = 0;
            Require(SendMessageTimeoutW(input, EM_GETSEL, 0, 0, SMTO_ABORTIFHUNG, 3000, &selection) &&
                LOWORD(selection) == 0 && HIWORD(selection) == initial.size(), "Initial input is not selected");
        }

        Check(CoInitializeEx(nullptr, COINIT_MULTITHREADED));
        struct Apartment { ~Apartment() { CoUninitialize(); } } apartment;
        ComPtr<IUIAutomation> automation;
        Check(CoCreateInstance(CLSID_CUIAutomation, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&automation)));
        ComPtr<IUIAutomationElement> edit;
        Check(automation->ElementFromHandle(input, &edit));
        CONTROLTYPEID type;
        Check(edit->get_CurrentControlType(&type));
        Require(type == UIA_EditControlTypeId, "Input is not an accessible native edit");
        BSTR name = nullptr;
        Check(edit->get_CurrentName(&name));
        const auto labeled = name && std::wstring_view(name, SysStringLen(name)) == argv[7];
        SysFreeString(name);
        Require(labeled, "Accessible input name differs");
        ComPtr<IUIAutomationValuePattern> value;
        Check(edit->GetCurrentPatternAs(UIA_ValuePatternId, IID_PPV_ARGS(&value)));
        if (std::wstring_view(argv[6]) != L"@unchanged")
        {
            Check(value->SetValue(argv[6]));
            Require(ReadControl(input) == argv[6], "Actual native edit differs after the change");
        }

        if (action == L"enter" || action == L"escape")
        {
            const UINT key = action == L"enter" ? VK_RETURN : VK_ESCAPE;
            const auto scan = static_cast<LPARAM>(MapVirtualKeyW(key, MAPVK_VK_TO_VSC));
            Require(PostMessageW(input, WM_KEYDOWN, key, 1 | (scan << 16)), "Could not send the native dialog key");
        }
        else if (action == L"close")
            Require(PostMessageW(dialog, WM_CLOSE, 0, 0), "Could not close the owned text dialog");
        else
            Invoke(automation.Get(), GetDlgItem(dialog, action == L"cancel" ? IDCANCEL : IDOK));

        std::puts("Owned native edit, text, labels, focus and modality checked");
        return 0;
    }
    catch (const std::exception& error)
    {
        if (target.dialog)
            PostMessageW(target.dialog, WM_CLOSE, 0, 0);
        std::fprintf(stderr, "%s\n", error.what());
        return 1;
    }
}
