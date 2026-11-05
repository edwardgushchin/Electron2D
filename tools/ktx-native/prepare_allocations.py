"""Guard the pinned codec's allocation refusal and partial ownership paths."""

import hashlib
from pathlib import Path
import re
import sys

PINS = {
    "external/basis_universal/transcoder/basisu_containers.h": ("b9681f9b1cbf96a07a2d88d6bb013720029d7f8b87cbd1e88c8f8f1c97740a9d", "09d02c5bd03943d9f6fcbf6bf9fce19ab685b0150050ba5ac0308dadc0f12f42"),
    "lib/src/texture2.c": ("3fd9f85588b002f585faadadf90cc435be9da97c8b09c39a45ff678dd0d0495b", "e9c237b593130782b1ba40afdb3dc396714ce7eba120111152f5ece24f7b9388"),
    "lib/src/texture.c": ("52ba8727684c566551012b800397c33ad5f0b975e9d5a866ac443c9512f1ba8a", "51b75213b3ce4e4e0d8b13ed4338b2ae35859ac9c3647fa9e3838dc9871c7a3e"),
    "external/dfdutils/createdfd.c": ("5f625c9ea86e5439342627036255cb6488c95a2bb1943e8c68023983f43a5092", "08f467cd20345319dab9b47772643b7c96772faae6b7fc584c75dca4215f9ab1"),
    "external/basis_universal/transcoder/basisu_containers_impl.h": ("4815d5ae9681516ceb89f7ba75d4f1225b5f5488485c025be64d5cec04e09555", "53df10bbc07dc73c399807c1a597e00719fcf33c5d9e3e17d77a7d45bac37a3e"),
    "lib/src/hashlist.c": ("f4cf7aeab9d45220741741ced4eb3f960a164b5675fbdce144f991bcf3ed33e7", "6da00d2e87609f3217f169a11641b573d39412bdedb131904f2184ac4cd0986a"),
    "lib/src/uthash.h": ("965161b818b4da54d252399d4af6b6bb7869160e8ab21efee1938f546bf3d57f", "4d3597c1edb84c072ab62574d7d8f44d8cc8d0cf75a5ed4ac3fc960e7b6acf46"),
    "external/basis_universal/transcoder/basisu_transcoder.cpp": ("564ba05990ea0855c1683bc5aec285b522e9a7e65abb4ce5b27451d2981f294e", "c9c499c09211942e31f6d71f6b4dced245f7a5888f57cd6b26786a3d947b3285"),
}


def replace(text, before, after):
    if text.count(before) != 1:
        raise ValueError("Pinned allocation preimage no longer matches: " + before)
    return text.replace(before, after)


def prepare(name, text):
    if name.endswith("basisu_containers.h"):
        # Delegation retires the vector's backing buffer if its body throws.
        start = text.index("\t\tinline vector(size_t n, const T& init) :")
        end = text.index("\t\t// mostly to ease porting from std::vector", start)
        constructors = text[start:end]
        constructors, count = re.subn(r":\n\t\t\tm_p\(nullptr\),\n\t\t\tm_size\(0\),\n\t\t\tm_capacity\(0\)", ": vector()", constructors)
        if count != 6:
            raise ValueError("Pinned vector constructor family no longer matches")
        text = text[:start] + constructors + text[end:]
        for bound in ("m_size", "other.m_size"):
            before = f"for (size_t i = {bound}; i > 0; i--)\n\t\t\t\t\tconstruct(pDst++, *pSrc++);"
            text = replace(text, before, "for (m_size = 0; m_size < other.m_size; ++m_size)\n\t\t\t\t\tconstruct(pDst++, *pSrc++);")
        for initializer in ("T", "T(init)"):
            before = f"""\t\tT* q = p + n;
\t\tfor (; p != q; ++p)
\t\t\tnew (static_cast<void*>(p)) {initializer};"""
            after = f"""\t\tT* const first = p;
\t\tT* q = p + n;
\t\ttry {{
\t\t\tfor (; p != q; ++p)
\t\t\t\tnew (static_cast<void*>(p)) {initializer};
\t\t}} catch (...) {{
\t\t\twhile (p != first) (--p)->~T();
\t\t\tthrow;
\t\t}}"""
            text = replace(text, before, after)
        return replace(text, """\t\t\t\tif (!BASISU_IS_BITWISE_COPYABLE(T))
\t\t\t\t{
\t\t\t\t\tscalar_type<T>::construct_array(m_p + m_size, new_size - m_size);""", """\t\t\t\tif (new_size > m_size && !BASISU_IS_BITWISE_COPYABLE(T))
\t\t\t\t{
\t\t\t\t\tscalar_type<T>::construct_array(m_p + m_size, new_size - m_size);""")
    if name.endswith("texture2.c"):
        return "#include <errno.h>\n" + replace(text, """        This->pDfd = ktxVk2dfd(createInfo->vkFormat);
        if (!This->pDfd)
            return KTX_INVALID_VALUE;  // Format is unknown or unsupported.""", """        errno = 0;
        This->pDfd = ktxVk2dfd(createInfo->vkFormat);
        if (!This->pDfd)
            return errno == ENOMEM ? KTX_OUT_OF_MEMORY : KTX_INVALID_VALUE;""")
    if name.endswith("basisu_transcoder.cpp"):
        text = replace(text, "\t\tg_initialized = true;\n\n\t\tinit_astc_block_grid_data_hash();\n\n\t\tcompute_base_ofs_requantize_tabs();", "\t\tinit_astc_block_grid_data_hash();\n\n\t\tcompute_base_ofs_requantize_tabs();\n\t\tg_initialized = true;")
        start = text.index("\tvoid init_astc_block_grid_data_hash()\n\t{")
        end = text.index('\n#include "basisu_idct.h"', start)
        body = text[start:end]
        body = replace(body, "g_astc_block_grid_data_hash.clear();", "astc_block_grid_data_hash_t pending;")
        body = replace(body, "g_astc_block_grid_data_hash.reserve(384);", "pending.reserve(384);")
        body = replace(body, "g_astc_block_grid_data_hash.insert(", "pending.insert(")
        body = replace(body, "\t\t} // block_w\n\t}", "\t\t} // block_w\n\t\tg_astc_block_grid_data_hash.swap(pending);\n\t}")
        text = text[:start] + body + text[end:]
        text = replace(text, """\t\t\t\tint* pZigZag = generate_zigzag_order(grid_width, grid_height);

\t\t\t\tbasisu::int_vec v(grid_width * grid_height);
\t\t\t\tmemcpy(v.data(), pZigZag, sizeof(int) * grid_width * grid_height);

\t\t\t\tfree(pZigZag);""", """\t\t\t\tstd::unique_ptr<int, decltype(&free)> pZigZag(generate_zigzag_order(grid_width, grid_height), free);
\t\t\t\tif (!pZigZag) throw std::bad_alloc();

\t\t\t\tbasisu::int_vec v(grid_width * grid_height);
\t\t\t\tmemcpy(v.data(), pZigZag.get(), sizeof(int) * grid_width * grid_height);""")
        start = text.index("\tvoid init_transcoding_tables()\n\t{")
        end = text.index("\n\tconst uint16_t g_total_unique_patterns", start)
        body = text[start:end]
        body = replace(body, "\t\tif (g_encoder_trial_modes[0].size())\n\t\t\treturn;", "\t\tstatic bool initialized = false;\n\t\tif (initialized) return;\n\t\ttry {")
        body = replace(body, "\t\t} // i\n\t}", """\t\t} // i
\t\t} catch (...) {
\t\t\tfor (uint32_t i = 0; i < astc_helpers::cTOTAL_BLOCK_SIZES; i++) {
\t\t\t\tg_encoder_trial_modes[i].clear();
\t\t\t\tg_grouped_encoder_trial_modes[i].clear();
\t\t\t\tg_grid_weight_dcts[i].m_grid_dim_key_vals.clear();
\t\t\t}
\t\t\tthrow;
\t\t}
\t\tinitialized = true;
\t}""")
        return "#include <memory>\n" + text[:start] + body + text[end:]
    if name.endswith("texture.c"):
        before = """    This->_protected = (struct ktxTexture_protected *)
                                malloc(sizeof(struct ktxTexture_protected));
    stream = ktxTexture_getStream(This);"""
        text = replace(text, before, before.replace("    stream =", "    if (!This->_protected) {\n        pStream->destruct(pStream);\n        return KTX_OUT_OF_MEMORY;\n    }\n    stream ="))
        for texture in ("tex1", "tex2"):
            text = replace(text, f"        if ({texture} == NULL)\n            return KTX_OUT_OF_MEMORY;", f"        if ({texture} == NULL) {{\n            pStream->destruct(pStream);\n            return KTX_OUT_OF_MEMORY;\n        }}")
        return text
    if name.endswith("createdfd.c"):
        text, count = re.subn(r"(\bDFD = (?:\(uint32_t \*\) malloc|writeHeader)\([\s\S]*?;)", r"\1\n    if (!DFD) return NULL;", text)
        if count != 11:
            raise ValueError("Pinned DFD allocation chain no longer matches")
        return text
    if name.endswith("basisu_containers_impl.h"):
        text, count = re.subn(r'if \(!new_p\)\s*\{[\s\S]*?container_abort\("elemental_vector::increase_capacity: (?:realloc|malloc)\(\) failed allocating %zu bytes", desired_size\);\s*\}', "if (!new_p) throw std::bad_alloc();", text)
        if count != 2:
            raise ValueError("Pinned container allocation chain no longer matches")
        return text
    if name.endswith("uthash.h"):
        return replace(text, "#define uthash_fatal(msg) exit(-1)", "#ifndef uthash_fatal\n#define uthash_fatal(msg) exit(-1)\n#endif")
    if name.endswith("hashlist.c"):
        text = replace(text, '#include "uthash.h"', '#define uthash_fatal(message) goto hash_allocation_failed\n#include "uthash.h"')
        text = replace(text, """        kv = (ktxKVListEntry*)malloc(sizeof(ktxKVListEntry) + keyLen + valueLen);
        /* Put key first */""", """        kv = (ktxKVListEntry*)malloc(sizeof(ktxKVListEntry) + keyLen + valueLen);
        if (!kv) return KTX_OUT_OF_MEMORY;
        memset(&kv->hh, 0, sizeof(kv->hh));
        ktxKVListEntry* previous = *pHead;
        /* Put key first */""")
        return replace(text, """        HASH_ADD_KEYPTR( hh, *pHead, kv->key, kv->keyLen-1, kv);
        return KTX_SUCCESS;""", """        HASH_ADD_KEYPTR( hh, *pHead, kv->key, kv->keyLen-1, kv);
        return KTX_SUCCESS;
    hash_allocation_failed:
        if (!previous) {
            if (kv->hh.tbl) {
                HASH_BLOOM_FREE(kv->hh.tbl);
                free(kv->hh.tbl->buckets);
                free(kv->hh.tbl);
            }
            *pHead = NULL;
        } else {
            HASH_DEL(*pHead, kv);
        }
        free(kv);
        return KTX_OUT_OF_MEMORY;""")
    raise ValueError(name)


def main(root):
    prepared = []
    for name, (original, patched) in PINS.items():
        path = root / name
        contents = path.read_bytes()
        digest = hashlib.sha256(contents).hexdigest()
        if digest == patched:
            continue
        if digest != original:
            raise ValueError("Pinned allocation input changed: " + name)
        result = prepare(name, contents.decode("utf-8")).encode("utf-8")
        if hashlib.sha256(result).hexdigest() != patched:
            raise ValueError("Pinned allocation output changed: " + name)
        prepared.append((path, result))
    for path, contents in prepared:
        path.write_bytes(contents)


if __name__ == "__main__":
    main(Path(sys.argv[1]))
