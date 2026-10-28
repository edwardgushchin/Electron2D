#version 450
layout(set=0,binding=1,r32ui) uniform uimage2D images[2];
layout(push_constant) uniform Push {uint selected;};
void main(){imageAtomicAdd(images[selected],ivec2(gl_FragCoord.xy),2);}
