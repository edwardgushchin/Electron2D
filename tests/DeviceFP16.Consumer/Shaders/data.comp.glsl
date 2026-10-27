#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_float16:require
#extension GL_EXT_shader_16bit_storage:require
layout(local_size_x=1)in;
struct Pair { float16_t x; float16_t y; };
layout(set=0,binding=0,std430)readonly buffer Source { Pair values[]; } sourceData;
layout(set=0,binding=1,std430)buffer Target { float16_t values[]; } targetData;
layout(set=1,binding=0,std140)uniform Parameters { f16vec2 scale; float16_t delta; } parameters;
layout(constant_id=0)const float16_t factor=float16_t(0.75);
layout(constant_id=1)const bool choose=true;
layout(constant_id=2)const float convertSource=0.333251953125;
layout(push_constant,std430)uniform Push {
 layout(offset=0)float16_t a; layout(offset=2)float16_t b;
 layout(offset=4)f16vec2 pair; layout(offset=8)float16_t values[3];
 layout(column_major,offset=16)f16mat2x3 column;
 layout(offset=32)Pair nested; layout(offset=36)uint index;
 layout(row_major,offset=40)f16mat2x3 row;
 layout(offset=52)float16_t tail[2]; layout(offset=126)float16_t last;
}pc;
float16_t pairValue(Pair p){return p.x+p.y;}
float16_t vectorValue(f16vec2 v,uint i){return v[i];}
void main(){
 uint i=gl_GlobalInvocationID.x,j=i*16;float16_t x=sourceData.values[i].x,y=sourceData.values[i].y;
 targetData.values[j]=x+factor+pc.a;targetData.values[j+1]=y*parameters.scale.x+vectorValue(pc.pair,i&1);
 targetData.values[j+2]=pc.column[pc.index][i%3];targetData.values[j+3]=pc.row[pc.index][i%3];
 targetData.values[j+4]=pc.values[i%3];targetData.values[j+5]=pairValue(pc.nested);targetData.values[j+6]=pc.tail[i&1];targetData.values[j+7]=pc.last;
 targetData.values[j+8]=x;targetData.values[j+9]=y;targetData.values[j+10]=factor;targetData.values[j+11]=choose?x:y;
 targetData.values[j+12]=parameters.scale.y+parameters.delta;targetData.values[j+13]=float16_t(convertSource);targetData.values[j+14]=pc.a;targetData.values[j+15]=pc.b;
}
