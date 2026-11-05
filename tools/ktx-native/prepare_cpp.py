"""Own ASTC context and working storage without replacing global C++ operators."""

PREFIX = "external/astc-encoder/Source/"
FILES = tuple(PREFIX + name + ".cpp" for name in (
    "astcenc_entry", "astcenc_block_sizes", "astcenc_percentile_tables", "astcenc_partition_tables")) + (PREFIX + "astcenc_internal_entry.h",)


def prepare(name, text):
    if name not in FILES:
        return text

    def change(before, after, count=1):
        nonlocal text
        if text.count(before) != count:
            raise ValueError("Pinned C++ ownership guard changed: " + name + ": " + before)
        text = text.replace(before, after)

    if name.endswith("astcenc_internal_entry.h"):
        change("#include <functional>\n", "")
        change("void init(std::function<size_t(void)> init_func)", "template <typename Init>\n\tvoid init(Init &&init_func)")
        change("void term(std::function<void(void)> term_func)", "template <typename Term>\n\tvoid term(Term &&term_func)")
        return text
    if name.endswith("astcenc_entry.cpp"):
        change("astcenc_context* ctxo = new astcenc_context;",
            "auto ctxo = e2d_ktx::make<astcenc_context>().release();\n"
            "\tstd::unique_ptr<astcenc_context, decltype(&astcenc_context_free)> context_owner(ctxo, astcenc_context_free);")
        change("\n\t\tdelete ctxo;\n\t}\n}", "\n\t\te2d_ktx::ObjectFree<astcenc_context>{}(ctxo);\n\t}\n}")
        change("\n\t\tdelete ctxo;\n", "\n")
        change("\n\t\t\tdelete ctxo;\n", "\n", 2)
        change("\t\tctx->owns_bsd = true;\n\t\tctx->bsd = bsd;\n", "")
        change("\t\tinit_block_size_descriptor(config.block_x", "\t\tctx->owns_bsd = true;\n\t\tctx->bsd = bsd;\n\t\tinit_block_size_descriptor(config.block_x")
        change("\t\t\tif (ctx->owns_bsd)\n\t\t\t{\n\t\t\t\taligned_free<const block_size_descriptor>(ctx->bsd);\n\t\t\t}\n", "")
        change("\t*context = ctxo;\n", "")
        change("\tprepare_angular_tables();\n#endif\n\n\treturn ASTCENC_SUCCESS;",
            "\tprepare_angular_tables();\n#endif\n\n\t*context = context_owner.release();\n\treturn ASTCENC_SUCCESS;")
        change("\t\taligned_free<compression_working_buffers>(ctx->working_buffers);",
            "\t\te2d_ktx::ArrayFree<float>{}(ctx->input_alpha_averages);\n\t\taligned_free<compression_working_buffers>(ctx->working_buffers);")
        change("arg.work_memory = new vfloat4[ag.work_memory_size];",
            "auto work_memory = e2d_ktx::make_array<vfloat4>(ag.work_memory_size);\n\targ.work_memory = work_memory.get();")
        change("\tdelete[] arg.work_memory;", "")
        change("ctx->input_alpha_averages = new float[texel_count];",
            "ctx->input_alpha_averages = e2d_ktx::make_array<float>(texel_count).release();")
        change("delete[] ctx->input_alpha_averages;", "e2d_ktx::ArrayFree<float>{}(ctx->input_alpha_averages);")
    elif name.endswith("astcenc_block_sizes.cpp"):
        change("dt_init_working_buffers* wb = new dt_init_working_buffers;",
            "auto working_owner = e2d_ktx::make<dt_init_working_buffers>();\n\tauto wb = working_owner.get();", 2)
        change("\tdelete wb;", "", 2)
        change("const float *percentiles = get_2d_percentile_table(texels_x, texels_y);",
            "e2d_ktx::Array<const float> percentile_owner(get_2d_percentile_table(texels_x, texels_y));\n\tconst float *percentiles = percentile_owner.get();")
        change("\tdelete[] percentiles;", "")
    elif name.endswith("astcenc_percentile_tables.cpp"):
        change("float* unpacked_table = new float[WEIGHTS_MAX_BLOCK_MODES];",
            "float* unpacked_table = e2d_ktx::make_array<float>(WEIGHTS_MAX_BLOCK_MODES).release();")
    else:
        change("uint64_t* canonical_patterns = new uint64_t[BLOCK_MAX_PARTITIONINGS * BIT_PATTERN_WORDS];",
            "auto pattern_owner = e2d_ktx::make_array<uint64_t>(BLOCK_MAX_PARTITIONINGS * BIT_PATTERN_WORDS);\n\tauto canonical_patterns = pattern_owner.get();")
        change("\tdelete[] canonical_patterns;", "")
    return '#include "memory.hpp"\n' + text
