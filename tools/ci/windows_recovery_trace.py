"""Instrument temporary motion sources through their existing private result padding."""


FLAGS = {
    'initial': 0, 'continued': 1, 'scan': 2, 'source': 3, 'geometry': 4,
    'bounds': 5, 'category': 6, 'leaf': 7, 'candidate': 8, 'evaluated': 9,
    'primaryValid': 10, 'secondaryValid': 11, 'nonnegative': 12,
    'accepted': 13, 'projection': 14, 'changed': 15,
    'recovering': 30, 'resting': 31,
}


def replace_once(text, old, new):
    if text.count(old) != 1:
        raise ValueError('Recovery trace source anchor is missing or ambiguous: ' + old)
    return text.replace(old, new)


def instrument_shader(text):
    if 'uvec4 recoveryTrace=outputWords[stateAt];' in text:
        raise ValueError('Recovery source is already instrumented.')
    target = 'outputWords[gl_GlobalInvocationID.x*8u+2u].w'
    markers = (
        ('    bool recovering=phase==0u,resting=phase==2u;',
         '4u|(recovering?0x40000000u:0u)|(resting?0x80000000u:0u)'),
        ('        uvec2 token=payload[q.body.z+own];', '8u'),
        ('if(ga.data.y==0u)continue;', '16u'),
        ('        vec4 bounds=queryBounds(shapeA,ga,recovering||resting);int at=1;', '32u'),
        ('            Node node=nodes[at];', 'node.hasCategory!=0?64u:0u'),
        ('                indexB=uint(node.shape);', '128u'),
        ('                    if(shapeB.owner.x>=bodyCount||mapB.identity.w!=shapeB.policy.x){fail();return;}', '256u'),
        ('                                    if(!contact.valid||contact.depth<0)continue;', '4096u'),
        ('                                    accepted=true;', '8192u'),
        ('    if(recovering&&bestDepth>0)\n    {', '16384u'),
        ('        converged=uintBitsToFloat(outputWords[stateAt+1u].xy)==recovery;', 'converged?0u:32768u'),
    )
    for anchor, value in markers:
        trace = '\n#if defined(E2D_MOTION_STAGE) && E2D_MOTION_STAGE < 2\n    atomicOr(' + target + ',' + value + ');\n#endif\n'
        text = replace_once(text, anchor, anchor + trace)
    anchor = '    bool converged=false;'
    initialize = '''
#if E2D_MOTION_STAGE == 0
    outputWords[stateAt+2u].w=1u;outputWords[stateAt+3u].x=0u;
#elif E2D_MOTION_STAGE == 1
    atomicOr(outputWords[stateAt+2u].w,2u);
#endif'''
    text = replace_once(text, anchor, anchor + initialize)
    anchor = '                                MotionContactPair contacts=evaluate(a,b,ga,gb,vec2(0),q.motionMargin.z);'
    evaluated = '''
#if E2D_MOTION_STAGE < 2
    atomicOr(outputWords[gl_GlobalInvocationID.x*8u+2u].w,512u|(contacts.first.valid?1024u:0u)|(contacts.second.valid?2048u:0u));
    outputWords[gl_GlobalInvocationID.x*8u+3u].x=floatBitsToUint(contacts.first.depth);
#endif'''
    text = replace_once(text, anchor, anchor + evaluated)
    anchor = '    outputWords[stateAt+2u].z=0u;'
    capture = '''    uvec4 recoveryTrace=outputWords[stateAt];
    outputWords[stateAt+6u].w=(outputWords[stateAt+2u].w&0xc000ffffu)|((recoveryTrace.x&63u)<<16)|((recoveryTrace.y&63u)<<22)|((outputWords[stateAt+6u].w&3u)<<28);
    outputWords[stateAt+7u].z=outputWords[stateAt+3u].x;outputWords[stateAt+7u].w=recoveryTrace.z;
'''
    text = replace_once(text, anchor, capture + anchor)
    anchor = '    if(rested)result=restResult;else safe=unsafeFraction=1;'
    publish = '''
#if defined(E2D_MOTION_STAGE) && E2D_MOTION_STAGE == 3
    result.owner.w=outputWords[stateAt+6u].w;result.objectID.zw=outputWords[stateAt+7u].zw;
#endif'''
    return replace_once(text, anchor, anchor + publish)


def instrument_query(text):
    return replace_once(text, 'results[i] = new() { TravelRemainder =',
                        'results[i] = new() { Padding = 0xDEADC0DEu, TravelRemainder =')
