find_package(Git REQUIRED)

function(verify_source name revision directory)
    execute_process(COMMAND "${GIT_EXECUTABLE}" -C "${directory}" rev-parse HEAD
        OUTPUT_VARIABLE actual OUTPUT_STRIP_TRAILING_WHITESPACE RESULT_VARIABLE result ERROR_QUIET)
    if(NOT result EQUAL 0 OR NOT actual STREQUAL revision)
        message(FATAL_ERROR "${name} source revision ${actual} differs from the required ${revision}")
    endif()
    execute_process(COMMAND "${GIT_EXECUTABLE}" -C "${directory}" diff --quiet HEAD --
        RESULT_VARIABLE changed)
    if(NOT changed EQUAL 0)
        message(FATAL_ERROR "${name} source has tracked changes; use a clean pinned checkout")
    endif()
    message(STATUS "Verified ${name} source: ${actual}")
endfunction()
