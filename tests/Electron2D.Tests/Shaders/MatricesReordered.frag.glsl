#version 450
layout(location = 0) out vec4 outputColor;
layout(set = 3, binding = 0, std140) uniform First {
    float tail;
    layout(column_major) mat2 rowMatrices[2];
    layout(row_major) mat2 columnMatrix;
};
layout(set = 3, binding = 1, std140) uniform Second {
    layout(row_major) mat2 columnMatrices[2];
    layout(column_major) mat2 rowMatrix;
};
void main() {
    bool valid = tail == 0.875 && all(equal(columnMatrix[0], columnMatrices[0][0])) && all(equal(columnMatrix[1], columnMatrices[0][1]))
        && all(equal(rowMatrix[0], columnMatrices[1][0])) && all(equal(rowMatrix[1], columnMatrices[1][1]))
        && all(equal(rowMatrix[0], rowMatrices[0][0])) && all(equal(rowMatrix[1], rowMatrices[0][1]))
        && all(equal(columnMatrix[0], rowMatrices[1][0])) && all(equal(columnMatrix[1], rowMatrices[1][1]));
    vec2 a = columnMatrix * vec2(1, 0.5);
    vec2 b = rowMatrix * vec2(0.5, 1);
    outputColor = valid ? vec4(a.x, a.y, (b.x + b.y) * 0.5, 1) : vec4(1, 0, 1, 1);
}
