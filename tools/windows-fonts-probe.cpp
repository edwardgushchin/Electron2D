#include <windows.h>
#include <dwrite_2.h>
#include <wrl/client.h>
#include <algorithm>
#include <cstdio>
#include <set>
#include <string>
#include <vector>
using Microsoft::WRL::ComPtr;
static void Check(HRESULT value) { if(FAILED(value)){std::fprintf(stderr,"DirectWrite HRESULT=%08lx\n",(unsigned long)value);throw value;} }
static std::string UTF8(const std::wstring& text) {
    int size=WideCharToMultiByte(CP_UTF8,0,text.data(),(int)text.size(),nullptr,0,nullptr,nullptr);std::string value(size,'\0');
    WideCharToMultiByte(CP_UTF8,0,text.data(),(int)text.size(),value.data(),size,nullptr,nullptr);return value;
}
static std::wstring Name(IDWriteLocalizedStrings* names,UINT32 slot) {
    UINT32 size=0;Check(names->GetStringLength(slot,&size));std::vector<wchar_t> value(size+1);Check(names->GetString(slot,value.data(),size+1));return {value.data(),size};
}
static DWRITE_FONT_STRETCH Width(int percent) {
    float widths[]={50,62.5f,75,87.5f,100,112.5f,125,150,200};int best=0;percent=std::clamp(percent,50,200);
    for(int i=1;i<9;i++)if(std::abs(widths[i]-percent)<std::abs(widths[best]-percent))best=i;return (DWRITE_FONT_STRETCH)(best+1);
}
class Source final : public IDWriteTextAnalysisSource {
    ULONG references=1;std::wstring text,locale;
public:
    Source(const wchar_t* value,const wchar_t* language):text(value),locale(language) {if(locale.empty()){wchar_t name[LOCALE_NAME_MAX_LENGTH];if(GetUserDefaultLocaleName(name,LOCALE_NAME_MAX_LENGTH))locale=name;}}
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid,void** result) override {if(iid==__uuidof(IUnknown)||iid==__uuidof(IDWriteTextAnalysisSource)){*result=this;AddRef();return S_OK;}*result=nullptr;return E_NOINTERFACE;}
    ULONG STDMETHODCALLTYPE AddRef() override {return InterlockedIncrement(&references);}
    ULONG STDMETHODCALLTYPE Release() override {auto remaining=InterlockedDecrement(&references);if(!remaining)delete this;return remaining;}
    HRESULT STDMETHODCALLTYPE GetTextAtPosition(UINT32 position,const wchar_t** value,UINT32* size) override {*size=position<text.size()?(UINT32)text.size()-position:0;*value=*size?text.data()+position:nullptr;return S_OK;}
    HRESULT STDMETHODCALLTYPE GetTextBeforePosition(UINT32 position,const wchar_t** value,UINT32* size) override {*size=position<=text.size()?position:0;*value=*size?text.data():nullptr;return S_OK;}
    DWRITE_READING_DIRECTION STDMETHODCALLTYPE GetParagraphReadingDirection() override {return DWRITE_READING_DIRECTION_LEFT_TO_RIGHT;}
    HRESULT STDMETHODCALLTYPE GetLocaleName(UINT32 position,UINT32* size,const wchar_t** value) override {*size=position<text.size()?(UINT32)text.size()-position:0;*value=locale.data();return S_OK;}
    HRESULT STDMETHODCALLTYPE GetNumberSubstitution(UINT32 position,UINT32* size,IDWriteNumberSubstitution** value) override {*size=position<text.size()?(UINT32)text.size()-position:0;*value=nullptr;return S_OK;}
};
static std::string Describe(IDWriteFont* font) {
    ComPtr<IDWriteFontFace> face;Check(font->CreateFontFace(&face));UINT32 count=0;Check(face->GetFiles(&count,nullptr));if(count!=1)throw E_NOTIMPL;
    ComPtr<IDWriteFontFile> file;Check(face->GetFiles(&count,&file));ComPtr<IDWriteFontFileLoader> loader;Check(file->GetLoader(&loader));ComPtr<IDWriteLocalFontFileLoader> local;Check(loader.As(&local));
    const void* key=nullptr;UINT32 keySize=0,size=0;Check(file->GetReferenceKey(&key,&keySize));Check(local->GetFilePathLengthFromKey(key,keySize,&size));std::vector<wchar_t> path(size+1);Check(local->GetFilePathFromKey(key,keySize,path.data(),size+1));
    ComPtr<IDWriteFontFamily> family;Check(font->GetFontFamily(&family));ComPtr<IDWriteLocalizedStrings> names;Check(family->GetFamilyNames(&names));UINT32 slot=0;BOOL exists=FALSE;Check(names->FindLocaleName(L"en-us",&slot,&exists));
    return UTF8(path.data())+'\t'+std::to_string(face->GetIndex())+'\t'+UTF8(Name(names.Get(),exists?slot:0))+'\t'+std::to_string(font->GetWeight())+'\t'+std::to_string(font->GetStretch())+'\t'+std::to_string(font->GetStyle());
}
int wmain(int argc,wchar_t** argv) {
    try {
        if(argc<2)return 2;ComPtr<IDWriteFactory2> factory;Check(DWriteCreateFactory(DWRITE_FACTORY_TYPE_SHARED,__uuidof(IDWriteFactory2),reinterpret_cast<IUnknown**>(factory.GetAddressOf())));
        ComPtr<IDWriteFontCollection> collection;Check(factory->GetSystemFontCollection(&collection,FALSE));
        if(std::wstring(argv[1])==L"names") {
            std::set<std::wstring> result;
            for(UINT32 i=0;i<collection->GetFontFamilyCount();i++){ComPtr<IDWriteFontFamily> family;Check(collection->GetFontFamily(i,&family));ComPtr<IDWriteLocalizedStrings> names;Check(family->GetFamilyNames(&names));for(UINT32 j=0;j<names->GetCount();j++)result.insert(Name(names.Get(),j));}
            for(auto& name:result)std::printf("%s\n",UTF8(name).c_str());return 0;
        }
        if(argc<6)return 2;int weight=std::clamp(_wtoi(argv[3]),100,999);auto width=Width(_wtoi(argv[4]));auto style=_wtoi(argv[5])?DWRITE_FONT_STYLE_ITALIC:DWRITE_FONT_STYLE_NORMAL;
        if(std::wstring(argv[1])==L"match"||std::wstring(argv[1])==L"advance") {
            UINT32 index=0;BOOL exists=FALSE;Check(collection->FindFamilyName(argv[2],&index,&exists));if(!exists)return 3;
            ComPtr<IDWriteFontFamily> family;Check(collection->GetFontFamily(index,&family));ComPtr<IDWriteFont> font;Check(family->GetFirstMatchingFont((DWRITE_FONT_WEIGHT)weight,width,style,&font));
            if(std::wstring(argv[1])==L"match")std::printf("%s\n",Describe(font.Get()).c_str());
            else {if(argc!=7)return 2;ComPtr<IDWriteFontFace> face;Check(font->CreateFontFace(&face));UINT32 scalar=wcstoul(argv[6],nullptr,10);UINT16 glyph=0;Check(face->GetGlyphIndices(&scalar,1,&glyph));DWRITE_GLYPH_METRICS glyphMetrics{};Check(face->GetDesignGlyphMetrics(&glyph,1,&glyphMetrics));DWRITE_FONT_METRICS metrics{};face->GetMetrics(&metrics);std::printf("%u\t%u\t%u\n",glyphMetrics.advanceWidth,metrics.designUnitsPerEm,glyph);}
            return 0;
        }
        if(std::wstring(argv[1])!=L"text"||argc!=8)return 2;
        ComPtr<IDWriteFontFallback> fallback;Check(factory->GetSystemFontFallback(&fallback));ComPtr<Source> source;source.Attach(new Source(argv[6],argv[7]));UINT32 length=(UINT32)wcslen(argv[6]),position=0;std::set<std::string> seen;
        while(position<length){UINT32 mapped=0;FLOAT scale=0;ComPtr<IDWriteFont> font;Check(fallback->MapCharacters(source.Get(),position,length-position,collection.Get(),argv[2],(DWRITE_FONT_WEIGHT)weight,style,width,&mapped,&font,&scale));if(!mapped||mapped>length-position)return 1;position+=mapped;if(font){auto value=Describe(font.Get());if(seen.insert(value).second)std::printf("%s\n",value.c_str());}}
        return 0;
    }catch(HRESULT){return 1;}
}
