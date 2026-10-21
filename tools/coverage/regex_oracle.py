#!/usr/bin/env python3
"""Generate comparison vectors through a separately compiled, unwrapped PCRE2-32."""
import argparse
import ctypes as C
import json
from pathlib import Path
import random
import subprocess

ROOT = Path(__file__).resolve().parents[2]
U32 = C.POINTER(C.c_uint32)
SIZE = C.c_size_t

def main():
    parser = argparse.ArgumentParser(); parser.add_argument('output', type=Path); args = parser.parse_args(); args.output.parent.mkdir(parents=True, exist_ok=True)
    manifest = json.loads((ROOT / 'src/Vendor/PCRE2/manifest.json').read_text()); build = args.output.parent / 'oracle'; build.mkdir(exist_ok=True)
    include = build / 'include'; include.mkdir(exist_ok=True)
    source = ROOT / 'src/Vendor/PCRE2'
    for original, target in [('src/config.h.generic','config.h'), ('src/pcre2.h.generic','pcre2.h'), ('src/pcre2_chartables.c.dist','pcre2_chartables.c')]:
        (include / target).write_bytes((source / original).read_bytes())
    library = build / 'libpcre2-oracle.so'
    command = ['cc','-shared','-fPIC','-O2','-DHAVE_CONFIG_H','-DPCRE2_STATIC','-DPCRE2_CODE_UNIT_WIDTH=32','-DSUPPORT_PCRE2_32','-DSUPPORT_UNICODE','-I'+str(include),'-I'+str(source/'src')]+[str(source/name) for name in manifest['runtimeSources']]+[str(include/'pcre2_chartables.c'),'-o',str(library)]
    subprocess.run(command,check=True); lib = C.CDLL(str(library))
    def bind(name, result, *arguments):
        value = getattr(lib, name+'_32'); value.restype = result; value.argtypes = arguments; return value
    compile = bind('pcre2_compile',C.c_void_p,U32,SIZE,C.c_uint32,C.POINTER(C.c_int),C.POINTER(SIZE),C.c_void_p)
    info = bind('pcre2_pattern_info',C.c_int,C.c_void_p,C.c_uint32,C.c_void_p)
    create_data = bind('pcre2_match_data_create_from_pattern',C.c_void_p,C.c_void_p,C.c_void_p)
    match = bind('pcre2_match',C.c_int,C.c_void_p,U32,SIZE,SIZE,C.c_uint32,C.c_void_p,C.c_void_p)
    vector = bind('pcre2_get_ovector_pointer',C.POINTER(SIZE),C.c_void_p)
    sub = bind('pcre2_substitute',C.c_int,C.c_void_p,U32,SIZE,SIZE,C.c_uint32,C.c_void_p,C.c_void_p,U32,SIZE,U32,C.POINTER(SIZE))
    free_data = bind('pcre2_match_data_free',None,C.c_void_p); free = bind('pcre2_code_free',None,C.c_void_p)
    def text(s): return (C.c_uint32*max(1,len(s)))(*[ord(x) for x in s])
    cases=[]
    def add(pattern, subject, offset=0, end=-1, replacement='$0', all=True): cases.append(dict(pattern=pattern,subject=subject,offset=offset,end=end,replacement=replacement,all=all))
    for pattern,subject in [
        ('(?<named>a)(b)(?<last>c)','abc'), ('(?:(?<piece>a)|(?<piece>b))(?<tail>c)?','b'), ('(?|(a)|(b))','b'),
        (r'(?<pair>\((?:[^()]++|(?&pair))*\))','x(a(b)c)!'), ('a++a','aaa'), (r'"skip"(*SKIP)(*F)|\w+','yes "skip" next'),
        (r'(?(DEFINE)(?<item>a+))(?&item)b','aaab'), ('(?<=a{1,3})b','aaab'), ('a\\Kb','ab'),
        (r'(*UTF)\X','A😀e\u0301'), ('(*UCP)\\w+','ёж42'), ('\\w+','ёж42'), ('^|$','ab'), ('','ab'), ('(?:|a)','a'),
        ('(?m)^x$','x\nx'), ('a\x00b','!a\x00b!'), ('.','\ud800x'), ('(*UTF).','\ud800x'), ('(*MARK:tag)a','a'),
        ('(?<bad','x'), ('(?<name>a)','a')]: add(pattern,subject)
    for replacement in ['$0+','$1$$','${key}/${missing}','$&',r'\n','$','${key}']:
        for all in [False,True]:add('(?<key>a)(b)?','a ab!',replacement=replacement,all=all)
    add('x','😀x😀x',1,3,'matched');add('^a','xa',1);add('$','xabcZ',1,4,'END');add('','',replacement='-');add('','ab',replacement='-')
    rng=random.Random(731)
    patterns=['.',r'(\w+)',r'(*UCP)(?<word>\w+)',r'(?<a>a)?(b*)','a|b',r'(?=.)',r'[^x]+','(?<dup>a)|(?<dup>b)','(a)(?<n>b)?',r'\p{L}+']
    for _ in range(256):
        subject=''.join(rng.choice('abx 😀ёж\x00\n') for _ in range(rng.randrange(0,15)));end=rng.choice([-1,0,2,len(subject),len(subject)+3]);add(rng.choice(patterns),subject,rng.randrange(len(subject)+3),end,rng.choice(['$0+','${missing}','$1$$','X']),rng.choice([False,True]))
    for item in cases:
        pattern,subject=item['pattern'],item['subject'];error=C.c_int();at=SIZE();code=compile(text(pattern),len(pattern),0x40,C.byref(error),C.byref(at),None)
        if not code:item['compileError']=error.value;continue
        try:
            groups=C.c_uint32();names=C.c_uint32();width=C.c_uint32();table=U32()
            for key,target in [(4,groups),(17,names),(18,width),(19,table)]:assert info(code,key,C.byref(target))==0
            declared=[(''.join(chr(table[i*width.value+j]) for j in range(1,width.value) if table[i*width.value+j]),int(table[i*width.value])) for i in range(names.value)]
            item['groups']=groups.value;item['names']=list(dict.fromkeys(name for name,index in declared));item['matches']=[];limit=len(subject) if item['end']<0 else min(item['end'],len(subject));offset=item['offset'];data=create_data(code,None)
            try:
                while offset<=limit:
                    status=match(code,text(subject),limit,offset,0,data,None)
                    if status == -1:break
                    if status<0:item['matchError']=status;break
                    positions=vector(data);captures=[]
                    for i in range(groups.value+1):
                        start,finish=positions[2*i],positions[2*i+1];captures.append([-1,-1,''] if start==SIZE(-1).value else [start,finish,subject[start:finish]])
                    matched={}
                    for name,index in declared:
                        if captures[index][0]>=0:matched.setdefault(name,index)
                    item['matches'].append(dict(captures=captures,names=matched));offset=captures[0][1]+(captures[0][0]==captures[0][1])
                capacity=SIZE(max(1,len(subject)+1));output=(C.c_uint32*capacity.value)();replacement=item['replacement'];flags=0x1000|0x400|0x800|(0x100 if item['all'] else 0)
                status=sub(code,text(subject),limit,item['offset'],flags,data,None,text(replacement),len(replacement),output,C.byref(capacity))
                if status == -48:
                    output=(C.c_uint32*capacity.value)();status=sub(code,text(subject),limit,item['offset'],flags,data,None,text(replacement),len(replacement),output,C.byref(capacity))
                if status<0:item['subError']=status
                else:item['sub']=''.join(chr(output[i]) for i in range(capacity.value))+subject[limit:]
            finally:free_data(data)
        finally:free(code)
    def encode(value):
        if isinstance(value,str) and any(0xD800<=ord(c)<=0xDFFF for c in value):return [ord(c) for c in value]
        if isinstance(value,list):return [encode(x) for x in value]
        if isinstance(value,dict):return {k:encode(v) for k,v in value.items()}
        return value
    args.output.write_text(json.dumps(encode(cases),ensure_ascii=True,indent=2)+'\n');print('Direct PCRE2-32 oracle vectors:',len(cases))

if __name__=='__main__':main()
