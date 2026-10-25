#include "line_builder.h"
#include <iostream>
#include <cstdint>
template<class T>T read(){T value{};std::cin.read(reinterpret_cast<char*>(&value),sizeof(T));if(!std::cin)throw std::runtime_error("truncated fixture");return value;}
template<class T>void write(T value){std::cout.write(reinterpret_cast<const char*>(&value),sizeof(T));}
int main(){
 auto cases=read<uint32_t>();write(cases);
 for(uint32_t n=0;n<cases;n++){
  LineBuilder line;auto count=read<int32_t>();line.joint_mode=static_cast<Line2D::LineJointMode>(read<int32_t>());line.begin_cap_mode=static_cast<Line2D::LineCapMode>(read<int32_t>());line.end_cap_mode=static_cast<Line2D::LineCapMode>(read<int32_t>());line.texture_mode=static_cast<Line2D::LineTextureMode>(read<int32_t>());line.closed=read<int32_t>()!=0;bool curve=read<int32_t>()!=0,gradient=read<int32_t>()!=0;line.round_precision=read<int32_t>();line.sharp_limit=read<float>();line.tile_aspect=read<float>();line.width=8;line.default_color=Color(.6f,.3f,.8f,.5f);Curve c;Gradient g;if(curve)line.curve=&c;if(gradient)line.gradient=&g;
  for(int i=0;i<count;i++){float x=read<float>(),y=read<float>();line.points.push_back(Vector2(x,y));}line.build();write(static_cast<uint32_t>(line.indices.size()));
  for(int i:line.indices){auto p=line.vertices[i];auto uv=line.uvs.empty()?Vector2():line.uvs[i];auto color=line.colors.size()==1?line.colors[0]:line.colors[i];for(float v:{p.x,p.y,uv.x,uv.y,color.r,color.g,color.b,color.a})write(v);}
 }
}
