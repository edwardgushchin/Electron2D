#version 450
#extension GL_EXT_nonuniform_qualifier:require
layout(set=3,binding=7,r32ui)uniform coherent uimageBuffer counters[2];
layout(location=0)out vec4 color;void main(){int slot=int(gl_FragCoord.x)&1;imageAtomicAdd(counters[nonuniformEXT(slot)],1,1);color=slot==0?vec4(1,0,0,1):vec4(0,1,0,1);}
