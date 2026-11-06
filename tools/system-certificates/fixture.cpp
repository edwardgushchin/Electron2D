#define UNICODE
#include <windows.h>
#include <wincrypt.h>
#include <cstdio>
#include <string>
#include <vector>

static std::wstring environment(const wchar_t* name) {
    wchar_t buffer[32768]{}; auto size = GetEnvironmentVariableW(name, buffer, 32768);
    return size && size < 32768 ? buffer : L"";
}
template<class T> static T native(const char* name) {
    static auto module = LoadLibraryExW(L"crypt32.dll", nullptr, LOAD_LIBRARY_SEARCH_SYSTEM32);
    return reinterpret_cast<T>(GetProcAddress(module, name));
}
static int outstanding = 0;
static bool mode(const wchar_t* value) { return environment(L"ELECTRON2D_CA_FIXTURE_MODE") == value; }
static void receipt() {
    FILE* file = nullptr; auto path = environment(L"ELECTRON2D_CA_FIXTURE_DIRECTORY") + L"/closed.txt";
    _wfopen_s(&file, path.c_str(), L"wb"); if (file) { fprintf(file, "outstanding=%d\n", outstanding); fclose(file); }
}
extern "C" HCERTSTORE WINAPI FixtureOpenStore(LPCSTR provider, DWORD encoding, HCRYPTPROV_LEGACY cryptography, DWORD flags, const void* parameter) {
    if (provider != CERT_STORE_PROV_SYSTEM_W || flags != (CERT_SYSTEM_STORE_CURRENT_USER | CERT_STORE_READONLY_FLAG | CERT_STORE_OPEN_EXISTING_FLAG) || wcscmp(static_cast<const wchar_t*>(parameter), L"ROOT")) { SetLastError(ERROR_INVALID_PARAMETER); return nullptr; }
    if (mode(L"open-error")) { receipt(); SetLastError(ERROR_ACCESS_DENIED); return nullptr; }
    auto store = native<decltype(&CertOpenStore)>("CertOpenStore")(CERT_STORE_PROV_MEMORY, 0, 0, 0, nullptr);
    if (!store || mode(L"empty")) return store;
    auto fail = [store]() { native<decltype(&CertCloseStore)>("CertCloseStore")(store, 0); return static_cast<HCERTSTORE>(nullptr); };
    auto directory = environment(L"ELECTRON2D_CA_FIXTURE_DIRECTORY");
    for (int index = 0; index < 3; index++) {
        FILE* file = nullptr; auto path = directory + L"/" + std::to_wstring(index) + L".der";
        _wfopen_s(&file, path.c_str(), L"rb"); if (!file) return fail();
        fseek(file, 0, SEEK_END); auto length = ftell(file); rewind(file);
        if (length <= 0) { fclose(file); return fail(); }
        std::vector<BYTE> bytes(length); auto read = fread(bytes.data(), 1, bytes.size(), file); fclose(file);
        if (read != bytes.size()) return fail();
        PCCERT_CONTEXT context = nullptr;
        if (!native<decltype(&CertAddEncodedCertificateToStore)>("CertAddEncodedCertificateToStore")(store, X509_ASN_ENCODING, bytes.data(), static_cast<DWORD>(bytes.size()), CERT_STORE_ADD_ALWAYS, &context)) return fail();
        if (index < 2) {
            FILETIME value; GetSystemTimeAsFileTime(&value); ULARGE_INTEGER time{};
            time.LowPart = value.dwLowDateTime; time.HighPart = value.dwHighDateTime;
            time.QuadPart += index == 0 ? -10000000LL : 864000000000LL;
            value.dwLowDateTime = time.LowPart; value.dwHighDateTime = time.HighPart;
            CRYPT_DATA_BLOB property{sizeof(value), reinterpret_cast<BYTE*>(&value)};
            if (index == 0 && mode(L"zero-time")) property = {0, nullptr};
            if (!native<decltype(&CertSetCertificateContextProperty)>("CertSetCertificateContextProperty")(context, CERT_DISALLOWED_FILETIME_PROP_ID, 0, &property)) { native<decltype(&CertFreeCertificateContext)>("CertFreeCertificateContext")(context); return fail(); }
        }
        native<decltype(&CertFreeCertificateContext)>("CertFreeCertificateContext")(context);
    }
    return store;
}
extern "C" PCCERT_CONTEXT WINAPI FixtureEnumerate(HCERTSTORE store, PCCERT_CONTEXT previous) {
    if (previous) outstanding--;
    if (previous && mode(L"enumerate-error")) {
        native<decltype(&CertFreeCertificateContext)>("CertFreeCertificateContext")(previous);
        SetLastError(ERROR_READ_FAULT); return nullptr;
    }
    auto result = native<decltype(&CertEnumCertificatesInStore)>("CertEnumCertificatesInStore")(store, previous);
    if (result) outstanding++;
    return result;
}
extern "C" BOOL WINAPI FixtureProperty(PCCERT_CONTEXT context, DWORD property, void* data, DWORD* size) {
    if (mode(L"property-error")) { SetLastError(ERROR_ACCESS_DENIED); return FALSE; }
    if (mode(L"bad-time")) { *size = 4; return TRUE; }
    return native<decltype(&CertGetCertificateContextProperty)>("CertGetCertificateContextProperty")(context, property, data, size);
}
extern "C" BOOL WINAPI FixtureFree(PCCERT_CONTEXT context) {
    if (context) outstanding--;
    return native<decltype(&CertFreeCertificateContext)>("CertFreeCertificateContext")(context);
}
extern "C" BOOL WINAPI FixtureClose(HCERTSTORE store, DWORD flags) {
    auto result = native<decltype(&CertCloseStore)>("CertCloseStore")(store, CERT_CLOSE_STORE_CHECK_FLAG);
    if (!result) outstanding = -999;
    receipt(); return result;
}
