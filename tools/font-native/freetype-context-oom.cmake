# Keep the pinned source checkout unchanged while backporting the context-allocation guard.
# https://github.com/freetype/freetype/blob/3f6dad8e4f82c4f9c7cf9ffbf82972b912c08e48/src/truetype/ttobjs.c#L1036
set(ft_truetype "${freetype_SOURCE_DIR}/src/truetype")
set(ft_override "${CMAKE_CURRENT_BINARY_DIR}/freetype-context-oom")
file(READ "${ft_truetype}/ttobjs.c" ft_objects)
string(REPLACE "\r\n" "\n" ft_objects "${ft_objects}")
set(ft_before "    size->context = TT_New_Context( (TT_Driver)face->root.driver );

    size->max_function_defs    = maxp->maxFunctionDefs;")
set(ft_after "    size->context = TT_New_Context( (TT_Driver)face->root.driver );
    /* Electron2D: a failed context allocation must not reach the interpreter. */
    if ( !size->context )
      return FT_THROW( Could_Not_Find_Context );

    size->max_function_defs    = maxp->maxFunctionDefs;")
string(FIND "${ft_objects}" "${ft_before}" ft_context_position)
if(ft_context_position EQUAL -1)
    message(FATAL_ERROR "Pinned FreeType bytecode initializer changed; review its context-allocation guard")
endif()
string(REPLACE "${ft_before}" "${ft_after}" ft_objects "${ft_objects}")
file(MAKE_DIRECTORY "${ft_override}")
configure_file("${ft_truetype}/truetype.c" "${ft_override}/truetype.c" COPYONLY)
file(GENERATE OUTPUT "${ft_override}/ttobjs.c" CONTENT "${ft_objects}")
get_target_property(ft_sources freetype SOURCES)
list(FIND ft_sources "src/truetype/truetype.c" ft_source_position)
if(ft_source_position EQUAL -1)
    message(FATAL_ERROR "Pinned FreeType TrueType compilation unit changed")
endif()
list(REMOVE_ITEM ft_sources "src/truetype/truetype.c")
list(APPEND ft_sources "${ft_override}/truetype.c")
set_property(TARGET freetype PROPERTY SOURCES "${ft_sources}")
target_include_directories(freetype PRIVATE "${ft_truetype}")
