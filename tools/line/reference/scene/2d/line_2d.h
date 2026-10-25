#pragma once
#include <vector>
#include <cmath>
#include <algorithm>
#include <stdexcept>
using real_t = float;
template<class T> struct Vector : std::vector<T> { using std::vector<T>::vector; int size()const{return static_cast<int>(std::vector<T>::size());} };
struct Vector2 {
 float x=0,y=0; Vector2()=default; Vector2(float a,float b):x(a),y(b){}
 Vector2 operator+(Vector2 v)const{return{x+v.x,y+v.y};} Vector2 operator-(Vector2 v)const{return{x-v.x,y-v.y};} Vector2 operator-()const{return{-x,-y};}
 Vector2 operator*(float v)const{return{x*v,y*v};} Vector2 operator/(float v)const{return{x/v,y/v};}
 Vector2&operator+=(Vector2 v){x+=v.x;y+=v.y;return *this;} Vector2&operator-=(Vector2 v){x-=v.x;y-=v.y;return *this;}
 friend Vector2 operator*(float s,Vector2 v){return v*s;}
 float dot(Vector2 v)const{return x*v.x+y*v.y;}float cross(Vector2 v)const{return x*v.y-y*v.x;}float length()const{return std::sqrt(dot(*this));}
 float distance_to(Vector2 v)const{return(*this-v).length();}float distance_squared_to(Vector2 v)const{auto d=*this-v;return d.dot(d);}
 Vector2 normalized()const{float n=length();return n==0?Vector2():*this/n;}Vector2 orthogonal()const{return{y,-x};}
 Vector2 lerp(Vector2 v,float t)const{return *this+(v-*this)*t;}float angle_to(Vector2 v)const{return std::atan2(cross(v),dot(v));}
};
struct Color {float r=0,g=0,b=0,a=1;Color()=default;Color(float R,float G,float B,float A=1):r(R),g(G),b(B),a(A){} };
struct Rect2 {Vector2 position,size;Rect2(float x,float y,float w,float h):position(x,y),size(w,h){}Vector2 get_size()const{return size;} };
namespace Math {
 constexpr double PI=3.14159265358979323846; using std::cos;using std::sin;using std::atan2;using std::abs;
 inline float lerp(float a,float b,float t){return a+(b-a)*t;}
 inline bool is_zero_approx(float v){return std::abs(v)<.00001f;}
 inline bool is_equal_approx(float a,float b){return a==b || std::abs(a-b)<.00001f*std::max(1.f,std::abs(a));}
}
#define CMP_EPSILON 0.00001f
#define ERR_FAIL_COND(c) if(c)throw std::runtime_error("invalid reference input")
struct Curve {float sample_baked(float t)const{return 1-.75f*std::clamp(t,0.f,1.f);} };
struct Gradient {
 int get_point_count()const{return 2;}Color get_color(int i)const{return i==0?Color(.2f,.7f,.3f,.4f):Color(.9f,.1f,.6f,.8f);}
 Color get_color_at_offset(float t)const{t=std::clamp(t,0.f,1.f);auto a=get_color(0),b=get_color(1);return{Math::lerp(a.r,b.r,t),Math::lerp(a.g,b.g,t),Math::lerp(a.b,b.b,t),Math::lerp(a.a,b.a,t)};}
};
struct Line2D {enum LineJointMode{LINE_JOINT_SHARP,LINE_JOINT_BEVEL,LINE_JOINT_ROUND};enum LineCapMode{LINE_CAP_NONE,LINE_CAP_BOX,LINE_CAP_ROUND};enum LineTextureMode{LINE_TEXTURE_NONE,LINE_TEXTURE_TILE,LINE_TEXTURE_STRETCH};};
