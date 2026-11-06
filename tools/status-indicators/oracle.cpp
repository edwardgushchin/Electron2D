#define UNICODE
#include <windows.h>
#include <shellapi.h>
#include <UIAutomation.h>
#include <oleacc.h>
#include <cstdio>
#include <string>
#include <vector>
struct Search {DWORD pid;HWND window;std::vector<HWND> menus;};
static BOOL CALLBACK findWindow(HWND w,LPARAM arg){auto &s=*reinterpret_cast<Search*>(arg);DWORD pid=0;GetWindowThreadProcessId(w,&pid);if(pid!=s.pid)return TRUE;wchar_t title[256]{},kind[64]{};GetWindowTextW(w,title,256);GetClassNameW(w,kind,64);if(wcscmp(title,L"Status indicator qualification")==0)s.window=w;if(IsWindowVisible(w)&&wcscmp(kind,L"#32768")==0)s.menus.push_back(w);return TRUE;}
static std::string utf8(const wchar_t *s){auto n=WideCharToMultiByte(CP_UTF8,0,s,-1,0,0,0,0);std::string out(n,0);WideCharToMultiByte(CP_UTF8,0,s,-1,out.data(),n,0,0);out.pop_back();return out;}
static int selectMenu(DWORD pid,HWND owner,FILE *out,bool activate){Search search{pid,0,{}};for(int n=0;n<300;n++){search.menus.clear();EnumWindows(findWindow,reinterpret_cast<LPARAM>(&search));if(!search.menus.empty())break;Sleep(10);}if(search.menus.empty())return 20;Sleep(150);auto window=search.menus.back(); GUITHREADINFO gui{sizeof(gui)};GetGUIThreadInfo(GetWindowThreadProcessId(window,0),&gui);fprintf(out,"menu owner=%p foreground=%p active=%p focus=%p\n",gui.hwndMenuOwner,GetForegroundWindow(),gui.hwndActive,gui.hwndFocus);auto menu=reinterpret_cast<HMENU>(SendMessageW(window,0x1e1,0,0));wchar_t label[256]{};GetMenuStringW(menu,0,label,256,MF_BYPOSITION);fprintf(out,"native_menu=%s\n",utf8(label).c_str());if(!activate){PostMessageW(owner,WM_CANCELMODE,0,0);fprintf(out,"native cancellation\n");return 0;}RECT item{};GetMenuItemRect(0,menu,0,&item);POINT point{(item.left+item.right)/2,(item.top+item.bottom)/2};ScreenToClient(window,&point);PostMessageW(window,WM_MOUSEMOVE,0,MAKELPARAM(point.x,point.y));Sleep(100);IAccessible *accessible=0;auto hr=AccessibleObjectFromWindow(window,OBJID_CLIENT,IID_IAccessible,reinterpret_cast<void**>(&accessible));if(FAILED(hr)){PostMessageW(owner,WM_CANCELMODE,0,0);return 21;}long count=0;accessible->get_accChildCount(&count);VARIANT self{};self.vt=VT_I4;self.lVal=0;VARIANT role{};accessible->get_accRole(self,&role);fprintf(out,"accessible children=%ld role=%ld\n",count,role.lVal);VARIANT child{};child.vt=VT_I4;child.lVal=1;hr=accessible->accDoDefaultAction(child);fprintf(out,"native invoke=%lx\n",hr);accessible->Release();if(FAILED(hr))PostMessageW(owner,WM_CANCELMODE,0,0);return SUCCEEDED(hr)?0:22;}
int wmain(int argc,wchar_t **argv){SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);if(argc!=6)return 2;CoInitializeEx(0,COINIT_APARTMENTTHREADED);static_assert(sizeof(NOTIFYICONDATAW)==976&&sizeof(NOTIFYICONIDENTIFIER)==40,"Windows x64 Shell ABI");Search search{wcstoul(argv[1],0,10),0,{}};EnumWindows(findWindow,reinterpret_cast<LPARAM>(&search));if(!search.window)return 3;std::wstring stem=argv[4],action=argv[3];FILE *out=0;_wfopen_s(&out,(stem+L".txt").c_str(),L"wb");if(!out)return 4;
NOTIFYICONIDENTIFIER id{};id.cbSize=sizeof(id);id.hWnd=search.window;id.uID=wcstoul(argv[2],0,10);RECT rect{};auto hr=Shell_NotifyIconGetRect(&id,&rect);fprintf(out,"ABI %zu %zu owner %p rect %lx %ld,%ld,%ld,%ld\n",sizeof(NOTIFYICONDATAW),sizeof(id),search.window,hr,rect.left,rect.top,rect.right,rect.bottom);fflush(out);
if(action==L"absent"){fclose(out);return FAILED(hr)?0:5;}
if(action==L"delete"||action==L"recover"){NOTIFYICONDATAW data{};data.cbSize=sizeof(data);data.hWnd=search.window;data.uID=id.uID;auto removed=Shell_NotifyIconW(NIM_DELETE,&data);fprintf(out,"external_remove=%d\n",removed);if(action==L"recover")SendMessageW(search.window,RegisterWindowMessageW(L"TaskbarCreated"),0,0);fclose(out);return removed||action==L"recover"?0:6;}
if(action.rfind(L"post:",0)==0){UINT button=wcstoul(action.c_str()+5,0,16);GetCursorPos(reinterpret_cast<POINT*>(&rect));fprintf(out,"cursor=%ld,%ld injected_channel=%x\n",rect.left,rect.top,button);PostMessageW(search.window,WM_APP+0x2d1,id.uID,button);Sleep(100);fclose(out);return 0;}
if(action==L"queueclose") {
    Search menus{search.pid,0,{}};for(int n=0;n<300&&menus.menus.empty();n++){EnumWindows(findWindow,reinterpret_cast<LPARAM>(&menus));Sleep(10);}
    if(menus.menus.empty()){fclose(out);return 20;}
    SendMessageW(search.window,WM_APP+0x2d1,id.uID,WM_LBUTTONDOWN);
    SendMessageW(search.window,WM_APP+0x2d1,id.uID,WM_MBUTTONDOWN);
    PostMessageW(search.window,WM_CANCELMODE,0,0);fclose(out);return 0;
}
if(action==L"postmenu"||action==L"cancelmenu"){if(action==L"postmenu"&&GetForegroundWindow()!=search.window){fprintf(out,"foreground prerequisite absent\n");fclose(out);return 23;}PostMessageW(search.window,WM_APP+0x2d1,id.uID,WM_RBUTTONDOWN);int result=selectMenu(search.pid,search.window,out,action==L"postmenu");fclose(out);return result;}
if(FAILED(hr)){fclose(out);return 7;}
IUIAutomation *automation=0;hr=CoCreateInstance(CLSID_CUIAutomation,0,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&automation));if(FAILED(hr)){fclose(out);return 8;}
VARIANT nameValue{};nameValue.vt=VT_BSTR;nameValue.bstrVal=SysAllocString(argv[5]);IUIAutomationCondition *condition=0;IUIAutomationCondition *nameCondition=0,*identity=0;automation->CreatePropertyCondition(UIA_NamePropertyId,nameValue,&nameCondition);VariantClear(&nameValue);VARIANT aid{};aid.vt=VT_BSTR;aid.bstrVal=SysAllocString(L"NotifyItemIcon");automation->CreatePropertyCondition(UIA_AutomationIdPropertyId,aid,&identity);VariantClear(&aid);automation->CreateAndCondition(nameCondition,identity,&condition);nameCondition->Release();identity->Release();
auto findIcon=[&](HWND window){IUIAutomationElement *root=0,*icon=0;if(window&&SUCCEEDED(automation->ElementFromHandle(window,&root))){root->FindFirst(TreeScope_Descendants,condition,&icon);root->Release();}return icon;};

IUIAutomationElement *icon=findIcon(FindWindowW(L"Shell_TrayWnd",0));
if(!icon)icon=findIcon(FindWindowW(L"TopLevelWindowForOverflowXamlIsland",0));

IUIAutomationInvokePattern *expander=0; bool opened=false;
if(!icon) {
    IUIAutomationElement *root=0;automation->ElementFromHandle(FindWindowW(L"Shell_TrayWnd",0),&root);
    if(root) {
        VARIANT cls{};cls.vt=VT_BSTR;cls.bstrVal=SysAllocString(L"SystemTray.NormalButton");IUIAutomationCondition *match=0;
        automation->CreatePropertyCondition(UIA_ClassNamePropertyId,cls,&match);VariantClear(&cls);IUIAutomationElementArray *array=0;root->FindAll(TreeScope_Descendants,match,&array);
        if(array) {
            int count=0;array->get_Length(&count);
            for(int i=0;i<count&&!expander;i++) {
                IUIAutomationElement *element=0;array->GetElement(i,&element);BSTR aid=0,label=0;element->get_CurrentAutomationId(&aid);element->get_CurrentName(&label);
                std::wstring name=label?label:L"";
                if(aid&&wcscmp(aid,L"SystemTrayIcon")==0&&(name.rfind(L"Показать скрытые значки",0)==0||name.rfind(L"Show hidden icons",0)==0)) {
                    bool expanded=IsWindowVisible(FindWindowW(L"TopLevelWindowForOverflowXamlIsland",0)); fprintf(out,"overflow initial visible=%d\n",expanded);
                    if(!expanded&&SUCCEEDED(element->GetCurrentPatternAs(UIA_InvokePatternId,IID_PPV_ARGS(&expander))))opened=SUCCEEDED(expander->Invoke());
                }
                SysFreeString(aid);SysFreeString(label);element->Release();
            }
            array->Release();
        }
        match->Release();root->Release();
    }
    for(int i=0;i<100&&!icon;i++){Sleep(30);icon=findIcon(FindWindowW(L"TopLevelWindowForOverflowXamlIsland",0));}
}
int result=0;
if(!icon){fprintf(out,"own icon not found overflow visible=%d\n",IsWindowVisible(FindWindowW(L"TopLevelWindowForOverflowXamlIsland",0)));result=9;}
else {
    BSTR name=0;icon->get_CurrentName(&name);RECT actual{};icon->get_CurrentBoundingRectangle(&actual);
    fprintf(out,"own name %s actual rect %ld,%ld,%ld,%ld\n",name?utf8(name).c_str():"",actual.left,actual.top,actual.right,actual.bottom);SysFreeString(name);
    if(action==L"invoke") {
        IUIAutomationInvokePattern *invoke=0;hr=icon->GetCurrentPatternAs(UIA_InvokePatternId,IID_PPV_ARGS(&invoke));
        if(SUCCEEDED(hr)){hr=invoke->Invoke();invoke->Release();}fprintf(out,"own Shell invoke %lx\n",hr);if(FAILED(hr))result=10;
    }
    icon->Release();
}
if(opened&&expander&&IsWindowVisible(FindWindowW(L"TopLevelWindowForOverflowXamlIsland",0)))expander->Invoke();if(expander)expander->Release();condition->Release();automation->Release();fclose(out);CoUninitialize();return result;
}
