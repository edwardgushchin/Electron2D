#define UNICODE
#define _UNICODE
#include <windows.h>
#include <oleacc.h>
#include <cstdio>
#include <string>
#include <vector>
#include <cstdlib>
struct Search { DWORD pid; std::vector<HWND> windows; };
static BOOL CALLBACK enumerate(HWND w, LPARAM p) {
 auto &s=*reinterpret_cast<Search*>(p);DWORD pid=0;GetWindowThreadProcessId(w,&pid);wchar_t name[64]{};GetClassNameW(w,name,64);
 if(pid==s.pid&&IsWindowVisible(w)&&wcscmp(name,L"#32768")==0)s.windows.push_back(w);return TRUE;
}
static std::vector<HWND> menus(DWORD pid){Search s{pid,{}};EnumWindows(enumerate,reinterpret_cast<LPARAM>(&s));return s.windows;}
static std::string utf8(const wchar_t *text){int n=WideCharToMultiByte(CP_UTF8,0,text,-1,nullptr,0,nullptr,nullptr);std::string s(n,0);WideCharToMultiByte(CP_UTF8,0,text,-1,s.data(),n,nullptr,nullptr);s.pop_back();return s;}
static void dump(HWND w,FILE *out){
 auto menu=reinterpret_cast<HMENU>(SendMessageW(w,0x1e1,0,0));RECT bounds{};GetWindowRect(w,&bounds);
 fprintf(out,"window %ld %ld %ld %ld rtl=%d items=%d\n",bounds.left,bounds.top,bounds.right,bounds.bottom,(GetWindowLongPtrW(w,GWL_EXSTYLE)&WS_EX_LAYOUTRTL)!=0,GetMenuItemCount(menu));
 for(int i=0;i<GetMenuItemCount(menu);i++){
  wchar_t text[4096]{};MENUITEMINFOW m{};m.cbSize=sizeof(m);m.fMask=MIIM_STRING|MIIM_FTYPE|MIIM_STATE|MIIM_ID|MIIM_BITMAP|MIIM_SUBMENU;m.dwTypeData=text;m.cch=4096;
  if(!GetMenuItemInfoW(menu,i,TRUE,&m)){fprintf(out,"ERROR %lu\n",GetLastError());continue;}RECT r{};GetMenuItemRect(nullptr,menu,i,&r);
  fprintf(out,"item %d id=%u type=%u state=%u bitmap=%d submenu=%d rect=%ld,%ld,%ld,%ld text=%s\n",i,m.wID,m.fType,m.fState,m.hbmpItem!=nullptr,m.hSubMenu!=nullptr,r.left,r.top,r.right,r.bottom,utf8(text).c_str());
 }fflush(out);
}
static void capture(HWND w,const wchar_t *path,FILE *out){
 RECT r{};GetWindowRect(w,&r);int width=r.right-r.left,height=r.bottom-r.top;if(width<=0||height<=0)return;
 BITMAPINFO info{};info.bmiHeader.biSize=sizeof(BITMAPINFOHEADER);info.bmiHeader.biWidth=width;info.bmiHeader.biHeight=-height;info.bmiHeader.biPlanes=1;info.bmiHeader.biBitCount=32;
 void *pixels=nullptr;HDC dc=CreateCompatibleDC(nullptr);HBITMAP bitmap=CreateDIBSection(dc,&info,DIB_RGB_COLORS,&pixels,nullptr,0);auto old=SelectObject(dc,bitmap);
 HDC screen=GetWindowDC(w);BitBlt(dc,0,0,width,height,screen,0,0,SRCCOPY);ReleaseDC(w,screen);
 int blue=0,red=0;auto data=static_cast<const unsigned char*>(pixels);for(int i=0;i<width*height;i++){auto p=data+i*4;if(p[0]==255&&p[1]==0&&p[2]==0)blue++;if(p[2]>p[0]+80&&p[2]>p[1]+80)red++;}fprintf(out,"blue_pixels=%d red_pixels=%d\n",blue,red);fflush(out);
 BITMAPFILEHEADER header{};header.bfType=0x4d42;header.bfOffBits=sizeof(header)+sizeof(BITMAPINFOHEADER);header.bfSize=header.bfOffBits+width*height*4;
 FILE *f=nullptr;_wfopen_s(&f,path,L"wb");if(f){fwrite(&header,sizeof(header),1,f);fwrite(&info.bmiHeader,sizeof(info.bmiHeader),1,f);fwrite(pixels,width*height*4,1,f);fclose(f);}SelectObject(dc,old);DeleteObject(bitmap);DeleteDC(dc);
}
static bool key(DWORD pid,HWND owner,UINT value){
 DWORD foregroundPid=0;auto foreground=GetForegroundWindow();GetWindowThreadProcessId(foreground,&foregroundPid);
 if(foreground!=owner||foregroundPid!=pid)return false;
 if(!PostMessageW(owner,WM_KEYDOWN,value,1)||!PostMessageW(owner,WM_KEYUP,value,0xc0000001))return false;Sleep(80);return true;
}
int wmain(int argc,wchar_t **argv){
 CoInitializeEx(nullptr,COINIT_APARTMENTTHREADED);static_assert(sizeof(MENUITEMINFOW)==80,"Windows x64 menu ABI");if(argc!=5)return 2;DWORD pid=wcstoul(argv[1],nullptr,10);FILE *out=nullptr;_wfopen_s(&out,argv[3],L"wb");if(!out)return 3;
 std::vector<HWND> found;for(int i=0;i<1000;i++){found=menus(pid);if(!found.empty())break;Sleep(10);}if(found.empty()){fprintf(out,"TIMEOUT\n");fclose(out);return 4;}
 Sleep(100);HWND w=found.back();dump(w,out);capture(w,argv[4],out);std::wstring action=argv[2];
 GUITHREADINFO gui{};gui.cbSize=sizeof(gui);GetGUIThreadInfo(GetWindowThreadProcessId(w,nullptr),&gui);HWND owner=gui.hwndMenuOwner;DWORD ownerPid=0;GetWindowThreadProcessId(owner,&ownerPid);fprintf(out,"menuowner=%p pid=%lu active=%p focus=%p foreground=%p flags=%lu\n",owner,ownerPid,gui.hwndActive,gui.hwndFocus,GetForegroundWindow(),gui.flags);fflush(out);
 wchar_t title[512]{};GetWindowTextW(owner,title,512);fprintf(out,"menuowner_title=%s\n",utf8(title).c_str());fflush(out);
 if(ownerPid!=pid)return 6;
 auto fail=[&](int code){PostMessageW(owner,WM_CANCELMODE,0,0);fclose(out);return code;};
 auto invoke=[&](HWND popup,int index){IAccessible *accessible=nullptr;auto hr=AccessibleObjectFromWindow(popup,OBJID_CLIENT,IID_IAccessible,reinterpret_cast<void**>(&accessible));fprintf(out,"accessible %lx\n",hr);if(SUCCEEDED(hr)){VARIANT child{};child.vt=VT_I4;child.lVal=index+1;hr=accessible->accDoDefaultAction(child);fprintf(out,"invoke %lx\n",hr);accessible->Release();}fflush(out);return SUCCEEDED(hr);};
 if(action.rfind(L"key:",0)==0 || action==L"keycancel"){
  if(GetForegroundWindow()!=owner)return fail(11);int index=action==L"keycancel"?0:_wtoi(action.c_str()+4);if(!key(pid,owner,VK_DOWN))return fail(12);for(int i=0;i<index;i++)if(!key(pid,owner,VK_DOWN))return fail(12);if(action!=L"keycancel"&&!key(pid,owner,VK_RETURN))return fail(12);
 }
 else if(action==L"cancel")PostMessageW(owner,WM_CANCELMODE,0,0);
 else if(action.rfind(L"sub:",0)==0){if(!invoke(w,_wtoi(action.c_str()+4)))return fail(7);for(int i=0;i<300;i++){found=menus(pid);if(found.size()>1)break;Sleep(10);}HWND nested=nullptr;for(auto candidate:found)if(candidate!=w)nested=candidate;if(!nested)return fail(8);dump(nested,out);if(!invoke(nested,0))return fail(9);}
 else{int index=_wtoi(argv[2]);auto menu=reinterpret_cast<HMENU>(SendMessageW(w,0x1e1,0,0));RECT rect{};GetMenuItemRect(nullptr,menu,index,&rect);POINT point{(rect.left+rect.right)/2,(rect.top+rect.bottom)/2};ScreenToClient(w,&point);PostMessageW(w,WM_MOUSEMOVE,0,MAKELPARAM(point.x,point.y));Sleep(100);if(!invoke(w,index))return fail(7);}
 for(int i=0;i<300;i++){if(menus(pid).empty()){fprintf(out,"closed\n");fclose(out);return 0;}Sleep(10);}
 for(auto popup:menus(pid))PostMessageW(popup,WM_CANCELMODE,0,0);fprintf(out,"CANCELLED_AFTER_TIMEOUT\n");fclose(out);return 5;
}
