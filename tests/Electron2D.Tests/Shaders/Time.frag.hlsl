cbuffer Material : register(b0, space3) {
    float4 tint;
    float TIME;
    float gain;
    float time;
};
float4 main(float4 color : TEXCOORD0) : SV_Target0 {
    return float4(frac(TIME * 4), gain, time, 1) * tint * color;
}
