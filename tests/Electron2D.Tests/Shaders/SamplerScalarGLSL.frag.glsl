#version 450
#extension GL_EXT_nonuniform_qualifier : require
#define SLOT nonuniformEXT(slot)
layout(location=0) in vec4 color;
layout(location=0) out vec4 outputColor;
layout(set=2,binding=0) uniform sampler2D maps;
layout(set=2,binding=1) uniform sampler2DArray layers;
layout(set=2,binding=2) uniform isampler2D signedMaps;
layout(set=2,binding=3) uniform usampler2D unsignedMaps;
layout(set=3,binding=0,std140) uniform Material { int selected; int mode; };
vec4 readMap(sampler2D source, vec2 uv) { return texture(source,uv); }
void main() {
    int slot=selected;
    if(mode>=10) slot=int(gl_FragCoord.x)&1;
    vec2 uv=gl_FragCoord.xy*.125;
    vec4 value;
    if(mode==0 || mode==10) value=readMap(maps,uv);
    else if(mode==1) value=textureLod(maps,uv,0);
    else if(mode==2) value=texelFetch(maps,ivec2(selected==0?1:3),0);
    else if(mode==3) value=vec4(vec2(textureSize(maps,0))/8,0,1);
    else if(mode==4) value=textureGrad(maps,uv,vec2(.125,0),vec2(0,.125));
    else if(mode==5) value=vec4(textureGather(maps,uv,0).xyz,1);
    else if(mode==6) value=textureLod(layers,vec3(uv,float(selected)),0);
    else if(mode==7) value=vec4(float(texelFetch(signedMaps,ivec2(0),0).x)*.125+.5,0,0,1);
    else if(mode==8) value=vec4(float(texelFetch(unsignedMaps,ivec2(0),0).x)/255,0,0,1);
    else if(mode==9) value=vec4(vec3(textureSize(layers,0))/8,1);
    else value=vec4(textureQueryLod(maps,uv)*.125+.5,0,1);
    outputColor=color*value;
}
