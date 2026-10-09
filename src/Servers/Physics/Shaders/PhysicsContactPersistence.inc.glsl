vec2 historyRotate(vec2 q,vec2 v) { return vec2(q.x*v.x-q.y*v.y,q.y*v.x+q.x*v.y); }
// Local anchors are boundary points, not the midpoint used by some solver Jacobians.
float contactHistoryDistance(vec4 previous,vec4 current,vec2 normal,vec4 poseA,vec4 poseB,vec2 limits)
{
    vec2 a=current.xy-previous.xy,b=current.zw-previous.zw;
    float da=dot(a,a),db=dot(b,b),radius2=limits.x*limits.x;
    if(da>=radius2||db>=radius2)return uintBitsToFloat(0x7f800000u);
    vec2 axis=(poseB.xy-poseA.xy)+historyRotate(poseB.zw,previous.zw)-historyRotate(poseA.zw,previous.xy);
    float separation=dot(axis,normal);vec2 tangent=axis-separation*normal;
    if(separation>limits.y||dot(tangent,tangent)>limits.y*limits.y)return uintBitsToFloat(0x7f800000u);
    return max(da,db);
}
