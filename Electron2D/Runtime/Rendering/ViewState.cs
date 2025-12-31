using System.Numerics;

namespace Electron2D;

internal struct ViewState(
    float halfW,
    float halfH,
    float ppu,
    Vector2 camPos,
    float camRot,
    float cos,
    float sin,
    bool hasRot,
    ViewCullRect cull)
{
    public readonly float HalfW = halfW;
    public readonly float HalfH = halfH;
    public readonly float Ppu = ppu;

    public readonly Vector2 CamPos = camPos;
    public readonly float CamRot = camRot;

    public readonly float Cos = cos;
    public readonly float Sin = sin;
    public readonly bool HasRot = hasRot;

    public readonly ViewCullRect Cull = cull;
}