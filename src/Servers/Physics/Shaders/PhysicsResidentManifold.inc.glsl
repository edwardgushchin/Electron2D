// Shared ordinary contact geometry; the caller owns emitPoint and contactLimit.
void facePoint(Hull refHull,Hull incHull,vec2 p,vec2 n,uint refEdge,uint incEdge,bool flip,uvec2 pieces)
{
    vec2 refVertex=vertex(refHull,refEdge);
    vec2 onPlane=p+dot2(refVertex-p,n)*n;
    vec2 a=onPlane+refHull.radius*n,b=p-incHull.radius*n;
    uint fa=edgeFeature(refHull,refEdge,onPlane),fb=incHull.count==1u?0u:edgeFeature(incHull,incEdge,p);
    if(flip)emitPoint(b,a,-n,uvec4(fb,fa,pieces));
    else emitPoint(a,b,n,uvec4(fa,fb,pieces));
}
void ordinary(Hull a,Hull b,uvec2 pieces)
{
    Axis axis;if(!separatingAxis(a,b,axis))return;
    if(axis.owner!=0u)
    {
        bool flip=axis.owner==2u;Hull r=flip?b:a,inc=flip?a:b;vec2 n=flip?-axis.normal:axis.normal;
        if(inc.count==1u) {facePoint(r,inc,vertex(inc,0u),n,axis.edge,0u,flip,pieces);return;}
        uint incident=0u;float minimum=3.402823466e38;
        for(uint i=0u;i<edges(inc);i++){float d=dot2(n,edgeNormal(inc,i));if(d<minimum){minimum=d;incident=i;}}
        vec2 from=vertex(r,axis.edge),to=vertex(r,(axis.edge+1u)%r.count),tangent=normalized(to-from);
        float span=dot2(to-from,tangent);
        vec2 p=vertex(inc,incident),q=vertex(inc,(incident+1u)%inc.count);
        float fp=dot2(p-from,tangent),fq=dot2(q-from,tangent);
        if(max(fp,fq)<0||min(fp,fq)>span)
        {
            vec2 f=fractions(from,to,p,q),pa=lerp2(from,to,f.x),pb=lerp2(p,q,f.y),delta=pb-pa;
            float distance=length(delta);if(distance-r.radius-inc.radius>contactLimit)return;
            vec2 direction=distance>0?delta/distance:n;
            uint fa=edgeFeature(r,axis.edge,pa),fb=edgeFeature(inc,incident,pb);
            pa+=r.radius*direction;pb-=inc.radius*direction;
            if(flip)emitPoint(pb,pa,-direction,uvec4(fb,fa,pieces));
            else emitPoint(pa,pb,direction,uvec4(fa,fb,pieces));
            return;
        }
        vec2 cp,cq;clipSegment(p,q,fp,fq,span,cp,cq);
        facePoint(r,inc,cp,n,axis.edge,incident,flip,pieces);
        if(length(cq-cp)>tolerances.z)facePoint(r,inc,cq,n,axis.edge,incident,flip,pieces);
        return;
    }
    uint fa,fb;vec2 pa=support(a,axis.normal,vertex(b,0u),fa);
    vec2 pb=support(b,-axis.normal,pa,fb);pa=support(a,axis.normal,pb,fa);
    emitPoint(pa+a.radius*axis.normal,pb-b.radius*axis.normal,axis.normal,uvec4(fa,fb,pieces));
}
