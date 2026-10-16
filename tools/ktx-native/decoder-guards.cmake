# The pinned reader ignores failed codebook decoding and then dereferences empty tables.
set(source "${electron2dktxcodec_SOURCE_DIR}/lib/src/basis_transcode.cpp")
file(READ "${source}" text)
set(before [=[    bit.decode_palettes(bgdh.endpointCount, BGD_ENDPOINTS_ADDR(bgd, imageCount),
                        bgdh.endpointsByteLength,
                        bgdh.selectorCount, BGD_SELECTORS_ADDR(bgd, bgdh, imageCount),
                        bgdh.selectorsByteLength);

    bit.decode_tables(BGD_TABLES_ADDR(bgd, bgdh, imageCount),
                      bgdh.tablesByteLength);]=])
set(after [=[    if (!bit.decode_palettes(bgdh.endpointCount, BGD_ENDPOINTS_ADDR(bgd, imageCount),
                        bgdh.endpointsByteLength,
                        bgdh.selectorCount, BGD_SELECTORS_ADDR(bgd, bgdh, imageCount),
                        bgdh.selectorsByteLength) ||
        !bit.decode_tables(BGD_TABLES_ADDR(bgd, bgdh, imageCount), bgdh.tablesByteLength)) {
        delete[] firstImages;
        return KTX_FILE_DATA_ERROR;
    }]=])
string(FIND "${text}" "${before}" position)
if(position LESS 0)
  message(FATAL_ERROR "Pinned KTX codebook error guard no longer matches its source")
endif()
string(REPLACE "${before}" "${after}" text "${text}")
# Both container paths share one initialization guard in the engine-owned adapter.
set(init_before [=[    static std::atomic<bool> transcoderInitialized(false);
    static std::mutex init_mutex;

    if (!transcoderInitialized.load(std::memory_order_acquire)) {
        std::lock_guard<std::mutex> lock(init_mutex);
        if (!transcoderInitialized.load(std::memory_order_relaxed)) {
            basisu_transcoder_init();
            transcoderInitialized.store(true, std::memory_order_release);
        }
    }]=])
string(FIND "${text}" "${init_before}" position)
if(position LESS 0)
  message(FATAL_ERROR "Pinned KTX initialization adapter no longer matches its source")
endif()
string(REPLACE "${init_before}" "    e2d_ktx_initialize_basis();" text "${text}")
string(REPLACE "#include <atomic>" "#include <atomic>\nextern \"C\" void e2d_ktx_initialize_basis();" text "${text}")
set(output "${CMAKE_CURRENT_BINARY_DIR}/basis-transcode.cpp")
set(previous "")
if(EXISTS "${output}")
  file(READ "${output}" previous)
endif()
if(NOT previous STREQUAL text)
  file(WRITE "${output}" "${text}")
endif()
set_source_files_properties("${source}" TARGET_DIRECTORY ktx_read PROPERTIES HEADER_FILE_ONLY TRUE)
target_sources(ktx_read PRIVATE "${output}")
target_include_directories(ktx_read PRIVATE "${electron2dktxcodec_SOURCE_DIR}/lib/src")
