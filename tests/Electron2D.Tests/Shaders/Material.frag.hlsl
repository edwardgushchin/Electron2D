cbuffer Material : register(b0, space3) {
    float4 tint;
    float gain;
    int mode;
    float2 offset;
};
cbuffer Extra : register(b1, space3) {
    float weights[2];
    int2 shift;
    uint enabled;
};
float4 main(float4 color : TEXCOORD0, float4 position : SV_Position) : SV_Target0 {
    float4 result = tint * color;
    result.rgb *= gain * (weights[0] + weights[1]);
    if (mode == 1) result = result.bgra;
    if (enabled == 0 || position.x < offset.x + shift.x || position.y < offset.y + shift.y) return float4(0, 0, 0, 1);
    return result;
}
