#define UNICODE
#include <windows.h>
#include <sapi.h>
#include <wrl/client.h>
#include <cstdio>
#include <string>

using Microsoft::WRL::ComPtr;
static std::string utf8(const wchar_t* text) {
    if (!text) return {};
    int size = WideCharToMultiByte(CP_UTF8, 0, text, -1, nullptr, 0, nullptr, nullptr);
    std::string result(size, 0); WideCharToMultiByte(CP_UTF8, 0, text, -1, result.data(), size, nullptr, nullptr); result.pop_back(); return result;
}
int wmain(int argc, wchar_t** argv) {
    if (argc != 2) return 2;
    if (FAILED(CoInitializeEx(nullptr, COINIT_MULTITHREADED))) return 3;
    FILE* output = nullptr; _wfopen_s(&output, argv[1], L"wb"); if (!output) return 4;
    ComPtr<ISpObjectTokenCategory> category;
    auto hr = CoCreateInstance(CLSID_SpObjectTokenCategory, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&category));
    if (FAILED(hr) || FAILED(category->SetId(SPCAT_VOICES, FALSE))) return 5;
    ComPtr<IEnumSpObjectTokens> enumeration; if (FAILED(category->EnumTokens(nullptr, nullptr, &enumeration))) return 6;
    ComPtr<ISpObjectToken> token; ULONG fetched = 0;
    while (enumeration->Next(1, &token, &fetched) == S_OK && fetched == 1) {
        wchar_t *id = nullptr, *name = nullptr, *language = nullptr;
        ComPtr<ISpDataKey> attributes;
        if (FAILED(token->GetId(&id)) || FAILED(token->GetStringValue(nullptr, &name)) || FAILED(token->OpenKey(L"Attributes", &attributes)) || FAILED(attributes->GetStringValue(L"Language", &language))) return 7;
        auto locale = wcstoul(language, nullptr, 16); wchar_t iso[85]{}, region[85]{};
        GetLocaleInfoW(locale, LOCALE_SISO639LANGNAME, iso, 85); GetLocaleInfoW(locale, LOCALE_SISO3166CTRYNAME, region, 85);
        fprintf(output, "%s|%s|%s_%s\n", utf8(id).c_str(), utf8(name).c_str(), utf8(iso).c_str(), utf8(region).c_str());
        CoTaskMemFree(id); CoTaskMemFree(name); CoTaskMemFree(language); token.Reset();
    }
    fclose(output); enumeration.Reset(); category.Reset(); CoUninitialize(); return 0;
}
