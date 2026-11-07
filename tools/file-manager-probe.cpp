#include <windows.h>
#include <shlobj.h>
#include <shobjidl.h>
#include <shlguid.h>
#include <exdisp.h>
#include <wrl/client.h>
#include <cstdio>
#include <cwchar>
#include <set>
#include <string>
#include <algorithm>
#include <vector>
using Microsoft::WRL::ComPtr;

static bool Same(IShellItem* first, IShellItem* second) {
    int order=1; return SUCCEEDED(first->Compare(second,SICHINT_CANONICAL,&order)) && order==0;
}
static ComPtr<IFolderView2> View(IDispatch* window, ComPtr<IShellBrowser>& browser) {
    ComPtr<IServiceProvider> service; ComPtr<IShellView> shell; ComPtr<IFolderView2> view;
    if (FAILED(window->QueryInterface(IID_PPV_ARGS(&service))) ||
        FAILED(service->QueryService(SID_STopLevelBrowser,IID_PPV_ARGS(&browser))) ||
        FAILED(browser->QueryActiveShellView(&shell))) return view;
    shell.As(&view); return view;
}
int wmain(int argc,wchar_t** argv) {
    if (argc<2) return 2;
    for(int i=2;i<argc;i++)std::replace(argv[i],argv[i]+std::wcslen(argv[i]),L'/',L'\\');
    auto initialized=CoInitializeEx(nullptr,COINIT_APARTMENTTHREADED|COINIT_DISABLE_OLE1DDE);
    if (FAILED(initialized)) return 1;
    struct Apartment { ~Apartment(){CoUninitialize();} } apartment;
    ComPtr<IShellWindows> windows;
    auto activation=CoCreateInstance(CLSID_ShellWindows,nullptr,CLSCTX_LOCAL_SERVER,IID_PPV_ARGS(&windows));
    if (FAILED(activation)) { std::printf("ShellWindows HRESULT=%08lx\n",(unsigned long)activation);return 1; }
    ComPtr<IShellItem> expectedFolder, expectedItem;
    PIDLIST_ABSOLUTE ownedRoot=nullptr;
    if (argc>2) { auto result=SHCreateItemFromParsingName(argv[2],nullptr,IID_PPV_ARGS(&expectedFolder));if(FAILED(result)){std::printf("folder HRESULT=%08lx\n",(unsigned long)result);return 2;} }
    if (argc>3 && std::wcscmp(argv[1],L"close")!=0 && std::wcscmp(argv[3],L"-")!=0 && FAILED(SHCreateItemFromParsingName(argv[3],nullptr,IID_PPV_ARGS(&expectedItem)))) return 2;
    if (argc>2 && std::wcscmp(argv[1],L"close")==0 && FAILED(SHParseDisplayName(argv[2],nullptr,&ownedRoot,0,nullptr))) return 2;
    std::set<HWND> original;
    for(int i=3;i<argc&&std::wcscmp(argv[1],L"close")==0;i++)original.insert(reinterpret_cast<HWND>(std::wcstoull(argv[i],nullptr,10)));
    bool done=false;
    auto deadline=GetTickCount64()+8000;
    do {
        std::vector<ComPtr<IWebBrowser2>> retiring;
        long count=0;windows->get_Count(&count);
        for(long i=0;i<count;i++) {
            VARIANT index{};index.vt=VT_I4;index.lVal=i;
            ComPtr<IDispatch> dispatch;if(FAILED(windows->Item(index,&dispatch))||!dispatch)continue;
            ComPtr<IShellBrowser> browser;auto view=View(dispatch.Get(),browser);if(!view)continue;
            HWND handle=nullptr;browser->GetWindow(&handle);
            if(std::wcscmp(argv[1],L"baseline")==0){std::printf("%llu\n",(unsigned long long)handle);done=true;continue;}
            ComPtr<IPersistFolder2> folder;if(FAILED(view->GetFolder(IID_PPV_ARGS(&folder))))continue;
            PIDLIST_ABSOLUTE location=nullptr;if(FAILED(folder->GetCurFolder(&location)))continue;
            ComPtr<IShellItem> actualFolder;SHCreateItemFromIDList(location,IID_PPV_ARGS(&actualFolder));
            bool owned=ownedRoot&&(ILIsEqual(ownedRoot,location)||ILIsParent(ownedRoot,location,FALSE));CoTaskMemFree(location);
            if(std::wcscmp(argv[1],L"close")==0){
                if(owned&&!original.contains(handle)){ComPtr<IWebBrowser2> web;if(SUCCEEDED(dispatch.As(&web)))retiring.push_back(web);}
                done=true;continue;
            }
            if(!actualFolder||!Same(actualFolder.Get(),expectedFolder.Get()))continue;
            int selectedCount=0;if(FAILED(view->ItemCount(SVGIO_SELECTION,&selectedCount)))continue;
            if((expectedItem&&selectedCount!=1)||(!expectedItem&&selectedCount!=0))continue;
            if(expectedItem){ComPtr<IShellItemArray> selected;DWORD arrayCount=0;ComPtr<IShellItem> item;
                if(FAILED(view->GetSelection(FALSE,&selected))||!selected||FAILED(selected->GetCount(&arrayCount))||arrayCount!=1||FAILED(selected->GetItemAt(0,&item))||!Same(item.Get(),expectedItem.Get()))continue;}
            std::printf("matched=1 selection=%d\n",selectedCount);done=true;break;
        }
        if(std::wcscmp(argv[1],L"baseline")==0){done=true;break;}
        if(std::wcscmp(argv[1],L"close")==0){if(retiring.empty()){done=true;break;}for(auto& web:retiring)web->Quit();done=false;}
        if(!done)Sleep(30);
    }while(!done&&GetTickCount64()<deadline);
    CoTaskMemFree(ownedRoot);
    if(!done)std::printf("owned folder/selection not observed\n");
    return done?0:1;
}
