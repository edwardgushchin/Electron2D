#version 450
#extension GL_EXT_shader_16bit_storage:require
layout(local_size_x=1)in;
layout(set=0,binding=0,std430)readonly buffer Source { int16_t values[]; } sourceData;
layout(set=0,binding=1,std430)buffer Target { int16_t values[]; } targetData;
void main(){uint i=gl_GlobalInvocationID.x;targetData.values[i]=sourceData.values[i];}
