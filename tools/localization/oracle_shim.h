#include <algorithm>
#include <map>
#include <set>
#include <string>
#include <vector>

struct String : std::string {
  using std::string::string;
  String(const std::string &value) : std::string(value) {}
  bool is_empty() const { return empty(); }
  String replace_char(char from, char to) const { String value = *this; std::replace(value.begin(), value.end(), from, to); return value; }
  String to_lower() const { String value = *this; for(char &c:value) if(c>='A'&&c<='Z') c+=32; return value; }
  std::vector<String> split(const char *separator, bool allow_empty = true) const {
    std::vector<String> result; size_t begin = 0;
    for(;;) { auto end = find(separator, begin); auto value = substr(begin, end-begin); if(allow_empty || !value.empty()) result.emplace_back(value); if(end==npos) break; begin=end+1; }
    return result;
  }
  String get_slicec(char separator, int index) const { char text[2]={separator,0}; auto values=split(text); return index<(int)values.size()?values[index]:String(); }
};
template<class T> using Vector=std::vector<T>;
struct StringMap : std::map<String,String> {
  bool has(const String &key) const { return count(key)!=0; }
  const String &operator[](const String &key) const { return at(key); }
  String &operator[](const String &key) { return std::map<String,String>::operator[](key); }
};
struct StringSet : std::set<String> { bool has(const String &key) const { return count(key)!=0; } };
bool is_ascii_upper_case(char c){return c>='A'&&c<='Z';}
bool is_ascii_lower_case(char c){return c>='a'&&c<='z';}
struct TranslationServer {
  struct LocaleScriptInfo { String name,script,default_country; StringSet supported_countries; };
  StringMap variant_map, locale_rename_map, country_rename_map, script_map;
  std::vector<LocaleScriptInfo> locale_script_info;
  struct Locale {
    String language,script,country,variant;
    Locale(const TranslationServer &p_server,const String &p_locale,bool p_add_defaults);
    String text() const { String value=language; for(auto part:{script,country,variant}) if(!part.empty()) value+="_"+part; return value; }
  };
};
