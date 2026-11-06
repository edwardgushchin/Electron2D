#define UNICODE
#include <windows.h>
#include <wincrypt.h>
#include <cstdio>
#include <string>

int wmain(int argc, wchar_t** argv) {
    if (argc != 2) return 2;
    auto store = CertOpenStore(CERT_STORE_PROV_SYSTEM_W, 0, 0,
        CERT_SYSTEM_STORE_CURRENT_USER | CERT_STORE_READONLY_FLAG | CERT_STORE_OPEN_EXISTING_FLAG, L"ROOT");
    if (!store) return 3;
    FILETIME now; GetSystemTimeAsFileTime(&now);
    PCCERT_CONTEXT current = nullptr; int count = 0, excluded = 0;
    while ((current = CertEnumCertificatesInStore(store, current))) {
        FILETIME disallowed{}; DWORD size = sizeof(disallowed);
        if (CertGetCertificateContextProperty(current, CERT_DISALLOWED_FILETIME_PROP_ID, &disallowed, &size)) {
            if (size == 0) { excluded++; continue; }
            if (size != sizeof(disallowed)) return 8;
            if (CompareFileTime(&now, &disallowed) >= 0) { excluded++; continue; }
        } else if (GetLastError() != CRYPT_E_NOT_FOUND) return 4;
        auto path = std::wstring(argv[1]) + L"/" + std::to_wstring(count++) + L".der";
        FILE* file = nullptr; _wfopen_s(&file, path.c_str(), L"wb"); if (!file) return 5;
        auto written = fwrite(current->pbCertEncoded, 1, current->cbCertEncoded, file); fclose(file);
        if (written != current->cbCertEncoded) return 6;
    }
    auto error = GetLastError(); CertCloseStore(store, CERT_CLOSE_STORE_CHECK_FLAG);
    if (error != CRYPT_E_NOT_FOUND) return 7;
    printf("SDK roots=%d; disallowed=%d\n", count, excluded); return 0;
}
