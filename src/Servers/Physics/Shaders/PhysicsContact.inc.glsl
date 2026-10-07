struct Contact {
    vec4 ids; vec4 mass; vec4 normal; vec4 rolling; vec4 soft;
    vec4 anchors1; vec4 params1; vec4 impulses1;
    vec4 anchors2; vec4 params2; vec4 impulses2;
    vec4 surfaceA; vec4 surfaceB;
};
// Feature IDs, point count and rolling impulse accompany two normal/tangent pairs.
struct ContactHistory { vec4 impulses; vec4 features; };
