#version 450
layout(local_size_x=1)in;
layout(set=0,binding=0) uniform usampler2D image0[2];
layout(set=1,binding=0,std430)buffer Output{vec4 result[];
};


vec4 forward_image(usampler2D image){return vec4(textureLod(image,vec2(0.75),0));
}
void main(){
uint selector=gl_GlobalInvocationID.x;
uint base=selector*8;
result[base]=vec4(textureLod(image0[selector],vec2(0.75),0));
result[base+1]=vec4(texelFetch(image0[selector],ivec2(0),0));
result[base+2]=vec4(textureGather(image0[selector],vec2(0.5),0));
result[base+3]=vec4(textureGather(image0[selector],vec2(0.5),1));
result[base+4]=vec4(textureGather(image0[selector],vec2(0.5),2));
result[base+5]=vec4(textureGather(image0[selector],vec2(0.5),3));
result[base+6]=vec4(vec2(textureSize(image0[selector],0)),textureQueryLevels(image0[selector]),0);
result[base+7]=forward_image(image0[selector]);
}
