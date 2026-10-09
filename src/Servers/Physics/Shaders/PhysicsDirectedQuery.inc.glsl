// Shared directed contact against an ordinary or extruded convex region.
Hull pointHull(Hull templateHull,vec2 point)
{
    templateHull.count=1u;templateHull.radius=0;templateHull.midpoint=false;templateHull.pose=vec4(point,0,0);return templateHull;
}
bool directedQuery(Hull ray,Hull other,vec2 extrusion,float margin,bool slide,bool flip,vec2 extension,uint pieceA,uint pieceB,bool emit)
{
    vec2 from=vertex(ray,0u),d=vertex(ray,1u)-from;if(d==vec2(0))return false;
    vec2 axis=normalized(d);d+=axis*(margin+max(0,dot2(axis,extension)));
    vec2 normal;float fraction;
    if(extrusion==vec2(0)&&(other.count<=2u||other.radius==0))
    {
        uint feature;if(!rayHit(other,from,d,fraction,normal,feature))return false;normal=-normal;
    }
    else
    {
        Hull point=pointHull(ray,from);
        if(queryDistance(point,other,vec2(0),extrusion,normal)<=other.radius)return false;
        if(!querySweep(point,other,d,extrusion,other.radius,0.00001,fraction,normal))return false;
    }
    // queryDistance points toward the other core; reverse it to get its outward surface normal.
    if(dot2(normal,d)<0.0001)return false;
    if(emit)
    {
        vec2 a=from+d;float depth=(1-fraction)*length(d);vec2 n=slide?normal:axis,b=a-depth*n;
        contactLimit=0.05;
        if(flip)emitPoint(b,a,-n,uvec4(0,0,pieceA,pieceB));else emitPoint(a,b,n,uvec4(0,0,pieceA,pieceB));
    }
    return true;
}
