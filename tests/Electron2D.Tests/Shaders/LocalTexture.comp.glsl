#version 450
layout(local_size_x=8,local_size_y=8) in;
layout(set=3,binding=7) uniform sampler2D source_image;
layout(set=5,binding=2,rgba32f) uniform image2D destination;
layout(set=9,binding=4,std430) buffer Counts { uint counts[]; };
void main() {
    ivec2 p=ivec2(gl_GlobalInvocationID.xy);
    if(any(greaterThanEqual(p,imageSize(destination)))) return;
    vec2 uv=(vec2(p)+.5)/vec2(imageSize(destination));
    imageStore(destination,p,textureLod(source_image,uv,0)+vec4(.25,0,0,0));
    atomicAdd(counts[0],1);
}
