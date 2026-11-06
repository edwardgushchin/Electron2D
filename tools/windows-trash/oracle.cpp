// Independent Windows Shell/metadata oracle. Only entries with the exact supplied original path are inspected/restored.
#include <windows.h>
#include <winioctl.h>
#include <shobjidl.h>
#include <shlobj.h>
#include <shlguid.h>
#include <knownfolders.h>
#include <wrl/client.h>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <vector>
using Microsoft::WRL::ComPtr;
namespace fs = std::filesystem;
static fs::path canonical(std::wstring value) {
    if (value.substr(0, 8) == L"\\\\?\\UNC\\") value = L"\\\\" + value.substr(8);
    else if (value.substr(0, 4) == L"\\\\?\\") value = value.substr(4);
    auto result = fs::absolute(value).lexically_normal(); return result.make_preferred();
}
class RecycleGuard final : public IFileOperationProgressSink {
    LONG refs = 1;
public:
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, void **value) override { *value = nullptr; if (id != IID_IUnknown && id != __uuidof(IFileOperationProgressSink)) return E_NOINTERFACE; *value = this; AddRef(); return S_OK; }
    ULONG STDMETHODCALLTYPE AddRef() override { return InterlockedIncrement(&refs); }
    ULONG STDMETHODCALLTYPE Release() override { LONG count = InterlockedDecrement(&refs); if (!count) delete this; return count; }
#define SINK_OK(name, ...) HRESULT STDMETHODCALLTYPE name(__VA_ARGS__) override { return S_OK; }
    SINK_OK(StartOperations)
    SINK_OK(FinishOperations, HRESULT)
    SINK_OK(PreRenameItem, DWORD, IShellItem *, LPCWSTR)
    SINK_OK(PostRenameItem, DWORD, IShellItem *, LPCWSTR, HRESULT, IShellItem *)
    SINK_OK(PreMoveItem, DWORD, IShellItem *, IShellItem *, LPCWSTR)
    SINK_OK(PostMoveItem, DWORD, IShellItem *, IShellItem *, LPCWSTR, HRESULT, IShellItem *)
    SINK_OK(PreCopyItem, DWORD, IShellItem *, IShellItem *, LPCWSTR)
    SINK_OK(PostCopyItem, DWORD, IShellItem *, IShellItem *, LPCWSTR, HRESULT, IShellItem *)
    HRESULT STDMETHODCALLTYPE PreDeleteItem(DWORD flags, IShellItem *) override { std::cout << "flags=" << std::hex << flags << '\n'; return (flags & TSF_DELETE_RECYCLE_IF_POSSIBLE) ? S_OK : E_ABORT; }
    HRESULT STDMETHODCALLTYPE PostDeleteItem(DWORD, IShellItem *, HRESULT result, IShellItem *created) override { std::cout << "post=" << std::hex << result << ",recycled=" << (created != nullptr) << '\n'; return S_OK; }
    SINK_OK(PreNewItem, DWORD, IShellItem *, LPCWSTR)
    SINK_OK(PostNewItem, DWORD, IShellItem *, LPCWSTR, LPCWSTR, DWORD, HRESULT, IShellItem *)
    SINK_OK(UpdateProgress, UINT, UINT)
    SINK_OK(ResetTimer)
    SINK_OK(PauseTimer)
    SINK_OK(ResumeTimer)
#undef SINK_OK
};
static void check(HRESULT result) { if (FAILED(result)) { std::cerr << "Shell oracle HRESULT " << std::hex << result << '\n'; exit(1); } }
static std::string utf8(const std::wstring &value) {
    int size = WideCharToMultiByte(CP_UTF8, 0, value.data(), (int)value.size(), nullptr, 0, nullptr, nullptr);
    std::string text(size, '\0'); WideCharToMultiByte(CP_UTF8, 0, value.data(), (int)value.size(), text.data(), size, nullptr, nullptr); return text;
}
static std::wstring original(const fs::path &payload) {
    auto leaf = payload.filename().wstring(); if (leaf.size() < 3 || leaf.substr(0, 2) != L"$R") return {};
    leaf[1] = L'I'; std::ifstream stream(payload.parent_path() / leaf, std::ios::binary);
    std::vector<char> bytes((std::istreambuf_iterator<char>(stream)), {}); if (bytes.size() < 28) return {};
    uint64_t version; memcpy(&version, bytes.data(), sizeof(version)); size_t offset = version == 1 ? 24 : version == 2 ? 28 : 0;
    if (!offset) return {}; size_t count = (bytes.size() - offset) / sizeof(wchar_t);
    auto value = reinterpret_cast<const wchar_t *>(bytes.data() + offset); size_t length = 0; while (length < count && value[length]) length++;
    return std::wstring(value, length);
}
int wmain(int argc, wchar_t **argv) {
    if (argc == 3 && !wcscmp(argv[1], L"guard")) {
        check(CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED));
        {
            fs::path source = fs::absolute(argv[2]); source.make_preferred(); ComPtr<IShellItem> item; check(SHCreateItemFromParsingName(source.c_str(), nullptr, IID_PPV_ARGS(&item)));
            ComPtr<IFileOperation> operation; check(CoCreateInstance(CLSID_FileOperation, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&operation)));
            ComPtr<IFileOperationProgressSink> sink; sink.Attach(new RecycleGuard());
            check(operation->SetOperationFlags(FOF_NO_UI | FOF_NO_CONNECTED_ELEMENTS | FOFX_RECYCLEONDELETE | FOFX_ADDUNDORECORD | FOFX_EARLYFAILURE));
            check(operation->DeleteItem(item.Get(), sink.Get())); HRESULT result = operation->PerformOperations(); std::cout << "result=" << std::hex << result << '\n';
        }
        CoUninitialize(); return 0;
    }
    if (argc == 4 && !wcscmp(argv[1], L"junction")) {
        fs::path destination = fs::absolute(argv[3]); destination.make_preferred();
        std::wstring substitute = L"\\??\\" + destination.wstring(), display = destination.wstring();
        struct MountPoint { DWORD tag; WORD length, reserved, substituteOffset, substituteLength, printOffset, printLength; wchar_t path[1]; };
        std::vector<BYTE> storage(16 + (substitute.size() + display.size() + 2) * sizeof(wchar_t)); auto data = reinterpret_cast<MountPoint *>(storage.data());
        data->tag = IO_REPARSE_TAG_MOUNT_POINT; data->length = (WORD)(storage.size() - 8); data->substituteLength = (WORD)(substitute.size() * 2); data->printOffset = data->substituteLength + 2; data->printLength = (WORD)(display.size() * 2);
        memcpy(data->path, substitute.c_str(), (substitute.size() + 1) * 2); memcpy((BYTE *)data->path + data->printOffset, display.c_str(), (display.size() + 1) * 2);
        if (!CreateDirectoryW(argv[2], nullptr)) check(HRESULT_FROM_WIN32(GetLastError()));
        HANDLE directory = CreateFileW(argv[2], GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAG_BACKUP_SEMANTICS, nullptr);
        if (directory == INVALID_HANDLE_VALUE) check(HRESULT_FROM_WIN32(GetLastError()));
        DWORD written; BOOL result = DeviceIoControl(directory, FSCTL_SET_REPARSE_POINT, data, (DWORD)storage.size(), nullptr, 0, &written, nullptr); DWORD error = GetLastError(); CloseHandle(directory); if (!result) check(HRESULT_FROM_WIN32(error)); return 0;
    }
    if (argc < 3 || argc > 4 || (wcscmp(argv[1], L"inspect") && wcscmp(argv[1], L"restore"))) return 2;
    check(CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED));
    fs::path expected = canonical(argv[2]);
    bool restored = false;
    {
        ComPtr<IShellItem> bin; check(SHGetKnownFolderItem(FOLDERID_RecycleBinFolder, KF_FLAG_DEFAULT, nullptr, IID_PPV_ARGS(&bin)));
        ComPtr<IEnumShellItems> items; check(bin->BindToHandler(nullptr, BHID_EnumItems, IID_PPV_ARGS(&items)));
        ComPtr<IShellItem> item;
        while (items->Next(1, item.ReleaseAndGetAddressOf(), nullptr) == S_OK) {
            PWSTR value = nullptr; if (FAILED(item->GetDisplayName(SIGDN_FILESYSPATH, &value))) continue;
            fs::path payload(value); CoTaskMemFree(value); auto source = original(payload);
            if (source.empty() || CompareStringOrdinal(canonical(source).c_str(), -1, expected.c_str(), -1, TRUE) != CSTR_EQUAL || (argc == 4 && payload != fs::path(argv[3]))) continue;
            if (!wcscmp(argv[1], L"restore")) {
                ComPtr<IContextMenu> menu; check(item->BindToHandler(nullptr, BHID_SFUIObject, IID_PPV_ARGS(&menu)));
                HMENU commands = CreatePopupMenu(); check(menu->QueryContextMenu(commands, 0, 1, 0x7FFF, CMF_NORMAL));
                CMINVOKECOMMANDINFO invoke = {sizeof(invoke)}; invoke.fMask = CMIC_MASK_FLAG_NO_UI | CMIC_MASK_NOASYNC; invoke.lpVerb = "undelete"; invoke.nShow = SW_HIDE;
                HRESULT result = menu->InvokeCommand(&invoke); DestroyMenu(commands); check(result);
                restored = true; std::cout << utf8(payload.wstring()) << '\n'; break;
            }
            std::cout << utf8(payload.wstring()) << '\n';
        }
    }
    CoUninitialize(); return !wcscmp(argv[1], L"restore") && !restored ? 4 : 0;
}
