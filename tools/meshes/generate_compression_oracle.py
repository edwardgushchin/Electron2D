#!/usr/bin/env python3
"""Regenerate deterministic mesh frame fixtures from immutable C++ reference functions.

Only the extracted axis-angle routines execute; float Vector/Math shims isolate
that numerical contract from renderer/scene behavior. Requires g++ and network.
Reference source is MIT licensed; no external runtime dependency is introduced.
"""
import hashlib
import tempfile
import urllib.request
COMMIT = "ed1daf0bf001b61586d9930840f2f1394092c079"
import json,subprocess,math,struct
from pathlib import Path
def fetch(path, digest):
    data = urllib.request.urlopen(f"https://raw.githubusercontent.com/godotengine/godot/{COMMIT}/{path}", timeout=30).read()
    if hashlib.sha256(data).hexdigest() != digest:
        raise ValueError("Pinned reference digest differs: " + path)
    return data.decode()
basis = fetch("core/math/basis.cpp", "07b6d2d2e43b6a738d76b3c860fd21863ae9b480fcc9e91bd16f6bd2d3053441")
basis = basis[basis.index('void Basis::get_axis_angle('):basis.index('\nvoid Basis::set_quaternion(', basis.index('void Basis::get_axis_angle('))]
server = fetch("servers/rendering/rendering_server.cpp", "b108f62d9d6113061dd1261e7bef51515fbdba9080b4393245e84cecc67ad7ca")
convert = server[server.index('void _get_axis_angle('):server.index('// The inputs to this function', server.index('void _get_axis_angle('))]
work = tempfile.TemporaryDirectory(prefix="e2d-compression-oracle-")
cpp_path = Path(work.name) / "oracle.cpp"
exe_path = Path(work.name) / "oracle"
prefix=r'''
#include <cmath>
#include <algorithm>
#include <iostream>
#include <cstdint>
using real_t=float;
constexpr float CMP_EPSILON=.00001f;
namespace Math {constexpr float PI=3.14159265358979323846f,SQRT12=.7071067811865475244f; template<class T> auto abs(T v){return std::abs(v);} template<class T> auto sqrt(T v){return std::sqrt(v);} inline float acos(float v){return std::acos(std::clamp(v,-1.f,1.f));} inline bool is_zero_approx(float v){return abs(v)<CMP_EPSILON;}}
#define CLAMP(v,l,h) std::clamp(float(v),float(l),float(h))
struct Vector3 {float x=0,y=0,z=0;Vector3(){}Vector3(float a,float b,float c):x(a),y(b),z(c){} float&operator[](int i){return (&x)[i];}float operator[](int i)const{return (&x)[i];} Vector3 normalized()const {float d=std::sqrt(x*x+y*y+z*z);return d==0?Vector3():Vector3(x/d,y/d,z/d);} Vector3 cross(Vector3 b)const{return Vector3(y*b.z-z*b.y,z*b.x-x*b.z,x*b.y-y*b.x);}};
struct Vector4 {float x,y,z,w;};
struct Basis {Vector3 rows[3];void get_axis_angle(Vector3&axis,real_t&angle)const;bool is_diagonal()const{return Math::is_zero_approx(rows[0][1])&&Math::is_zero_approx(rows[0][2])&&Math::is_zero_approx(rows[1][0])&&Math::is_zero_approx(rows[1][2])&&Math::is_zero_approx(rows[2][0])&&Math::is_zero_approx(rows[2][1]);}};
'''
main=r'''
uint32_t oct(Vector3 v) {float l=std::abs(v.x)+std::abs(v.y)+std::abs(v.z);v=Vector3(v.x/l,v.y/l,v.z/l);float x=v.x,y=v.y;if(v.z<0){x=(1-std::abs(v.y))*(v.x>=0?1:-1);y=(1-std::abs(v.x))*(v.y>=0?1:-1);}float u=x*.5f+.5f,w=y*.5f+.5f;return uint16_t(CLAMP(u*65535,0,65535))|(uint32_t(uint16_t(CLAMP(w*65535,0,65535)))<<16);}
int main(){Vector3 n;Vector4 t;while(std::cin>>n.x>>n.y>>n.z>>t.x>>t.y>>t.z>>t.w){Vector3 axis;float angle;_get_axis_angle(n,t,angle,axis);std::cout<<oct(axis)<<" "<<uint16_t(CLAMP(angle*65535,0,65535))<<"\n";}}
'''
cpp=cpp_path;cpp.write_text(prefix+basis+'\n'+convert+main);subprocess.run(['g++','-std=c++17','-O2',str(cpp),'-o',str(exe_path)],check=True)
def f(v):return struct.unpack('f',struct.pack('f',v))[0]
records=[]
for axis in [(1,0,0),(0,1,0),(0,0,1),(1,2,3),(-2,1,1)]:
 l=math.sqrt(sum(v*v for v in axis));x,y,z=[v/l for v in axis]
 for angle in [0,.0001,.2,.8,1.7,math.pi-.0001,math.pi]:
  co=math.cos(angle);si=math.sin(angle);u=1-co;t=[x*x+co*(1-x*x),x*y*u-z*si,x*z*u+y*si];n=[x*z*u-y*si,y*z*u+x*si,z*z+co*(1-z*z)]
  for sign in [-1,1]:records.append({'normal':[f(v) for v in n],'tangent':[f(v) for v in t]+[sign]})
inputs=''.join(' '.join(map(str,r['normal']+r['tangent']))+'\n' for r in records);output=subprocess.check_output([str(exe_path)],input=inputs,text=True).splitlines()
for r,line in zip(records,output):r['axis_word'],r['angle_word']=map(int,line.split())
path=(Path(__file__).resolve().parents[2] / 'tests/Electron2D.Tests/Fixtures/MeshCompressionOracle.json');path.parent.mkdir(exist_ok=True);path.write_text(json.dumps({'source_commit':'ed1daf0bf001b61586d9930840f2f1394092c079','records':records},indent=2)+'\n');print('Pinned C++ frame fixtures',len(records))
