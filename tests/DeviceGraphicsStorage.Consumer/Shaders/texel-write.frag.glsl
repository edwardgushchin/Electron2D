#version 450
layout(set=3,binding=7,rgba8)uniform imageBuffer texels;
layout(set=4,binding=2,r32ui)uniform coherent uimageBuffer counter;
layout(push_constant)uniform Push{uint step;}pc;
layout(location=0)out vec4 color;
void main(){int i=int(gl_FragCoord.x)+int(gl_FragCoord.y)*4;imageStore(texels,4+i,pc.step==1?vec4(1,0,0,1):vec4(0,1,.5,1));imageStore(texels,-1,vec4(1));imageStore(texels,imageSize(texels),vec4(1));imageAtomicAdd(counter,1,pc.step);color=vec4(float(imageSize(texels))/40,1,.5,1);}
