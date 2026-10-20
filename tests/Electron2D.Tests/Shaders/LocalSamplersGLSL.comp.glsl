#version 450
#extension GL_EXT_samplerless_texture_functions : require
layout(local_size_x=1) in;
layout(set=3,binding=7) uniform texture2D first;
layout(set=4,binding=11) uniform texture2D second;
layout(set=6,binding=2) uniform texture2D fetch_only;
layout(set=8,binding=1) uniform utexture2D unsigned_image;
layout(set=10,binding=2) uniform itexture2D signed_image;
layout(set=7,binding=3) uniform texture2DArray layers;
layout(set=2,binding=5) uniform sampler point_filter;
layout(set=5,binding=13) uniform sampler linear_filter;
layout(set=9,binding=4,std430) buffer Results { vec4 values[]; };
vec4 read_image(texture2D image, sampler state, vec2 uv) { return textureLod(sampler2D(image,state),uv,0); }
vec4 choose_image(texture2D a,texture2D b,sampler x,sampler y,bool choose) { if(choose) return read_image(a,x,vec2(.5)); return read_image(b,y,vec2(.5)); }
void main() {
 values[0]=read_image(first,point_filter,vec2(.75,.25));
 values[1]=read_image(second,point_filter,vec2(.75,.25));
 values[2]=read_image(first,linear_filter,vec2(.5,.5));
 values[3]=texelFetch(fetch_only,ivec2(0),0);
 values[4]=vec4(textureSize(first,0),textureSize(fetch_only,0));
 values[5]=textureLod(sampler2DArray(layers,point_filter),vec3(.5,.5,1),0);
 values[6]=vec4(textureSize(layers,0),0);
 values[7]=choose_image(first,second,linear_filter,point_filter,values[2].r>.75);
 values[8]=vec4(textureLod(usampler2D(unsigned_image,point_filter),vec2(.5),0));
 values[9]=vec4(textureLod(isampler2D(signed_image,point_filter),vec2(.5),0));
}
