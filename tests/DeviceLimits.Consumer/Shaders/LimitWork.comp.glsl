#version 450
layout(local_size_x_id=0,local_size_y_id=1,local_size_z_id=2) in;
layout(constant_id=3) const uint shared_count=1;
shared uint scratch[shared_count];
layout(set=0,binding=0,std430) buffer Result { uint groups; uint sum; uint threads; uint padding; } result_data;
void main() {
    uint threads=gl_WorkGroupSize.x*gl_WorkGroupSize.y*gl_WorkGroupSize.z;
    for(uint i=gl_LocalInvocationIndex;i<shared_count;i+=threads) scratch[i]=i+1;
    barrier();
    if(gl_LocalInvocationIndex==0) {
        uint total=0;
        for(uint i=0;i<shared_count;i++) total+=scratch[i];
        atomicAdd(result_data.groups,1);
        atomicAdd(result_data.sum,total);
        atomicExchange(result_data.threads,threads);
    }
}
