#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_int64:require
#extension GL_EXT_shader_atomic_int64:require
layout(local_size_x=64) in;
layout(set=0,binding=0,std430) buffer Data { uint64_t values[3]; } data;
shared uint64_t count;
void main(){
 uint i=gl_LocalInvocationID.x;
 if(i==0){count=0ul;data.values[0]=0ul;}
 memoryBarrierBuffer();barrier();
 atomicAdd(count,1ul);atomicAdd(data.values[0],1ul);
 memoryBarrierBuffer();barrier();
 if(i==0){data.values[1]=count;data.values[2]=data.values[0];}
}
