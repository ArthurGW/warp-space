enable_language(CXX)

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# ===================================================
# Set up clingo build
# ===================================================
set(CLINGO_SOURCE_DIR "../../external/clingo")
cmake_path(ABSOLUTE_PATH CLINGO_SOURCE_DIR NORMALIZE)

set(CLINGO_BUILD_TESTS OFF)
set(CLINGO_BUILD_EXAMPLES OFF)
set(CLINGO_BUILD_APPS OFF)
set(CLINGO_BUILD_STATIC ON)
set(CLINGO_BUILD_SHARED OFF)
set(CLINGO_INSTALL_LIB OFF)

set(CLASP_BUILD_APP OFF)
set(CLASP_USE_LOCAL_LIB_POTASSCO ON)
add_subdirectory("${CLINGO_SOURCE_DIR}" "./clingo")

# ===================================================
# Build the level generator C++ library
# ===================================================

function(tidy_program PROG OUT_VAR)
    string(REGEX REPLACE "%\\*[^%]*\\*%\n*" "" PROG "${PROG}")  # Remove comment blocks
    string(REGEX REPLACE "%[^\n]*\n+" "" PROG "${PROG}")  # Remove comment lines
    string(REPLACE "\\" "\\\\" PROG "${PROG}")  # Escape slashes
    string(REPLACE "\r\n" "\n" PROG "${PROG}")  # Standardise newlines
    string(REGEX REPLACE "\n\n+" "\n" PROG "${PROG}")  # Remove duplicate newlines
    string(REPLACE "\n" "\\n\\\n" PROG "${PROG}")  # Add line continuation characters to lines
    set(${OUT_VAR} ${PROG} PARENT_SCOPE)
endfunction()

# Embed the ASP programs in the DLL as strings
file(READ "programs/ship.lp" SHIP_PROGRAM)
file(READ "programs/portal.lp" PORTAL_PROGRAM)
tidy_program("${SHIP_PROGRAM}" SHIP_PROGRAM)
tidy_program("${PORTAL_PROGRAM}" PORTAL_PROGRAM)
configure_file("include/program.h.in" "include/program.h" ESCAPE_QUOTES @ONLY)

add_library(level-gen-cpp SHARED
        level_gen.cpp
        level.cpp
        ${CMAKE_CURRENT_BINARY_DIR}/include/program.h
        "programs/ship.lp"
        "programs/portal.lp")
set_property(TARGET level-gen-cpp PROPERTY OUTPUT_NAME LevelGenCpp)
target_include_directories(level-gen-cpp PUBLIC include)
target_include_directories(level-gen-cpp PRIVATE ${CMAKE_CURRENT_BINARY_DIR}/include)
target_link_libraries(level-gen-cpp PRIVATE libclingo)
target_compile_definitions(level-gen-cpp PRIVATE LEVEL_GEN_EXPORT)

install(TARGETS level-gen-cpp
        RUNTIME
        DESTINATION "LevelGenerator"
        COMPONENT level-gen)

install(FILES $<TARGET_PDB_FILE:level-gen-cpp>
        COMPONENT level-gen
        DESTINATION "LevelGenerator"
        CONFIGURATIONS Debug
        CONFIGURATIONS RelWithDebInfo
)

install(FILES "programs/ship.lp"
        COMPONENT level-gen
        DESTINATION "LevelGenerator/Resources/programs"
        RENAME "ship.txt")

install(FILES "programs/portal.lp"
        COMPONENT level-gen
        DESTINATION "LevelGenerator/Resources/programs"
        RENAME "portal.txt")

set(HEADER_PATH "include/level_gen.h")
cmake_path(ABSOLUTE_PATH HEADER_PATH NORMALIZE)
set_property(TARGET level-gen-cpp PROPERTY PUBLIC_HEADER ${HEADER_PATH})

# ===================================================
# Test the level generator C++ library
# ===================================================
if (BUILD_TESTING)
    add_library(level-gen-cpp-for-test SHARED
            level_gen.cpp
            level.cpp
            ${CMAKE_CURRENT_BINARY_DIR}/include/program.h
            "programs/ship.lp"
            "programs/portal.lp")
    target_include_directories(level-gen-cpp-for-test PUBLIC include)
    target_include_directories(level-gen-cpp-for-test PRIVATE ${CMAKE_CURRENT_BINARY_DIR}/include)
    target_link_libraries(level-gen-cpp-for-test PRIVATE libclingo)
    # Add TEST_BUILD define to get some extra test data
    target_compile_definitions(level-gen-cpp-for-test PRIVATE LEVEL_GEN_EXPORT TEST_BUILD)

    # Use clingo's bundled copy of Catch2 for simplicity
    add_subdirectory(${CLINGO_SOURCE_DIR}/third_party/catch "./catch")
    include(Catch)

    add_executable(level-gen-cpp-test
            tests/test-create.cpp
            tests/test-solve.cpp
            tests/test-cancel.cpp
            tests/test-fuzz.cpp
    )
    target_compile_definitions(level-gen-cpp-test PRIVATE TEST_BUILD)
    target_link_libraries(level-gen-cpp-test PRIVATE Catch2::Catch2WithMain level-gen-cpp-for-test)

    add_custom_command(TARGET level-gen-cpp-test
            POST_BUILD
            COMMAND ${CMAKE_COMMAND}
            ARGS -E copy_directory ${CMAKE_CURRENT_LIST_DIR}/programs $<TARGET_FILE_DIR:level-gen-cpp-test>/programs)

    catch_discover_tests(level-gen-cpp-test)
endif ()
