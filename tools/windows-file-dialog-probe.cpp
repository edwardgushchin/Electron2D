#include <windows.h>
#include <UIAutomation.h>
#include <wrl/client.h>
#include <cstdio>
#include <string>
using Microsoft::WRL::ComPtr;
static void Check(HRESULT value) {if(FAILED(value)){std::fprintf(stderr,"UIA HRESULT=%08lx\n",(unsigned long)value);throw value;}}
static std::string UTF8(const wchar_t* text) {if(!text)return {};int length=WideCharToMultiByte(CP_UTF8,0,text,-1,nullptr,0,nullptr,nullptr);std::string value(length,'\0');WideCharToMultiByte(CP_UTF8,0,text,-1,value.data(),length,nullptr,nullptr);value.pop_back();return value;}
struct Target {DWORD pid;std::wstring title;HWND window=nullptr;};
static BOOL CALLBACK Locate(HWND window,LPARAM context) {auto& target=*reinterpret_cast<Target*>(context);DWORD pid=0;GetWindowThreadProcessId(window,&pid);if(pid!=target.pid)return TRUE;wchar_t title[1024];GetWindowTextW(window,title,1024);if(target.title==title){target.window=window;return FALSE;}return TRUE;}
static ComPtr<IUIAutomationElement> ByName(IUIAutomation* automation,IUIAutomationElement* root,const wchar_t* name) {
    VARIANT value{};value.vt=VT_BSTR;value.bstrVal=SysAllocString(name);ComPtr<IUIAutomationCondition> condition;Check(automation->CreatePropertyCondition(UIA_NamePropertyId,value,&condition));VariantClear(&value);ComPtr<IUIAutomationElement> found;Check(root->FindFirst(TreeScope_Descendants,condition.Get(),&found));return found;
}
static BOOL CALLBACK FindEdit(HWND child,LPARAM context) {wchar_t name[64];GetClassNameW(child,name,64);if(std::wstring(name)==L"Edit"){*reinterpret_cast<HWND*>(context)=child;return FALSE;}return TRUE;}
static void Toggle(IUIAutomation* automation,IUIAutomationElement* root,const wchar_t* label,bool desired) {
    auto element=ByName(automation,root,label);if(!element)throw E_FAIL;ComPtr<IUIAutomationTogglePattern> pattern;Check(element->GetCurrentPatternAs(UIA_TogglePatternId,IID_PPV_ARGS(&pattern)));ToggleState state;Check(pattern->get_CurrentToggleState(&state));if((state==ToggleState_On)!=desired)Check(pattern->Toggle());Check(pattern->get_CurrentToggleState(&state));if((state==ToggleState_On)!=desired)throw E_FAIL;
}
static void ChooseElement(IUIAutomation* automation,IUIAutomationElement* root,IUIAutomationElement* element,const wchar_t* caption) {
    ComPtr<IUIAutomationExpandCollapsePattern> expand;Check(element->GetCurrentPatternAs(UIA_ExpandCollapsePatternId,IID_PPV_ARGS(&expand)));Check(expand->Expand());
    auto item=ByName(automation,element,caption);if(!item)item=ByName(automation,root,caption);if(!item)throw E_FAIL;ComPtr<IUIAutomationSelectionItemPattern> selection;Check(item->GetCurrentPatternAs(UIA_SelectionItemPatternId,IID_PPV_ARGS(&selection)));Check(selection->Select());Check(expand->Collapse());
}
static void Choose(IUIAutomation* automation,IUIAutomationElement* root,const wchar_t* label,const wchar_t* caption) {
    auto element=ByName(automation,root,label);if(!element)throw E_FAIL;CONTROLTYPEID type;Check(element->get_CurrentControlType(&type));if(type!=UIA_ComboBoxControlTypeId){VARIANT value{};value.vt=VT_I4;value.lVal=UIA_ComboBoxControlTypeId;ComPtr<IUIAutomationCondition> condition;Check(automation->CreatePropertyCondition(UIA_ControlTypePropertyId,value,&condition));ComPtr<IUIAutomationElement> combo;Check(element->FindFirst(TreeScope_Descendants,condition.Get(),&combo));element=combo;if(!element)throw E_FAIL;}
    ChooseElement(automation,root,element.Get(),caption);
}
int wmain(int argc,wchar_t** argv) {
    HWND window=nullptr;const char* stage="locate";
    try {
        if(argc<5)return 2;Check(CoInitializeEx(nullptr,COINIT_MULTITHREADED));struct Apartment{~Apartment(){CoUninitialize();}} apartment;
        Target target{wcstoul(argv[2],nullptr,10),argv[3]};auto deadline=GetTickCount64()+15000;do{EnumWindows(Locate,reinterpret_cast<LPARAM>(&target));if(!target.window)Sleep(20);}while(!target.window&&GetTickCount64()<deadline);window=target.window;if(!window)return 3;
        auto ready=GetTickCount64()+10000;while(!GetDlgItem(window,1)&&GetTickCount64()<ready)Sleep(20);if(!GetDlgItem(window,1))throw E_FAIL;
        if(std::wstring(argv[4])==L"cancel"){PostMessageW(window,WM_COMMAND,MAKEWPARAM(IDCANCEL,BN_CLICKED),0);std::puts("owned-dialog-cancelled");return 0;}
        stage="automation";ComPtr<IUIAutomation> automation;Check(CoCreateInstance(CLSID_CUIAutomation,nullptr,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&automation)));ComPtr<IUIAutomationElement> root;Check(automation->ElementFromHandle(window,&root));
        if(argc>=11){stage="initial-folder";auto ready=GetTickCount64()+10000;while(!ByName(automation.Get(),root.Get(),argv[10])&&GetTickCount64()<ready)Sleep(20);if(!ByName(automation.Get(),root.Get(),argv[10]))throw E_FAIL;}
        if(argc>=10&&std::wstring(argv[6])!=L"-"){auto ready=GetTickCount64()+10000;while(!ByName(automation.Get(),root.Get(),argv[6])&&GetTickCount64()<ready)Sleep(20);}
        if(std::wstring(argv[4])==L"dump") {
            ComPtr<IUIAutomationCondition> condition;Check(automation->CreateTrueCondition(&condition));ComPtr<IUIAutomationElementArray> elements;Check(root->FindAll(TreeScope_Descendants,condition.Get(),&elements));int count=0;elements->get_Length(&count);
            for(int i=0;i<count;i++){ComPtr<IUIAutomationElement> item;elements->GetElement(i,&item);CONTROLTYPEID type;item->get_CurrentControlType(&type);if(type!=UIA_EditControlTypeId&&type!=UIA_ComboBoxControlTypeId&&type!=UIA_CheckBoxControlTypeId&&type!=UIA_ButtonControlTypeId)continue;BSTR name=nullptr,id=nullptr;item->get_CurrentName(&name);item->get_CurrentAutomationId(&id);std::printf("type=%d id=%s name=%s\n",type,UTF8(id).c_str(),UTF8(name).c_str());SysFreeString(name);SysFreeString(id);}
            PostMessageW(window,WM_COMMAND,MAKEWPARAM(IDCANCEL,BN_CLICKED),0);return 0;
        }
        if(argc<10)return 2;
        stage="checkbox";if(std::wstring(argv[6])!=L"-")Toggle(automation.Get(),root.Get(),argv[6],_wtoi(argv[7])!=0);
        stage="choice";if(std::wstring(argv[8])!=L"-")Choose(automation.Get(),root.Get(),argv[8],argv[9]);
        if(argc>=12&&_wtoi(argv[11])>=0){stage="filter";auto combo=GetDlgItem(window,1136);int index=_wtoi(argv[11]);if(!combo||SendMessageW(combo,CB_GETCOUNT,0,0)<=index)throw E_FAIL;auto length=SendMessageW(combo,CB_GETLBTEXTLEN,index,0);if(length<1||length>16384)throw E_FAIL;std::wstring caption(length+1,L'\0');SendMessageW(combo,CB_GETLBTEXT,index,reinterpret_cast<LPARAM>(caption.data()));ComPtr<IUIAutomationElement> element;Check(automation->ElementFromHandle(combo,&element));ChooseElement(automation.Get(),root.Get(),element.Get(),caption.c_str());}
        if(std::wstring(argv[4])==L"directory"||std::wstring(argv[4])==L"item") {stage="item-selection";auto item=ByName(automation.Get(),root.Get(),argv[5]);if(!item&&std::wstring(argv[4])==L"item"){std::wstring stem=argv[5];auto dot=stem.find_last_of(L'.');if(dot!=std::wstring::npos)stem.resize(dot);item=ByName(automation.Get(),root.Get(),stem.c_str());}if(!item)throw E_FAIL;ComPtr<IUIAutomationSelectionItemPattern> selection;Check(item->GetCurrentPatternAs(UIA_SelectionItemPatternId,IID_PPV_ARGS(&selection)));Check(selection->Select());}
        else if(std::wstring(argv[5])!=L"-") {
            stage="filename";HWND control=GetDlgItem(window,1148),edit=nullptr;if(control)EnumChildWindows(control,FindEdit,reinterpret_cast<LPARAM>(&edit));if(!edit&&control){wchar_t name[64];GetClassNameW(control,name,64);if(std::wstring(name)==L"Edit")edit=control;}
            if(!edit)throw E_FAIL;ComPtr<IUIAutomationElement> field;Check(automation->ElementFromHandle(edit,&field));Check(field->SetFocus());ComPtr<IUIAutomationValuePattern> value;Check(field->GetCurrentPatternAs(UIA_ValuePatternId,IID_PPV_ARGS(&value)));Check(value->SetValue(argv[5]));wchar_t actual[32768];SendMessageW(edit,WM_GETTEXT,32768,reinterpret_cast<LPARAM>(actual));if(std::wstring(actual)!=argv[5])throw E_FAIL;
        }
        stage="accept";VARIANT id{};id.vt=VT_BSTR;id.bstrVal=SysAllocString(L"1");ComPtr<IUIAutomationCondition> condition;Check(automation->CreatePropertyCondition(UIA_AutomationIdPropertyId,id,&condition));VariantClear(&id);VARIANT button{};button.vt=VT_I4;button.lVal=UIA_ButtonControlTypeId;ComPtr<IUIAutomationCondition> buttonCondition,acceptCondition;Check(automation->CreatePropertyCondition(UIA_ControlTypePropertyId,button,&buttonCondition));Check(automation->CreateAndCondition(condition.Get(),buttonCondition.Get(),&acceptCondition));condition=acceptCondition;ComPtr<IUIAutomationElement> accept;auto acceptDeadline=GetTickCount64()+10000;do{Check(root->FindFirst(TreeScope_Descendants,condition.Get(),&accept));if(!accept)Sleep(20);}while(!accept&&GetTickCount64()<acceptDeadline);if(!accept)throw E_FAIL;ComPtr<IUIAutomationInvokePattern> invoke;Check(accept->GetCurrentPatternAs(UIA_InvokePatternId,IID_PPV_ARGS(&invoke)));Check(invoke->Invoke());std::puts("owned-controls-selected-and-accepted");return 0;
    }catch(HRESULT failure){std::fprintf(stderr,"owned-dialog-action-failed stage=%s HRESULT=%08lx\n",stage,(unsigned long)failure);if(window)PostMessageW(window,WM_COMMAND,MAKEWPARAM(IDCANCEL,BN_CLICKED),0);return 1;}
}
