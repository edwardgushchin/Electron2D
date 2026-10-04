#version 450
layout(location=0) in vec4 color;
layout(location=0) out vec4 result;
layout(set=2,binding=0) uniform sampler2D SCREEN_TEXTURE;
layout(std140,set=3,binding=0) uniform Effect {
    vec2 SCREEN_PIXEL_SIZE;
    float lod;
    float unpremultiply;
    vec4 tint;
    vec2 offset;
};
void main() {
    vec4 c = textureLod(SCREEN_TEXTURE,(gl_FragCoord.xy+offset) * SCREEN_PIXEL_SIZE,lod);
    if (unpremultiply > 0.5 && c.a > 0.0001) c.rgb /= c.a;
    result = color*tint*c;
}
