cbuffer Matrices : register(b0, space3) {
    column_major float2x2 columnMatrix;
    row_major float2x2 rowMatrix;
    column_major float2x2 columnMatrices[2];
    row_major float2x2 rowMatrices[2];
    float tail;
};
float4 main() : SV_Target0 {
    bool valid = tail == 0.875 && all(columnMatrix[0] == columnMatrices[0][0]) && all(columnMatrix[1] == columnMatrices[0][1])
        && all(rowMatrix[0] == columnMatrices[1][0]) && all(rowMatrix[1] == columnMatrices[1][1])
        && all(rowMatrix[0] == rowMatrices[0][0]) && all(rowMatrix[1] == rowMatrices[0][1])
        && all(columnMatrix[0] == rowMatrices[1][0]) && all(columnMatrix[1] == rowMatrices[1][1]);
    float2 a = mul(float2(1, 0.5), columnMatrix);
    float2 b = mul(float2(0.5, 1), rowMatrix);
    return valid ? float4(a.x, a.y, (b.x + b.y) * 0.5, 1) : float4(1, 0, 1, 1);
}
