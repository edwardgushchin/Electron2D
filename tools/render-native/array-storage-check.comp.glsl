#version 450
layout(local_size_x=1,local_size_y=1,local_size_z=1) in;
layout(set=0,binding=0) uniform sampler2DArray shared_source;
layout(set=0,binding=1) uniform sampler2DArray independent_source;
layout(set=1,binding=0,rgba8) uniform writeonly image2DArray destination;
layout(set=1,binding=1,rgba8) uniform writeonly image2DArray same_storage;
void main(){vec4 color=texelFetch(shared_source,ivec3(0,0,0),1)+texelFetch(independent_source,ivec3(0,0,0),1);imageStore(destination,ivec3(0,0,2),color);imageStore(same_storage,ivec3(0,0,1),color);}
