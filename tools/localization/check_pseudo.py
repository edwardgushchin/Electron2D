#!/usr/bin/env python3
"""Check pseudolocalization against the pinned C++ transformations through a public consumer."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import subprocess

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('source', type=Path)
parser.add_argument('--runner', type=Path, required=True)
parser.add_argument('--artifacts', type=Path, required=True)
args = parser.parse_args()
if os.name == 'posix':
    import resource
    resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
source = args.source.read_text()
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
assert sha(args.source) == 'fccec2a280af4d8017ba417a9c6ccbf090e0988a4295d24178095422ea45058e'
assert 'StringName TranslationDomain::pseudolocalize' in source
root = args.artifacts.resolve()
root.mkdir(parents=True, exist_ok=True)
# Compile the original methods and accent table; the shim only supplies UTF-32 String operations.
methods = source[source.index('struct _character_accent_pair'):source.index('StringName TranslationDomain::get_message_from_translations')]
method_start = source.index('StringName TranslationDomain::pseudolocalize')
method_end = source.index('\nvoid TranslationDomain::_bind_methods', method_start)
shim = r'''
#include <codecvt>
#include <cstdint>
#include <cstring>
#include <iostream>
#include <iterator>
#include <locale>
#include <string>
struct String : std::u32string {
    using std::u32string::u32string;
    String(const std::u32string &value) : std::u32string(value) {}
    String(const char *value) { while (*value) push_back(*value++); }
    bool is_empty() const { return empty(); }
    String repeat(int count) const { String result; for (int i=0;i<count;i++) result += *this; return result; }
};
using StringName = String;
template<class T, size_t N> constexpr size_t std_size(const T (&)[N]) { return N; }
bool is_ascii_alphabet_char(char32_t c) { return (c>=U'A'&&c<=U'Z') || (c>=U'a'&&c<=U'z'); }
struct TranslationDomain {
    struct { bool override_enabled, double_vowels_enabled, accents_enabled, fake_bidi_enabled, skip_placeholders_enabled; float expansion_ratio; String prefix, suffix; } pseudolocalization;
    String _get_override_string(const String &) const;
    String _double_vowels(const String &) const;
    String _replace_with_accented_string(const String &) const;
    String _wrap_with_fakebidi_characters(const String &) const;
    String _add_padding(const String &, int) const;
    const char32_t *_get_accented_version(char32_t) const;
    bool _is_placeholder(const String &, int) const;
    StringName pseudolocalize(const StringName &) const;
};
std::string hex(const String &value) {
    auto bytes = std::wstring_convert<std::codecvt_utf8<char32_t>, char32_t>().to_bytes(value.data(), value.data()+value.size());
    std::string result; const char *digits="0123456789ABCDEF";
    for (unsigned char c:bytes) { result+=digits[c>>4]; result+=digits[c&15]; } return result;
}
'''
inputs = list('ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz') + [
    '', '😀', 'A😀%s', '😀a', '𐐀😀𐐨', 'FQXfhdm', 'a\u0301', 'F\u0301', 'éÅ中',
    '\n', 'a\n%s', '\r\n', '\t', '\0a', '%', '%%', '%%s', '%s%c%d%o%x%X%f',
    '%i %u %2d %.2f %S', '%s😀%f', '\u202eA\u202c', 'a\u200db', 'A\n\nB',
    '😀\n%s\n𐐀', 'aAeiIOuU', 'abcde', 'a'*31, 'A😀B😀C', '😀%s😀%X😀',
    'x\r%s\t%c\n', '100% ready', 'ß', 'F́Q́X́', 'suffix%']
assert len(inputs) * 32 * 9 * 3 > 50000
literal = lambda value: 'String(U"' + ''.join('\\x%X' % ord(c) for c in value) + '",' + str(len(value)) + ')'
code = source[:source.index('#include')] + shim + methods + source[method_start:method_end]
code += '\nint main(){ TranslationDomain domain;\n'
code += 'for (const auto &input:std::initializer_list<String>{' + ','.join(map(literal, inputs)) + '})\n'
code += 'for(float ratio:{0.0f,0.1f,0.33333334f,0.5f,0.99999994f,1.0f,1.5f,2.0f,3.25f})\n'
code += 'for (int affix=0;affix<3;affix++) for(int mask=0;mask<32;mask++){\n'
code += 'auto &p=domain.pseudolocalization;p.override_enabled=mask&1;p.double_vowels_enabled=mask&2;p.accents_enabled=mask&4;p.fake_bidi_enabled=mask&8;p.skip_placeholders_enabled=mask&16;p.expansion_ratio=ratio;\n'
code += 'p.prefix=affix==0?' + literal('') + ':affix==1?' + literal('[') + ':' + literal('前😀') + ';\n'
code += 'p.suffix=affix==0?' + literal('') + ':affix==1?' + literal(']') + ':' + literal('尾') + ';\n'
code += 'std::uint32_t bits;std::memcpy(&bits,&ratio,sizeof(bits));std::cout<<mask<<"\\t"<<bits<<"\\t"<<hex(p.prefix)<<"\\t"<<hex(p.suffix)<<"\\t"<<hex(input)<<"\\t"<<hex(domain.pseudolocalize(input))<<"\\n";\n}}\n'
(root/'oracle.cpp').write_text(code)
with (root/'build.log').open('w') as build:
    subprocess.run(['c++', '-std=c++17', '-O2', str(root/'oracle.cpp'), '-o', str(root/'oracle')], stdout=build, stderr=subprocess.STDOUT, check=True)
with (root/'expected.tsv').open('w') as output:
    subprocess.run([str(root/'oracle')], stdout=output, check=True)
runner = args.runner.resolve()
command = (['dotnet', str(runner)] if runner.suffix == '.dll' else [str(runner)]) + ['--pseudo-oracle', str(root/'expected.tsv')]
env = {k: v for k, v in os.environ.items() if not k.startswith('ELECTRON2D_TEST_')}
env.update(VK_DRIVER_FILES=str(root/'no-gpu.json'), VK_ICD_FILENAMES=str(root/'no-gpu.json'), SDL_VIDEODRIVER='dummy', DOTNET_TieredCompilation='0')
with (root/'runner.log').open('w') as log:
    result = subprocess.run(command, env=env, stdout=log, stderr=subprocess.STDOUT)
record = dict(command=command, exit=result.returncode, sourceSHA256=sha(args.source), generatedSHA256=sha(root/'oracle.cpp'), records=sum(1 for _ in (root/'expected.tsv').open()), expectedSHA256=sha(root/'expected.tsv'), binarySHA256=sha(runner))
(root/'check.json').write_text(json.dumps(record, indent=2)+'\n')
assert result.returncode == 0, (root/'runner.log').read_text()[-2000:]
print(json.dumps(record))
