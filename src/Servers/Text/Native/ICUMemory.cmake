# Preserve the pinned source while making failed dictionary preparation terminate.
set(icu_cache_path "${ICU_ROOT}/common/rbbi_cache.cpp")
file(READ "${icu_cache_path}" icu_cache)
set(icu_cache_anchor [=[
            foundBreakCount += lbe->findBreaks(text, current, rangeEnd, fBreaks, fBI->fIsPhraseBreaking, status);
        }
]=])
string(FIND "${icu_cache}" "${icu_cache_anchor}" icu_cache_position)
if(icu_cache_position LESS 0)
    message(FATAL_ERROR "The pinned dictionary allocation guard no longer matches ICU.")
endif()
set(icu_cache_guard [=[
        // Failed cached property setup can leave an engine that cannot advance.
        if (U_FAILURE(status) || UTEXT_GETNATIVEINDEX(text) <= current) {
            e2d_icu_break_error = U_FAILURE(status) ? status : U_MEMORY_ALLOCATION_ERROR;
            fBreaks.removeAllElements();
            return;
        }
]=])
string(REPLACE "${icu_cache_anchor}" "${icu_cache_anchor}${icu_cache_guard}" icu_cache "${icu_cache}")
set(icu_cache_output "${CMAKE_CURRENT_BINARY_DIR}/rbbi_cache_memory.cpp")
file(WRITE "${icu_cache_output}" "#include <cstdint>\nthread_local int32_t e2d_icu_break_error;\n${icu_cache}")
list(REMOVE_ITEM ICU_RUNTIME_SOURCES "${icu_cache_path}")
list(APPEND ICU_RUNTIME_SOURCES "${icu_cache_output}")
set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${icu_cache_path}")

set(icu_locale_path "${ICU_ROOT}/common/locid.cpp")
file(READ "${icu_locale_path}" icu_locale)
set(icu_locale_anchor "        id = uprv_getDefaultLocaleID();   // This function not thread safe? TODO: verify.")
string(FIND "${icu_locale}" "${icu_locale_anchor}" icu_locale_position)
if(icu_locale_position LESS 0)
    message(FATAL_ERROR "The pinned default-locale allocation guard no longer matches ICU.")
endif()
string(REPLACE "${icu_locale_anchor}" "${icu_locale_anchor}\n        if (id == nullptr) { status = U_MEMORY_ALLOCATION_ERROR; return gDefaultLocale; }" icu_locale "${icu_locale}")
set(icu_locale_output "${CMAKE_CURRENT_BINARY_DIR}/locid_memory.cpp")
file(WRITE "${icu_locale_output}" "${icu_locale}\nextern \"C\" int32_t e2d_icu_prepare_locales() {\n    U_NAMESPACE_USE\n    UErrorCode status = U_ZERO_ERROR;\n    umtx_initOnce(gLocaleCacheInitOnce, locale_init, status);\n    return status;\n}\n")
list(REMOVE_ITEM ICU_RUNTIME_SOURCES "${icu_locale_path}")
list(APPEND ICU_RUNTIME_SOURCES "${icu_locale_output}")
set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${icu_locale_path}")

set(icu_dictionary_path "${ICU_ROOT}/common/dictbe.cpp")
file(READ "${icu_dictionary_path}" icu_dictionary)
set(icu_dictionary_anchor "    utext_openUnicodeString(&fu, &inString, &status);")
string(FIND "${icu_dictionary}" "${icu_dictionary_anchor}" icu_dictionary_position)
if(icu_dictionary_position LESS 0)
    message(FATAL_ERROR "The pinned CJK allocation guard no longer matches ICU.")
endif()
string(REPLACE "${icu_dictionary_anchor}" "${icu_dictionary_anchor}\n    if (U_FAILURE(status)) { utext_close(&fu); return 0; }" icu_dictionary "${icu_dictionary}")
string(REPLACE "fDictionary(adoptDictionary), isCj(false)" "fDictionary(adoptDictionary), isCj(false), fSkipSet(status)" icu_dictionary "${icu_dictionary}")
set(icu_dictionary_output "${CMAKE_CURRENT_BINARY_DIR}/dictbe_memory.cpp")
file(WRITE "${icu_dictionary_output}" "${icu_dictionary}")
list(REMOVE_ITEM ICU_RUNTIME_SOURCES "${icu_dictionary_path}")
list(APPEND ICU_RUNTIME_SOURCES "${icu_dictionary_output}")
set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${icu_dictionary_path}")

set(icu_resource_path "${ICU_ROOT}/common/uresbund.cpp")
file(READ "${icu_resource_path}" icu_resource)
set(icu_resource_anchor "uprv_free(r);")
string(REGEX MATCHALL "uprv_free\\(r\\)" icu_resource_matches "${icu_resource}")
list(LENGTH icu_resource_matches icu_resource_count)
if(NOT icu_resource_count EQUAL 3)
    message(FATAL_ERROR "The pinned partial-resource cleanup no longer matches ICU.")
endif()
string(REPLACE "${icu_resource_anchor}" "free_entry(r);" icu_resource "${icu_resource}")
set(icu_resource_output "${CMAKE_CURRENT_BINARY_DIR}/uresbund_memory.cpp")
file(WRITE "${icu_resource_output}" "${icu_resource}")
list(REMOVE_ITEM ICU_RUNTIME_SOURCES "${icu_resource_path}")
list(APPEND ICU_RUNTIME_SOURCES "${icu_resource_output}")
set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${icu_resource_path}")

set(icu_iterator_path "${ICU_ROOT}/common/rbbi.cpp")
file(READ "${icu_iterator_path}" icu_iterator)
set(icu_iterator_anchor [=[
    fData = new RBBIDataWrapper(udm, status); // status checked in constructor
    if (U_FAILURE(status)) {return;}
    if(fData == nullptr) {
        status = U_MEMORY_ALLOCATION_ERROR;
        return;
    }
]=])
string(FIND "${icu_iterator}" "${icu_iterator_anchor}" icu_iterator_position)
if(icu_iterator_position LESS 0)
    message(FATAL_ERROR "The pinned iterator adoption guard no longer matches ICU.")
endif()
set(icu_iterator_guard [=[
    if (U_FAILURE(status)) { udata_close(udm); return; }
    fData = new RBBIDataWrapper(udm, status);
    if (fData == nullptr) {
        udata_close(udm);
        status = U_MEMORY_ALLOCATION_ERROR;
        return;
    }
    if (U_FAILURE(status)) { return; }
]=])
string(REPLACE "${icu_iterator_anchor}" "${icu_iterator_guard}" icu_iterator "${icu_iterator}")
set(icu_factory_anchor "    gLanguageBreakFactories = new UStack(_deleteFactory, nullptr, status);")
string(REPLACE "${icu_factory_anchor}" "${icu_factory_anchor}\n    if (gLanguageBreakFactories == nullptr) status = U_MEMORY_ALLOCATION_ERROR;" icu_iterator "${icu_iterator}")
set(icu_iterator_output "${CMAKE_CURRENT_BINARY_DIR}/rbbi_memory.cpp")
file(WRITE "${icu_iterator_output}" "${icu_iterator}")
list(REMOVE_ITEM ICU_RUNTIME_SOURCES "${icu_iterator_path}")
list(APPEND ICU_RUNTIME_SOURCES "${icu_iterator_output}")
set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${icu_iterator_path}")

set(icu_rules_path "${ICU_ROOT}/common/rbbidata.cpp")
file(READ "${icu_rules_path}" icu_rules)
set(icu_rules_anchor "RBBIDataWrapper::RBBIDataWrapper(UDataMemory* udm, UErrorCode &status) {\n    init0();")
string(FIND "${icu_rules}" "${icu_rules_anchor}" icu_rules_position)
if(icu_rules_position LESS 0)
    message(FATAL_ERROR "The pinned compiled-rule ownership guard no longer matches ICU.")
endif()
string(REPLACE "${icu_rules_anchor}" "${icu_rules_anchor}\n    fUDataMem = udm;" icu_rules "${icu_rules}")
string(REPLACE "fRefCount     = 0;" "fRefCount     = 1;" icu_rules "${icu_rules}")
set(icu_rules_output "${CMAKE_CURRENT_BINARY_DIR}/rbbidata_memory.cpp")
file(WRITE "${icu_rules_output}" "${icu_rules}")
list(REMOVE_ITEM ICU_RUNTIME_SOURCES "${icu_rules_path}")
list(APPEND ICU_RUNTIME_SOURCES "${icu_rules_output}")
set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${icu_rules_path}")

set(icu_bundle_path "${ICU_ROOT}/common/resbund.cpp")
file(READ "${icu_bundle_path}" icu_bundle)
set(icu_bundle_anchor "    if (U_SUCCESS(status)) {\n        ures_close(&r);\n    }")
string(REPLACE "${icu_bundle_anchor}" "    ures_close(&r);" icu_bundle "${icu_bundle}")
string(REPLACE "    if(U_SUCCESS(status)){\n        ures_close(&r);\n    }" "    ures_close(&r);" icu_bundle "${icu_bundle}")
set(icu_bundle_output "${CMAKE_CURRENT_BINARY_DIR}/resbund_memory.cpp")
file(WRITE "${icu_bundle_output}" "${icu_bundle}")
list(REMOVE_ITEM ICU_RUNTIME_SOURCES "${icu_bundle_path}")
list(APPEND ICU_RUNTIME_SOURCES "${icu_bundle_output}")
set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${icu_bundle_path}")

set(icu_spoof_path "${ICU_ROOT}/i18n/uspoof_impl.cpp")
file(READ "${icu_spoof_path}" icu_spoof)
set(icu_spoof_anchor "    if (gDefaultSpoofData == nullptr) {\n        status = U_MEMORY_ALLOCATION_ERROR;")
string(FIND "${icu_spoof}" "${icu_spoof_anchor}" icu_spoof_position)
if(icu_spoof_position LESS 0)
    message(FATAL_ERROR "The pinned security-data adoption guard no longer matches ICU.")
endif()
string(REPLACE "${icu_spoof_anchor}" "${icu_spoof_anchor}\n        udata_close(udm);" icu_spoof "${icu_spoof}")
set(icu_spoof_output "${CMAKE_CURRENT_BINARY_DIR}/uspoof_impl_memory.cpp")
file(WRITE "${icu_spoof_output}" "${icu_spoof}")
list(REMOVE_ITEM ICU_RUNTIME_SOURCES "${icu_spoof_path}")
list(APPEND ICU_RUNTIME_SOURCES "${icu_spoof_output}")
set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${icu_spoof_path}")

set(icu_allowed_path "${ICU_ROOT}/i18n/uspoof.cpp")
file(READ "${icu_allowed_path}" icu_allowed)
set(icu_allowed_anchor "    if (clonedSet == nullptr || clonedSet->isBogus()) {\n        *status = U_MEMORY_ALLOCATION_ERROR;")
string(FIND "${icu_allowed}" "${icu_allowed_anchor}" icu_allowed_position)
if(icu_allowed_position LESS 0)
    message(FATAL_ERROR "The pinned allowed-character copy guard no longer matches ICU.")
endif()
string(REPLACE "${icu_allowed_anchor}" "${icu_allowed_anchor}\n        delete clonedSet;" icu_allowed "${icu_allowed}")
set(icu_allowed_output "${CMAKE_CURRENT_BINARY_DIR}/uspoof_memory.cpp")
file(WRITE "${icu_allowed_output}" "${icu_allowed}")
list(REMOVE_ITEM ICU_RUNTIME_SOURCES "${icu_allowed_path}")
list(APPEND ICU_RUNTIME_SOURCES "${icu_allowed_output}")
set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${icu_allowed_path}")
