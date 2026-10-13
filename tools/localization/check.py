#!/usr/bin/env python3
"""Compare generated locale data/selectors against direct C++ tables and arithmetic."""
from pathlib import Path
import argparse
import hashlib
import subprocess
import tempfile
import generate

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('source', type=Path)
parser.add_argument('--runner', type=Path, required=True)
args = parser.parse_args()
source = args.source.read_bytes()
assert hashlib.sha256(source).hexdigest() == generate.SHA256
rules = generate.rows(source.decode(), 'plural_rules')
with tempfile.TemporaryDirectory(prefix='electron2d-locale-oracle-') as directory:
    root = Path(directory)
    translation_source = args.source.parent / 'translation_server.cpp'
    assert hashlib.sha256(translation_source.read_bytes()).hexdigest() == "42446b4ce562ff00917b271c48ed138407e6e6f77cda42a58e42375c89a2c0e9"
    original = translation_source.read_text()
    start = original.index('TranslationServer::Locale::Locale(')
    end = original.index('\nString TranslationServer::format_number', start)
    code = ['#include <iostream>', '#include <sstream>', '#include <cstdint>', '#include <set>',
            '#include "' + str(args.source.resolve()) + '"',
            '#include "' + str((Path(__file__).parent / 'oracle_shim.h').resolve()) + '"', original[start:end], 'int main(){']
    for name, tag, first, second in [('language_list','L',0,1),('script_list','S',1,0),('country_names','C',0,1)]:
        code += [f'{{std::set<std::string> seen; for(int i=0; {name}[i][0]; ++i) if(seen.insert({name}[i][{first}]).second) std::cout<<"{tag}\\t"<<{name}[i][{first}]<<"\\t"<<{name}[i][{second}]<<"\\n";}}']
    code += ['TranslationServer server;',
             'for(int i=0;locale_variants[i][0];++i)server.variant_map[locale_variants[i][0]]=locale_variants[i][1];',
             'for(int i=0;locale_renames[i][0];++i)if(locale_renames[i][1][0])server.locale_rename_map[locale_renames[i][0]]=locale_renames[i][1];',
             'for(int i=0;country_renames[i][0];++i)if(country_renames[i][1][0])server.country_rename_map[country_renames[i][0]]=country_renames[i][1];',
             'for(int i=0;script_list[i][0];++i)server.script_map[script_list[i][1]]=script_list[i][0];',
             'for(int i=0;locale_scripts[i][0];++i){TranslationServer::LocaleScriptInfo entry;entry.name=locale_scripts[i][0];entry.script=locale_scripts[i][1];entry.default_country=locale_scripts[i][2];for(auto country:String(locale_scripts[i][3]).split(",",false))entry.supported_countries.insert(country);server.locale_script_info.push_back(entry);}',
             'auto check=[&](const String &input){for(bool defaults:{false,true}){TranslationServer::Locale locale(server,input,defaults);std::cout<<"N\\t"<<input<<"\\t"<<defaults<<"\\t"<<locale.text()<<"\\n";}};',
             'for(int i=0;language_list[i][0];++i){check(language_list[i][0]);check(String(language_list[i][0])+"_US");}',
             'for(int i=0;locale_renames[i][0];++i)check(locale_renames[i][0]);',
             'for(int i=0;locale_scripts[i][0];++i){check(String(locale_scripts[i][0])+"_"+locale_scripts[i][1]);for(auto country:String(locale_scripts[i][3]).split(",",false))check(String(locale_scripts[i][0])+"_"+country);}',
             'for(auto input:{"","en_us","sr_RS@latin","sr@cyrillic;latin","ca_ES@valencia","unknown_Abcd_US","en-UK","en__US","zh_Arab_CN","sr_Latn_RS"})check(input);']
    counts = list(range(0, 301)) + [1000,1001,100000,100001,1000000,1000001,9223372036854775807]
    for index, (locales, definition) in enumerate(rules):
        import re
        forms, expression = re.fullmatch(r'nplurals=(\d+); plural=(.*);', definition).groups()
        # Independently correct the four known singular predicates; don't use the C# expression generator.
        if locales in ['is mk', 'tzm', 'am as bn doi fa gu hi kn pcm zu', 'ceb fil tl']:
            expression = f'(({expression}) ? 0 : 1)'
        code += ['{std::istringstream locales(' + generate.json.dumps(locales) + ');std::string locale;',
                 'while(locales>>locale){', 'for(std::uint64_t n:std::vector<std::uint64_t>{' + ','.join(str(n)+'ULL' for n in counts) + '})',
                 f'std::cout<<"P\\t"<<locale<<"\\t"<<n<<"\\t"<<int({expression})<<"\\t{forms}\\n";', '}}']
    code += ['}']
    (root/'oracle.cpp').write_text('\n'.join(code))
    subprocess.run(['c++','-std=c++17','-O2',str(root/'oracle.cpp'),'-o',str(root/'oracle')],check=True)
    with (root/'expected.tsv').open('w') as output:
        subprocess.run([str(root/'oracle')],stdout=output,check=True)
    subprocess.run(['dotnet',str(args.runner.resolve()),'--locale-oracle',str(root/'expected.tsv')],check=True)
    print('Independent C++ locale table and plural arithmetic oracle passed.')
