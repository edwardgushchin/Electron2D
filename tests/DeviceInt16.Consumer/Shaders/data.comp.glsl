#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_int16:require
#extension GL_EXT_shader_16bit_storage:require
layout(local_size_x=1)in;
struct Pair { int16_t x; uint16_t y; };
layout(set=0,binding=0,std430)readonly buffer Source { Pair values[]; } sourceData;
layout(set=0,binding=1,std430)buffer Target { uint values[]; } targetData;
layout(set=1,binding=0,std140)uniform Parameters { uint16_t scale; uint16_t delta; } parameters;
layout(constant_id=0)const int16_t factor=int16_t(-7);
layout(constant_id=1)const uint16_t limit=uint16_t(5);
layout(push_constant,std430)uniform Push {
 layout(offset=0)int16_t a; layout(offset=2)uint16_t b; layout(offset=4)u16vec2 pair;
 layout(offset=8)int16_t values[3]; layout(offset=16)Pair nested; layout(offset=126)uint16_t last;
}pc;
int16_t pairValue(Pair p){return int16_t(p.x+int16_t(p.y));}
uint16_t vectorValue(u16vec2 v,uint i){return v[i];}
void main(){
 uint i=gl_GlobalInvocationID.x,j=i*16;int16_t x=sourceData.values[i].x;uint16_t y=sourceData.values[i].y;
 targetData.values[j]=uint(int(x+factor));targetData.values[j+1]=uint(uint16_t(y+limit));
 targetData.values[j+2]=uint(int(x/int16_t(3)));targetData.values[j+3]=uint(int(int16_t(y&uint16_t(32767))%int16_t(3)));
 targetData.values[j+4]=uint(int(x>>3));targetData.values[j+5]=uint(uint16_t(y<<3));
 targetData.values[j+6]=uint(y^limit);targetData.values[j+7]=x<int16_t(0)?1:0;
 targetData.values[j+8]=uint(int(pc.a));targetData.values[j+9]=uint(pc.b);targetData.values[j+10]=uint(vectorValue(pc.pair,i&1));
 targetData.values[j+11]=uint(int(pc.values[uint16_t(i%3)]));targetData.values[j+12]=uint(int(pairValue(pc.nested)));targetData.values[j+13]=uint(pc.last);
 targetData.values[j+14]=uint(int(x+int16_t(parameters.scale)-int16_t(2)));targetData.values[j+15]=uint(uint16_t(y+parameters.delta));
}
