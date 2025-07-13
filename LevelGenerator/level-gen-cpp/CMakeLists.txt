enable_language(CXX)

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# ===================================================
# Set up clingo build
# ===================================================
set(CLINGO_SOURCE_DIR "../../external/clingo")
cmake_path(ABSOLUTE_PATH CLINGO_SOURCE_DIR NORMALIZE)

include(CheckCXXSourceCompiles)
list(APPEND CMAKE_MODULE_PATH "${CLINGO_SOURCE_DIR}/cmake")
include(clingo_functions)

set(CLINGO_BUILD_TESTS OFF)
set(CLINGO_BUILD_EXAMPLES OFF)
set(CLINGO_BUILD_APPS OFF)
set(CLINGO_BUILD_STATIC ON)
set(CLINGO_BUILD_SHARED OFF)
set(CLINGO_INSTALL_LIB OFF)
set(CLINGO_MAP_TYPE "hopscotch")

find_package(BISON "2.5")
find_package(RE2C "0.101")

add_subdirectory("${CLINGO_SOURCE_DIR}/third_party" "./clingo/third_party")
set(CLASP_BUILD_APP OFF)
set(CLASP_USE_LOCAL_LIB_POTASSCO ON)
add_subdirectory("${CLINGO_SOURCE_DIR}/clasp" "./clingo/clasp")

add_subdirectory("${CLINGO_SOURCE_DIR}/libreify" "./clingo/libreify")
add_subdirectory("${CLINGO_SOURCE_DIR}/libgringo" "./clingo/libgringo")
add_subdirectory("${CLINGO_SOURCE_DIR}/libclingo" "./clingo/libclingo")

# ===================================================
# Build the level generator C++ library
# ===================================================
add_library(level-gen-cpp SHARED level_gen.cpp)
target_include_directories(level-gen-cpp PUBLIC include)
target_link_libraries(level-gen-cpp PRIVATE libclingo)
target_compile_definitions(level-gen-cpp PRIVATE LEVEL_GEN_EXPORT)
install(TARGETS level-gen-cpp
    RUNTIME
        DESTINATION "LevelGenerator"
    COMPONENT level-gen)
    
install(DIRECTORY programs
    COMPONENT level-gen
    DESTINATION .  # "programs" is already in the target dir path
    FILES_MATCHING PATTERN "*.lp")

set(HEADER_PATH "include/level_gen.h")
cmake_path(ABSOLUTE_PATH HEADER_PATH NORMALIZE)
set_property(TARGET level-gen-cpp PROPERTY PUBLIC_HEADER ${HEADER_PATH})

# ===================================================
# Test the level generator C++ library
# ===================================================
if(BUILD_TESTING)
    # Use clingo's bundled copy of Catch2 for simplicity
    add_subdirectory(${CLINGO_SOURCE_DIR}/third_party/catch "./catch")
    include(Catch)

    add_executable(level-gen-cpp-test
            tests/test-create.cpp
            tests/test-solve.cpp
    )
    target_link_libraries(level-gen-cpp-test PRIVATE Catch2::Catch2WithMain level-gen-cpp)

    add_custom_command(TARGET level-gen-cpp-test
        POST_BUILD
        COMMAND ${CMAKE_COMMAND}
            ARGS -E copy_directory ${CMAKE_CURRENT_LIST_DIR}/programs $<TARGET_FILE_DIR:level-gen-cpp-test>/programs)

    catch_discover_tests(level-gen-cpp-test)
endif ()
