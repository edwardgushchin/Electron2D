# Do not copy a bit buffer after its allocator has invalidated it.
set(info "${electron2dvorbis_SOURCE_DIR}/lib/info.c")
set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${info}")
file(SHA256 "${info}" info_hash)
if(NOT info_hash STREQUAL "03af6059fe7e50d0b37a0370707139227e6cd4cc74be56473cd22f0300739fca")
  message(FATAL_ERROR "Re-audit Vorbis packet publication when updating libvorbis.")
endif()
file(READ "${info}" info_text)
list(FIND vorbis_files "${info}" info_position)
if(info_position EQUAL -1)
  message(FATAL_ERROR "Pinned Vorbis target no longer owns lib/info.c")
endif()
string(REPLACE "if(_vorbis_pack_comment(&opb,vc)){" "if(oggpack_writecheck(&opb)||_vorbis_pack_comment(&opb,vc)||oggpack_writecheck(&opb)){" info_text "${info_text}")
foreach(kind info comment books)
  if(kind STREQUAL "comment")
    set(argument vc)
  else()
    set(argument vi)
  endif()
  string(REPLACE "if(_vorbis_pack_${kind}(&opb,${argument}))goto err_out;"
    "if(oggpack_writecheck(&opb)||_vorbis_pack_${kind}(&opb,${argument})||oggpack_writecheck(&opb))goto err_out;" info_text "${info_text}")
endforeach()
string(REPLACE "  memcpy(op->packet, opb.buffer, oggpack_bytes(&opb));"
  "  if(!op->packet){oggpack_writeclear(&opb);return OV_EFAULT;}\n  memcpy(op->packet, opb.buffer, oggpack_bytes(&opb));" info_text "${info_text}")
# This standalone packet is released by Ogg; backend-owned analysis headers are not.
string(REPLACE "op->packet = _ogg_malloc(oggpack_bytes(&opb));"
  "op->packet = e2d_ogv_allocate(oggpack_bytes(&opb));" info_text "${info_text}")
foreach(header header header1 header2)
  string(REPLACE "  memcpy(b->${header},opb.buffer,oggpack_bytes(&opb));"
    "  if(!b->${header}){ret=OV_EFAULT;goto err_out;}\n  memcpy(b->${header},opb.buffer,oggpack_bytes(&opb));" info_text "${info_text}")
endforeach()
list(REMOVE_ITEM vorbis_files "${info}")
list(APPEND vorbis_files "${CMAKE_CURRENT_BINARY_DIR}/info-memory.c")
file(WRITE "${CMAKE_CURRENT_BINARY_DIR}/info-memory.c" "#include \"memory.h\"\n${info_text}")
