using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private readonly record struct TonemapCompositeUniforms(TonemapUniforms Tonemap, AdjustmentUniforms Adjustment);
    private readonly RenderHandle?[] _tonemapPipelines = new RenderHandle?[2];
    private static readonly byte[] TonemapShader = BuiltInShaders.Read("CanvasTonemap.frag.spv");

    private void DrawTonemap(nint command, CanvasRenderTarget output, in TonemapUniforms tone,
        in AdjustmentUniforms correction, in SDL.GPUTextureSamplerBinding lookup)
    {
        Span<SDL.GPUTextureSamplerBinding> bindings = stackalloc SDL.GPUTextureSamplerBinding[2];
        bindings[0] = new()
        {
            Texture = output.PostProcess!.Raw.DangerousGetHandle(),
            Sampler = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled, bias: 0)
        };
        bindings[1] = lookup;
        var parameters = new TonemapCompositeUniforms(tone, correction);
        DrawPostProcessPass(command, output.Next.DangerousGetHandle(), output.Size, _tonemapPipelines[output.HDR ? 1 : 0]!, bindings, in parameters);
    }
}

internal readonly record struct TonemapUniforms(Vector4 Options, Vector4 Curve, Vector4 Transfer)
{
    internal static TonemapUniforms Prepare(TonemapSettings settings, float outputMaximum, bool byteTarget = false, bool linearSource = false)
    {
        var output = Math.Clamp((double)outputMaximum, 1, 65504);
        var white = (double)settings.White;
        var curve = Vector4.Zero;
        switch (settings.Mode)
        {
            case CanvasEnvironment.ToneMapper.Reinhard:
                white = Math.Max(output, white);
                curve.X = (float)(output / (white * white));
                break;
            case CanvasEnvironment.ToneMapper.Filmic:
                white = Math.Max(1, white);
                var filmic = (white * (.88 * white + .06) + .002) /
                    (white * (.88 * white + .6) + .06) - 1d / 30;
                curve.X = (float)(1 / filmic);
                break;
            case CanvasEnvironment.ToneMapper.ACES:
                white = Math.Max(1, white);
                var biased = white * 1.8;
                var ACES = (biased * (biased + .0245786) - .000090537) /
                    (biased * (biased * .983729 + .432951) + .238081);
                curve.X = (float)(1 / ACES);
                break;
            case CanvasEnvironment.ToneMapper.AGX:
                const double middle = .18;
                white = Math.Max(2, settings.AGXWhite) * output;
                curve.X = settings.AGXContrast;
                curve.Y = (float)(1 / (white - middle));
                curve.Z = (float)(settings.AGXContrast * (1 - middle) / (output - middle));
                break;
            default:
                white = output;
                break;
        }
        var options = new Vector4((float)settings.Mode, settings.Exposure, (float)output, (float)Math.Min(white, 65504));
        return new(options, curve, new(byteTarget && !linearSource ? 1 : 0, byteTarget ? 1 : 0, 0, 0));
    }
}
