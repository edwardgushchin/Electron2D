#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_int8:require
#extension GL_EXT_shader_8bit_storage:require
#extension GL_EXT_shader_16bit_storage:require
#extension GL_EXT_shader_explicit_arithmetic_types_int16:require
#extension GL_EXT_shader_explicit_arithmetic_types_float16:require
layout(local_size_x=1)in;
struct Pair { int8_t x; uint8_t y; };
layout(set=0,binding=0,std430)readonly buffer Source { Pair values[]; } sourceData;
layout(set=0,binding=1,std430)buffer Target { uint values[]; } targetData;
layout(set=1,binding=0,std140)uniform Parameters { uint8_t scale; uint8_t delta; float16_t halfValue; int16_t wideValue; } parameters;
layout(constant_id=0)const int8_t factor=int8_t(-7);
layout(constant_id=1)const uint8_t limit=uint8_t(5);
layout(constant_id=2)const uint8_t length=uint8_t(3);
layout(push_constant,std430)uniform Push {
 layout(offset=0)int8_t a; layout(offset=1)uint8_t b; layout(offset=2)int8_t c; layout(offset=3)uint8_t d; layout(offset=4)u8vec4 pair;
 layout(offset=8)int8_t values[uint(length)]; layout(offset=12)Pair nested; layout(offset=16)int16_t wideValue; layout(offset=18)float16_t halfValue; layout(offset=127)uint8_t last;
}pc;
int8_t pairValue(Pair p){return int8_t(p.x+int8_t(p.y));}
uint8_t vectorValue(u8vec4 v,uint i){return v[i];}
void main(){
 uint i=gl_GlobalInvocationID.x,j=i*16;int8_t x=sourceData.values[i].x;uint8_t y=sourceData.values[i].y;
 targetData.values[j]=uint(int(x+factor));targetData.values[j+1]=uint(uint8_t(y+limit));
 targetData.values[j+2]=uint(int(x/int8_t(3)));targetData.values[j+3]=uint(int(int8_t(y&uint8_t(127))%int8_t(3)));
 targetData.values[j+4]=uint(int(x>>3));targetData.values[j+5]=uint(uint8_t(y<<3));
 targetData.values[j+6]=uint(y^limit);targetData.values[j+7]=x<int8_t(0)?1:0;
 targetData.values[j+8]=uint(int(pc.a)+int(pc.c));targetData.values[j+9]=uint(pc.b)+uint(pc.d);targetData.values[j+10]=uint(vectorValue(pc.pair,i&3));
 targetData.values[j+11]=uint(int(pc.values[uint8_t(i%3)]));targetData.values[j+12]=uint(int(pairValue(pc.nested))+int(pc.wideValue)+int(pc.halfValue*float16_t(8)));targetData.values[j+13]=uint(pc.last);
 targetData.values[j+14]=uint(int(x)+int(parameters.wideValue)+int(parameters.halfValue*float16_t(8)));targetData.values[j+15]=uint(uint8_t(y+parameters.delta));
}
