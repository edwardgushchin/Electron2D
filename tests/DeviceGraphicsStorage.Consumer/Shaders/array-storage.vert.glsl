#version 450
layout(constant_id=0) const uint factor=7;
layout(set=0,binding=0,rgba32f) uniform image2DArray destinations[2];
layout(set=0,binding=1,r32ui) uniform uimage2DArray unsignedImages[2];
layout(set=0,binding=2,r32i) uniform iimage2DArray signedImages[2];
layout(set=0,binding=3) uniform sampler2DArray sampled;
layout(push_constant) uniform Push {uint selected;};

void main(){for(int layer=0;layer<3;layer++){ivec3 at=ivec3(gl_VertexIndex,0,layer);uint u=imageAtomicAdd(unsignedImages[selected],at,1u*factor);int s=imageAtomicAdd(signedImages[selected],at,-1*2*int(factor));imageStore(destinations[selected],at,vec4(texelFetch(sampled,ivec3(0,0,0),1).r,float(imageLoad(unsignedImages[selected],at).r+u),float(imageLoad(signedImages[selected],at).r+s),float(imageSize(destinations[selected]).z)));}vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);}
