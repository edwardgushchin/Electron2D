#version 450
layout(local_size_x=4,local_size_y=4,local_size_z=1) in;
layout(set=0,binding=0,rgba32f) uniform image2DArray destination;
layout(set=0,binding=1,r32ui) uniform uimage2DArray counter;
layout(set=0,binding=2) uniform sampler2DArray source;
layout(set=0,binding=3,std430) buffer Results { uvec4 values[]; };
void main(){ivec3 p=ivec3(gl_GlobalInvocationID);ivec3 size=imageSize(destination);if(any(greaterThanEqual(p,size)))return;
 uint prior=imageAtomicAdd(counter,ivec3(p.xy,2),1u);float stored=uintBitsToFloat(imageLoad(counter,ivec3(p.xy,1)).r);
 imageStore(destination,p,vec4(textureLod(source,vec3(0.5,0.5,0),1).r,float(p.z+1),stored,1));
 uint index=uint((p.z*size.y+p.y)*size.x+p.x);values[index]=uvec4(prior,uint(size.z),uint(imageSize(counter).z),uint(textureQueryLevels(source)));}
