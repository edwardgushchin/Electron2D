// Distance between convex cores, optionally extruded by each full translation.
struct QuerySupport { vec2 a; vec2 b; vec2 w; };
vec2 querySupportVertex(Hull h,vec2 d)
{
    vec2 best=vertex(h,0u);float projection=dot2(best,d);
    for(uint i=1u;i<h.count;i++){vec2 candidate=vertex(h,i);float value=dot2(candidate,d);if(value>projection){best=candidate;projection=value;}}
    return best;
}
QuerySupport querySupport(Hull a,Hull b,vec2 sweepA,vec2 sweepB,vec2 d)
{
    // GJK requires an extreme vertex; tolerant contact-face selection can cycle near crossing segments.
    vec2 pa=querySupportVertex(a,-d),pb=querySupportVertex(b,d);
    if(dot2(sweepA,-d)>0)pa+=sweepA;
    if(dot2(sweepB,d)>0)pb+=sweepB;
    return QuerySupport(pa,pb,pb-pa);
}
vec3 lineWeights(vec2 a,vec2 b)
{
    vec2 d=b-a;float dd=dot2(d,d),t=dd>0?clamp(-dot2(a,d)/dd,0,1):0;
    return vec3(1-t,t,0);
}
vec3 triangleWeights(vec2 a,vec2 b,vec2 c)
{
    float denominator=cross2(b-a,c-a);
    if(denominator!=0)
    {
        vec3 weights=vec3(cross2(b,c),cross2(c,a),cross2(a,b))/denominator;
        if(all(greaterThanEqual(weights,vec3(0))))return weights;
    }
    vec3 ab=lineWeights(a,b),bc=lineWeights(b,c),ca=lineWeights(c,a);
    float dab=dot2(ab.x*a+ab.y*b,ab.x*a+ab.y*b),dbc=dot2(bc.x*b+bc.y*c,bc.x*b+bc.y*c),dca=dot2(ca.x*c+ca.y*a,ca.x*c+ca.y*a);
    return dab<=dbc&&dab<=dca?ab:dbc<=dca?vec3(0,bc.xy):vec3(ca.y,0,ca.x);
}
float queryDistance(Hull a,Hull b,vec2 sweepA,vec2 sweepB,out vec2 normal)
{
    if(a.boundary||b.boundary)
    {
        if(a.boundary&&b.boundary){normal=vec2(0);return 3.402823466e38;}
        bool flip=b.boundary;Hull plane=flip?b:a,other=flip?a:b;vec3 line=boundaryPlane(plane);
        vec2 sp=flip?sweepB:sweepA,so=flip?sweepA:sweepB;
        float minimum=dot2(line.xy,vertex(other,0u));
        for(uint i=1u;i<other.count;i++)minimum=min(minimum,dot2(line.xy,vertex(other,i)));
        normal=flip?-line.xy:line.xy;return max(0,minimum+min(0,dot2(line.xy,so))-line.z-max(0,dot2(line.xy,sp)));
    }
    QuerySupport simplex[3];uint count=1u;vec3 weights=vec3(1,0,0);
    simplex[0]=querySupport(a,b,sweepA,sweepB,vec2(1,0));
    vec2 v=simplex[0].w;
    [[dont_unroll]] for(uint iteration=0u;iteration<64u;iteration++)
    {
        float squared=dot2(v,v);if(!finite2(vec2(squared))){fail();normal=vec2(0);return 0;}
        if(squared<=1e-12)break;
        QuerySupport next=querySupport(a,b,sweepA,sweepB,-v);
        bool duplicate=false;for(uint i=0u;i<count;i++)if(next.w==simplex[i].w)duplicate=true;
        if(duplicate||squared-dot2(v,next.w)<=max(1e-12,1e-7*squared))break;
        if(count==3u){failQuery(4u);break;}
        simplex[count++]=next;
        weights=count==2u?lineWeights(simplex[0].w,simplex[1].w):triangleWeights(simplex[0].w,simplex[1].w,simplex[2].w);
        vec2 previous=v;v=vec2(0);uint kept=0u;vec3 reduced=vec3(0);
        for(uint i=0u;i<count;i++)if(weights[i]>0){v+=weights[i]*simplex[i].w;simplex[kept]=simplex[i];reduced[kept++]=weights[i];}
        count=kept;weights=reduced;
        if(count==3u){normal=vec2(0);return 0;}
        if(dot2(v,v)>=squared){v=previous;break;}
        if(iteration==63u)failQuery(2u);
    }
    normal=normalized(v);return length(v);
}
// Advances a translating convex core without sampling past a thin obstacle.
bool querySweep(Hull a,Hull b,vec2 motion,vec2 sweptB,float target,float tolerance,out float fraction,out vec2 normal)
{
    fraction=0;normal=vec2(0);float time=0;
    [[dont_unroll]] for(uint i=0u;i<128u;i++)
    {
        Hull moved=a;moved.pose.xy+=time*motion;
        vec2 previous=normal;float distance=queryDistance(moved,b,vec2(0),sweptB,normal),gap=distance-target;
        if(normal==vec2(0)||(distance<=0.00001&&previous!=vec2(0)))normal=previous;
        if(gap<=tolerance){fraction=time;return true;}
        float closing=dot2(motion,normal);if(closing<=0)return false;
        float next=time+gap/closing;if(!finite2(vec2(next))){fail();return false;}if(next>1)return false;
        if(next<=time){failQuery(8u);return false;}time=next;
    }
    failQuery(16u);return false;
}
