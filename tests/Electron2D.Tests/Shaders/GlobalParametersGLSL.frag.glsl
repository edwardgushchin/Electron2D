#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(set=3,binding=0,std140,row_major) uniform Globals {
    mat3 affine;
    mat4 matrix4;
    mat3 matrix3;
    mat2 basis;
    vec4 rectangle;
    vec4 tint;
    vec4 wide;
    vec3 rgb;
    vec2 offset;
    float gain;
    uvec4 unsigned4;
    uvec3 unsigned3;
    uvec2 unsigned2;
    uint unsignedValue;
    ivec4 rectangleI;
    ivec4 signed4;
    ivec3 signed3;
    ivec2 signed2;
    int signedValue;
    bvec4 mask4;
    bvec3 mask3;
    bvec2 mask2;
    bool enabled;
} g;
layout(set=2,binding=0) uniform sampler2D GLOBAL_map;
layout(set=2,binding=1) uniform sampler2DArray GLOBAL_layers;
layout(location=0) out vec4 result;
void main() { result=color*g.gain*g.tint*texture(GLOBAL_map,uv)*texture(GLOBAL_layers,vec3(uv,0))*(g.enabled && g.mask2.x && g.mask2.y && g.mask3.x && !g.mask3.y && g.mask3.z && g.mask4.w && g.signedValue == -7 && g.signed2.x == -2 && g.signed3.z == -6 && g.signed4.w == 4 && g.rectangleI.z == 7 && g.unsignedValue == 4294967294u && g.unsigned2.x == 4294967295u && g.unsigned3.z == 2147483648u && g.unsigned4.w == 123u && g.rgb.z == .125 && g.wide.w == 1 && g.rectangle.z == 7 && g.basis[0][0] == 1 && g.matrix3[1][1] == 1 && g.matrix4[3][3] == 1 && g.matrix3[2][0] == .25 && g.matrix4[0][3] == .125 ? 1.0 : 0.0); result.a=1; }
