#version 450
layout(location=0) in vec2 position;
layout(location=1) in vec4 color;
layout(location=2) in vec2 uv;
layout(set=1,binding=0,std140) uniform Frame { vec2 size; };
layout(set=1,binding=1,std140) uniform Globals {
    bool enabled;
    bvec2 mask2;
    bvec3 mask3;
    bvec4 mask4;
    int signedValue;
    ivec2 signed2;
    ivec3 signed3;
    ivec4 signed4;
    ivec4 rectangleI;
    uint unsignedValue;
    uvec2 unsigned2;
    uvec3 unsigned3;
    uvec4 unsigned4;
    float gain;
    vec2 offset;
    vec3 rgb;
    vec4 wide;
    vec4 tint;
    vec4 rectangle;
    mat2 basis;
    mat3 matrix3;
    mat4 matrix4;
    mat3 affine;
} g;
layout(set=0,binding=0) uniform sampler2D GLOBAL_map;
layout(set=0,binding=1) uniform sampler2DArray GLOBAL_layers;
layout(location=0) out vec4 outColor;
layout(location=1) out vec2 outUV;
void main() {
    vec2 p=(g.affine*vec3(position,1)).xy+g.offset*g.gain;
    gl_Position=vec4(p.x/size.x*2-1,1-p.y/size.y*2,0,1);
    outUV=uv;
    outColor=color*g.gain*textureLod(GLOBAL_map,uv,0)*textureLod(GLOBAL_layers,vec3(uv,0),0)*(g.enabled && g.mask2.x && g.mask2.y && g.mask3.x && !g.mask3.y && g.mask3.z && g.mask4.w && g.signedValue == -7 && g.signed2.x == -2 && g.signed3.z == -6 && g.signed4.w == 4 && g.rectangleI.z == 7 && g.unsignedValue == 4294967294u && g.unsigned2.x == 4294967295u && g.unsigned3.z == 2147483648u && g.unsigned4.w == 123u && g.rgb.z == .125 && g.wide.w == 1 && g.rectangle.z == 7 && g.basis[0][0] == 1 && g.matrix3[1][1] == 1 && g.matrix4[3][3] == 1 && g.matrix3[2][0] == .25 && g.matrix4[0][3] == .125 ? 1.0 : 0.0);
}
