#version 450
layout(set=0,binding=0,rgba32f) writeonly uniform image2D destination;
layout(set=0,binding=1,rgba32f) readonly uniform image2D sourceImage;
layout(set=0,binding=2) uniform sampler2D sampled;
layout(set=0,binding=3,std430) buffer Result {uint result[];};
layout(set=0,binding=4,std140) uniform Parameters {vec4 increment;};
layout(push_constant) uniform Push {uint step;};
layout(location=0) out vec4 color;
void main(){ivec2 p=ivec2(gl_FragCoord.xy);imageStore(destination,p,imageLoad(sourceImage,p)+texelFetch(sampled,p,0)+increment);result[p.x+4*p.y]=step+uint(p.x+4*p.y);color=vec4(0);}
