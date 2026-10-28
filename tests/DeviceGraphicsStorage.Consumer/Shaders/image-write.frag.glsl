#version 450
layout(set=0,binding=1,r32ui) uniform uimage2D second;
layout(push_constant) uniform Push {uint step;};
layout(location=0) out vec4 color;
void main(){ivec2 p=ivec2(gl_FragCoord.xy);imageStore(second,p,uvec4(step+100u+uint(p.x)+4u*uint(p.y)));color=vec4(1,.5,.25,1);}
