# Keep decoder scratch storage in the face's supplied allocation domain.
set(ft_sfnt "${freetype_SOURCE_DIR}/src/sfnt")
set(ft_woff2_override "${CMAKE_CURRENT_BINARY_DIR}/freetype-woff2-memory")
file(READ "${ft_sfnt}/sfwoff2.c" ft_woff2)
string(REPLACE "\r\n" "\n" ft_woff2 "${ft_woff2}")
string(SHA256 ft_woff2_hash "${ft_woff2}")
if(NOT ft_woff2_hash STREQUAL "9c67d2b0e2d0a88520bbfe132827252d0f7bd731bb735fba7a36575222d7cc72")
    message(FATAL_ERROR "Pinned FreeType WOFF2 decoder changed; review its allocator integration")
endif()
string(FIND "${ft_woff2}" "  static FT_Error\n  woff2_decompress(" ft_woff2_start)
string(FIND "${ft_woff2}" "  static WOFF2_Table\n  find_table(" ft_woff2_end)
math(EXPR ft_woff2_length "${ft_woff2_end} - ${ft_woff2_start}")
string(SUBSTRING "${ft_woff2}" ${ft_woff2_start} ${ft_woff2_length} ft_woff2_before)
set(ft_woff2_after [=[
  typedef struct WOFF2_MemoryRec_
  {
    FT_Memory  memory;
    FT_Bool    failed;
  } WOFF2_MemoryRec;


  static void*
  woff2_brotli_alloc( void*   opaque,
                     size_t  size )
  {
    WOFF2_MemoryRec*  owner = (WOFF2_MemoryRec*)opaque;
    void*            block = NULL;


    if ( size <= FT_LONG_MAX )
      block = owner->memory->alloc( owner->memory, (long)size );
    if ( !block )
      owner->failed = 1;
    return block;
  }


  static void
  woff2_brotli_free( void*  opaque,
                    void*  block )
  {
    WOFF2_MemoryRec*  owner = (WOFF2_MemoryRec*)opaque;


    if ( block )
      owner->memory->free( owner->memory, block );
  }


  static FT_Error
  woff2_decompress( FT_Byte*        dst,
                    FT_ULong        dst_size,
                    const FT_Byte*  src,
                    FT_ULong        src_size,
                    FT_Memory       memory )
  {
    WOFF2_MemoryRec       owner = { memory, 0 };
    BrotliDecoderState*   decoder;
    BrotliDecoderResult   result;
    size_t                available_in = src_size;
    size_t                available_out = dst_size;
    size_t                uncompressed_size = 0;
    const uint8_t*        next_in = src;
    uint8_t*              next_out = dst;


    decoder = BrotliDecoderCreateInstance( woff2_brotli_alloc,
                                           woff2_brotli_free, &owner );
    if ( !decoder )
      return FT_THROW( Out_Of_Memory );
    result = BrotliDecoderDecompressStream( decoder,
                                            &available_in, &next_in,
                                            &available_out, &next_out,
                                            &uncompressed_size );
    BrotliDecoderDestroyInstance( decoder );
    if ( owner.failed )
      return FT_THROW( Out_Of_Memory );
    if ( result != BROTLI_DECODER_RESULT_SUCCESS ||
         uncompressed_size != dst_size           )
    {
      FT_ERROR(( "woff2_decompress: Stream length mismatch.\n" ));
      return FT_THROW( Invalid_Table );
    }

    FT_TRACE2(( "woff2_decompress: Brotli stream decompressed.\n" ));
    return FT_Err_Ok;
  }


]=])
string(REPLACE "${ft_woff2_before}" "${ft_woff2_after}" ft_woff2 "${ft_woff2}")
string(REPLACE "    error = woff2_decompress( uncompressed_buf,
                              woff2.uncompressed_size,
                              stream->cursor,
                              woff2.totalCompressedSize );"
    "    error = woff2_decompress( uncompressed_buf,
                              woff2.uncompressed_size,
                              stream->cursor,
                              woff2.totalCompressedSize, memory );" ft_woff2 "${ft_woff2}")
file(MAKE_DIRECTORY "${ft_woff2_override}")
configure_file("${ft_sfnt}/sfnt.c" "${ft_woff2_override}/sfnt.c" COPYONLY)
file(GENERATE OUTPUT "${ft_woff2_override}/sfwoff2.c" CONTENT "${ft_woff2}")
get_target_property(ft_sources freetype SOURCES)
list(FIND ft_sources "src/sfnt/sfnt.c" ft_source_position)
if(ft_source_position EQUAL -1)
    message(FATAL_ERROR "Pinned FreeType SFNT compilation unit changed")
endif()
list(REMOVE_ITEM ft_sources "src/sfnt/sfnt.c")
list(APPEND ft_sources "${ft_woff2_override}/sfnt.c")
set_property(TARGET freetype PROPERTY SOURCES "${ft_sources}")
target_include_directories(freetype PRIVATE "${ft_sfnt}")
