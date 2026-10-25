#version 450
layout(local_size_x=1)in;
layout(set=0,binding=0) uniform itexture2D image0[2];
layout(set=0,binding=1) uniform sampler point0[2];
layout(set=1,binding=0,std430)buffer Output{vec4 result[];
};


vec4 forward_image(itexture2D image,sampler point){return vec4(textureLod(isampler2D(image,point),vec2(0.75),0));
}
void main(){
uint selector=gl_GlobalInvocationID.x;
uint base=selector*8;
result[base]=vec4(textureLod(isampler2D(image0[selector],point0[selector^1]),vec2(0.75),0));
result[base+1]=vec4(texelFetch(isampler2D(image0[selector],point0[selector^1]),ivec2(0),0));
result[base+2]=vec4(textureGather(isampler2D(image0[selector],point0[selector^1]),vec2(0.5),0));
result[base+3]=vec4(textureGather(isampler2D(image0[selector],point0[selector^1]),vec2(0.5),1));
result[base+4]=vec4(textureGather(isampler2D(image0[selector],point0[selector^1]),vec2(0.5),2));
result[base+5]=vec4(textureGather(isampler2D(image0[selector],point0[selector^1]),vec2(0.5),3));
result[base+6]=vec4(vec2(textureSize(isampler2D(image0[selector],point0[selector^1]),0)),textureQueryLevels(isampler2D(image0[selector],point0[selector^1])),0);
result[base+7]=forward_image(image0[selector],point0[selector^1]);
}
