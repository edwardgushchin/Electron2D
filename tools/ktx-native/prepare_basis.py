"""Own the pinned Basis standard containers, queued callbacks and resamplers."""

PREFIX = "external/basis_universal/"
FILES = tuple(PREFIX + name for name in (
    "encoder/3rdparty/android_astc_decomp.cpp", "encoder/3rdparty/tinyexr.h",
    "encoder/basisu_astc_hdr_6x6_enc.h", "encoder/basisu_astc_hdr_common.cpp",
    "encoder/basisu_astc_ldr_encode.cpp", "encoder/basisu_astc_ldr_encode.h",
    "encoder/basisu_comp.cpp", "encoder/basisu_comp.h", "encoder/basisu_enc.cpp",
    "encoder/basisu_enc.h", "encoder/basisu_frontend.cpp", "encoder/basisu_gpu_texture.cpp",
    "encoder/basisu_opencl.cpp", "encoder/basisu_uastc_enc.cpp", "encoder/pvpngreader.cpp",
    "transcoder/basisu.h", "transcoder/basisu_astc_helpers.h", "transcoder/basisu_containers.h",
    "transcoder/basisu_containers_impl.h", "transcoder/basisu_transcoder.cpp",
    "transcoder/basisu_transcoder.h", "transcoder/basisu_transcoder_internal.h"))


def prepare(name, text):
    if name not in FILES:
        return text
    if text.startswith("\xef\xbb\xbf"):
        text = text[3:]

    def change(before, after, count):
        nonlocal text
        if text.count(before) != count:
            raise ValueError("Pinned Basis ownership changed: " + name + ": " + before)
        text = text.replace(before, after)

    if name.endswith("encoder/basisu_enc.cpp"):
        change("std::function<void()>", "e2d_ktx::Function", 4)
        for declaration in ("Resampler *resamplers[cMaxComps];", "Resampler* resamplers[cMaxComps];"):
            change(declaration, "std::unique_ptr<Resampler, e2d_ktx::ObjectFree<Resampler>> resamplers[cMaxComps];", 1)
        change("new Resampler(", "e2d_ktx::make<Resampler>(", 4)
        change(": filter_scale, 0, 0);", ": filter_scale, 0.0f, 0.0f);", 2)
        change("filter_scale, filter_scale, 0, 0);", "filter_scale, filter_scale, 0.0f, 0.0f);", 2)
        change("\t\tsamples[0].resize(src_w);",
               "\t\tif (resamplers[0]->status() != Resampler::STATUS_OKAY) return false;\n\t\tsamples[0].resize(src_w);", 2)
        change("\t\t\tsamples[i].resize(src_w);",
               "\t\t\tif (resamplers[i]->status() != Resampler::STATUS_OKAY) return false;\n\t\t\tsamples[i].resize(src_w);", 2)
        change("delete resamplers[i];", "resamplers[i].reset();", 4)
    elif name.endswith("encoder/basisu_enc.h"):
        change("std::function<void()>", "e2d_ktx::Function", 3)
    return '#include "basis-memory.hpp"\n' + text
