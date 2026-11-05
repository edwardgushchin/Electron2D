include_guard(GLOBAL)
if(MSVC AND CMAKE_GENERATOR MATCHES "Ninja")
    set(probe "${CMAKE_BINARY_DIR}/CMakeFiles/Electron2DIncludes")
    file(MAKE_DIRECTORY "${probe}")
    file(WRITE "${probe}/header.h" "\n")
    file(WRITE "${probe}/probe.cpp" "#include \"header.h\"\n")
    # Ninja compares raw compiler bytes; automatic console decoding can corrupt a localized prefix.
    execute_process(COMMAND "${CMAKE_CXX_COMPILER}" /nologo /showIncludes /c probe.cpp
        WORKING_DIRECTORY "${probe}" OUTPUT_VARIABLE output ERROR_VARIABLE output
        RESULT_VARIABLE result ENCODING NONE)
    if(NOT result EQUAL 0 OR NOT output MATCHES "(^|\n)([^:\r\n]+:[^:\r\n]+:[ \t]+)")
        message(FATAL_ERROR "Cannot determine the native compiler include prefix")
    endif()
    set(CMAKE_CL_SHOWINCLUDES_PREFIX "${CMAKE_MATCH_2}")
    string(SHA256 prefix_hash "${CMAKE_CL_SHOWINCLUDES_PREFIX}")
    foreach(language C CXX)
        set(CMAKE_${language}_CL_SHOWINCLUDES_PREFIX "${CMAKE_CL_SHOWINCLUDES_PREFIX}")
        # A prefix change alone does not invalidate Ninja's existing empty dependency records.
        set(CMAKE_${language}_COMPILER_LAUNCHER "${CMAKE_COMMAND}" -E env
            "ELECTRON2D_INCLUDE_PREFIX=${prefix_hash}" ${CMAKE_${language}_COMPILER_LAUNCHER})
    endforeach()
endif()
