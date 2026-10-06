#define _POSIX_C_SOURCE 200809L
#include <box2d/box2d.h>
#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>

static double now_ms(void)
{
    struct timespec t;
    clock_gettime(CLOCK_MONOTONIC, &t);
    return 1000.0 * t.tv_sec + t.tv_nsec / 1000000.0;
}
static int compare(const void* a, const void* b)
{
    double x = *(const double*)a, y = *(const double*)b;
    return (x > y) - (x < y);
}
int main(int argc, char** argv)
{
    enum { count = 1536, warmup = 1600, samples = 1024 };
    b2WorldDef def = b2DefaultWorldDef();
    def.gravity = (b2Vec2){0, 9.8f};
    def.restitutionThreshold = 0;
    b2WorldId world = b2CreateWorld(&def);
    b2BodyDef ground_def = b2DefaultBodyDef();
    b2BodyId ground = b2CreateBody(world, &ground_def);
    b2ShapeDef surface = b2DefaultShapeDef();
    surface.material.friction = .25f;
    surface.material.restitution = .35f;
    float walls[4][4] = {{5.76f,6.38f,5.4f,.09f}, {.38f,4.2f,.09f,2.16f}, {11.14f,4.2f,.09f,2.16f}, {5.76f,1.9f,5.4f,.06f}};
    for (int i = 0; i < 4; ++i)
    {
        float* w = walls[i];
        b2Polygon polygon = b2MakeOffsetBox(w[2], w[3], (b2Vec2){w[0], w[1]}, b2Rot_identity);
        b2CreatePolygonShape(ground, &surface, &polygon);
    }
    b2ShapeDef shape = b2DefaultShapeDef();
    shape.density = .2f / (3.14159265358979323846f * .06f * .06f);
    shape.material.friction = .25f;
    shape.material.restitution = .35f;
    b2Circle circle = {.radius = .06f};
    b2BodyId bodies[count];
    for (int i = 0; i < count; ++i)
    {
        b2BodyDef body = b2DefaultBodyDef();
        body.type = b2_dynamicBody;
        body.position = (b2Vec2){.65f + (i % 50) * .2f, 2.16f + (i / 50) * .125f};
        body.enableSleep = false;
        body.linearVelocity = (b2Vec2){(i % 7 - 3) * .03f, 0};
        body.linearDamping = .05f;
        body.angularDamping = .1f;
        bodies[i] = b2CreateBody(world, &body);
        b2CreateCircleShape(bodies[i], &shape, &circle);
    }
    for (int i = 0; i < warmup; ++i) b2World_Step(world, 1.0f / 144, 4);
    double times[samples], mean = 0, collide = 0, solve = 0, pairs = 0;
    for (int i = 0; i < samples; ++i)
    {
        double start = now_ms();
        b2World_Step(world, 1.0f / 144, 4);
        times[i] = now_ms() - start;
        mean += times[i];
        b2Profile profile = b2World_GetProfile(world);
        collide += profile.collide; solve += profile.solve; pairs += profile.pairs;
    }
    b2Counters counters = b2World_GetCounters(world);
    float states[count * 7];
    for (int i = 0; i < count; ++i)
    {
        b2Vec2 p = b2Body_GetPosition(bodies[i]);
        if (!isfinite(p.x) || !isfinite(p.y) || p.y > 6.4f || p.y < 1.85f) abort();
        b2Rot q = b2Body_GetRotation(bodies[i]);
        b2Vec2 v = b2Body_GetLinearVelocity(bodies[i]);
        states[i*7] = p.x; states[i*7+1] = p.y; states[i*7+2] = q.c; states[i*7+3] = q.s;
        states[i*7+4] = v.x; states[i*7+5] = v.y; states[i*7+6] = b2Body_GetAngularVelocity(bodies[i]);
    }
    if (argc > 1)
    {
        FILE* file = fopen(argv[1], "wb");
        if (!file || fwrite(states, sizeof(states), 1, file) != 1 || fclose(file) != 0) abort();
    }
    int touching = 0;
    for (int i = 0; i < 24; ++i) touching += counters.colorCounts[i];
    qsort(times, samples, sizeof(double), compare);
    printf("{\"bodies\":%d,\"contacts\":%d,\"touchingContacts\":%d,\"workers\":1,\"hz\":144,\"substeps\":4,\"warmup\":%d,\"samples\":%d,\"meanMS\":%.6f,\"p95MS\":%.6f,\"p99MS\":%.6f,\"collideMS\":%.6f,\"solveMS\":%.6f,\"pairsMS\":%.6f}\n",
        count, counters.contactCount, touching, warmup, samples, mean/samples, times[(int)(samples*.95)], times[(int)(samples*.99)], collide/samples, solve/samples, pairs/samples);
    b2DestroyWorld(world);
    return 0;
}
