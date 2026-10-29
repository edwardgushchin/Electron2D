from pathlib import Path
import re,subprocess,hashlib,json,sys,tempfile
if len(sys.argv) != 2: raise SystemExit("Supply the directory containing pinned animation.cpp, math_funcs.h and math_defs.h.")
proof=Path(sys.argv[1]);out=Path(__file__).parent
expected=json.loads((out/'sources.json').read_text())['sha256']
for name,digest in expected.items():
 if hashlib.sha256((proof/name).read_bytes()).hexdigest()!=digest:raise SystemExit("Pinned source digest mismatch: "+name)

s=(proof/'animation.cpp').read_text()
def function(text,name):
 a=text.index(name);b=text.index('{',a);depth=1;i=b+1
 while depth:
  depth+=(text[i]=='{')-(text[i]=='}');i+=1
 return text[a:i]
walk=function(s,'void Animation::track_get_key_indices_in_range(')
for m in reversed(list(re.finditer(r'(?m)^\s*case (TYPE_[A-Z0-9_]+): \{',walk))):
 if m[1] in {'TYPE_METHOD','TYPE_AUDIO','TYPE_ANIMATION'}:continue
 b=walk.index('{',m.start());depth=1;i=b+1
 while depth:depth+=(walk[i]=='{')-(walk[i]=='}');i+=1
 tail=re.match(r' break;',walk[i:]);assert tail
 walk=walk[:m.start()]+walk[i+len(tail[0]):]
keys='template <typename T>\n'+function(s,'void Animation::_track_get_key_indices_in_range(')
header=s[:s.index('#include')]
math=(proof/'math_funcs.h').read_text()
compare=function(math,'_ALWAYS_INLINE_ bool is_equal_approx(double p_left, double p_right)')
ping=function(math,'_ALWAYS_INLINE_ double pingpong(double p_value, double p_length)')
preamble=r'''
#include <algorithm>
#include <cmath>
#include <cstdint>
#include <iomanip>
#include <iostream>
#include <stdexcept>
#include <vector>
#define _ALWAYS_INLINE_ inline
#define CMP_EPSILON 0.00001
#define SWAP(a,b) std::swap(a,b)
#define ERR_FAIL_UNSIGNED_INDEX(i,n) if ((uint32_t)(i)>=(n)) throw std::out_of_range("track")
template <typename T> using LocalVector = std::vector<T>;
namespace Math {
using std::abs;
inline double fposmod(double x,double y) { double r=std::fmod(x,y);return r<0?r+y:r; }
inline double fract(double x) { return x-std::floor(x); }
'''
classes=r'''
}
class Animation {
public:
enum LoopedFlag {LOOPED_FLAG_NONE,LOOPED_FLAG_END,LOOPED_FLAG_START};
enum {LOOP_NONE,LOOP_LINEAR,LOOP_PINGPONG,TYPE_METHOD,TYPE_AUDIO,TYPE_ANIMATION,FIND_MODE_EXACT};
struct Key { double time; };
struct Track {int type;};
struct MethodTrack:Track {LocalVector<Key> methods;};
struct AudioTrack:Track {LocalVector<Key> values;};
struct AnimationTrack:Track {LocalVector<Key> values;};
LocalVector<Track*> tracks;
double length=1;int loop_mode=LOOP_NONE;
int track_find_key(int i,double time,int) const {auto &v=static_cast<MethodTrack*>(tracks[i])->methods;for(int k=0;k<(int)v.size();k++)if(v[k].time==time)return k;return -1;}
template <typename T> void _track_get_key_indices_in_range(const LocalVector<T>&,double,double,LocalVector<int>*,bool) const;
void track_get_key_indices_in_range(int,double,double,double,double,LocalVector<int>*,LoopedFlag) const;
};
'''
main=r'''
int main(){
 const double positions[]={-2,-.25,0,.000005,.25,.5,.75,.999995,1,1.000005,1.25,2.5};
 const double deltas[]={-2,-1.25,-.5,-.25,-.000001,0,.000001,.25,.5,1.25,2};
 const double keyTimes[]={0,.000001,.249999,.25,.250001,.5,.75,.999999,1,1.000001,1.25};
 std::cout<<std::setprecision(17);
 for(double length:{1.,2.5,.001})for(int loop=0;loop<3;loop++)for(int flag=0;flag<3;flag++)for(double p:positions)for(double d:deltas){
 Animation a;a.length=length;a.loop_mode=loop;Animation::MethodTrack t;t.type=Animation::TYPE_METHOD;
 for(double k:keyTimes)t.methods.push_back({k*length});a.tracks.push_back(&t);LocalVector<int> result;
 a.track_get_key_indices_in_range(0,p*length,d*length,0,length,&result,(Animation::LoopedFlag)flag);
 std::cout<<length<<'\t'<<loop<<'\t'<<flag<<'\t'<<p*length<<'\t'<<d*length<<'\t';
 if(result.size()==0)std::cout<<'-';
 for(size_t i=0;i<result.size();i++){if(i)std::cout<<',';std::cout<<result[i];}std::cout<<'\n';
 }
}
'''
(out/'oracle.cpp').write_text(header+preamble+compare+'\n'+ping+classes+keys+'\n'+walk+'\n'+main)
with tempfile.TemporaryDirectory(prefix='e2d-animation-endpoints-') as tmp:
 executable=str(Path(tmp)/'oracle')
 subprocess.run(['c++','-std=c++17','-O2',str(out/'oracle.cpp'),'-o',executable],check=True)
 r=subprocess.check_output([executable])
(out/'reference.tsv').write_bytes(r)
print('reference vectors',len(r.splitlines()))
