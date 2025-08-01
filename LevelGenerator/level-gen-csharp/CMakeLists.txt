# ===================================================
# Set up CppSharp build
# ===================================================
set(CPPSHARP_SOURCE_DIR "../../external/CppSharp")
cmake_path(ABSOLUTE_PATH CPPSHARP_SOURCE_DIR NORMALIZE)
cmake_path(APPEND CPPSHARP_SOURCE_DIR "bin" "Release" OUTPUT_VARIABLE CPPSHARP_BINARY_DIR)
cmake_path(NATIVE_PATH CPPSHARP_BINARY_DIR NORMALIZE CPPSHARP_BINARY_DIR_NATIVE)
cmake_path(APPEND CPPSHARP_BINARY_DIR "CppSharp.Runtime.dll" OUTPUT_VARIABLE CPPSHARP_RUNTIME)

add_custom_command(
    OUTPUT "${CPPSHARP_RUNTIME}"
    COMMAND build.sh ARGS generate -configuration Release -platform x64 -target-framework .netstandard2.1
    COMMAND build.sh ARGS -configuration Release -platform x64
    VERBATIM
    DEPENDS "${CPPSHARP_SOURCE_DIR}/version.json"
    WORKING_DIRECTORY "${CPPSHARP_SOURCE_DIR}/build")

add_custom_target(CppSharp
    COMMAND echo CppSharp target finished
    VERBATIM
    DEPENDS "${CPPSHARP_RUNTIME}")

# ===================================================
# Build the level generator C# library using CppSharp
# ===================================================
cmake_path(APPEND CMAKE_CURRENT_BINARY_DIR $<CONFIG> OUTPUT_VARIABLE CSPROJ_DIR)
cmake_path(APPEND CSPROJ_DIR "build" OUTPUT_VARIABLE CSPROJ_BUILD_DIR)
cmake_path(APPEND CSPROJ_DIR "obj" OUTPUT_VARIABLE CSPROJ_OBJ_DIR)
cmake_path(APPEND CMAKE_CURRENT_BINARY_DIR $<CONFIG> "output" OUTPUT_VARIABLE OUTPUT_DIR)
cmake_path(APPEND OUTPUT_DIR "LevelGenerator" OUTPUT_VARIABLE TARGET_DIR)
cmake_path(APPEND TARGET_DIR "LevelGenerator.dll" OUTPUT_VARIABLE TARGET_PATH)

configure_file(Program.cs.in Program.cs.in NEWLINE_STYLE WIN32)
configure_file(LevelGenerator.csproj.in LevelGenerator.csproj.in NEWLINE_STYLE WIN32)

file(GENERATE
    OUTPUT ${CSPROJ_DIR}/Program.cs
    INPUT ${CMAKE_CURRENT_BINARY_DIR}/Program.cs.in
    NEWLINE_STYLE WIN32)
file(GENERATE
    OUTPUT ${CSPROJ_DIR}/LevelGenerator.csproj
    INPUT ${CMAKE_CURRENT_BINARY_DIR}/LevelGenerator.csproj.in
    NEWLINE_STYLE WIN32)

file(GLOB CLANG_LIB LIST_DIRECTORIES true CONFIGURE_DEPENDS ${CPPSHARP_SOURCE_DIR}/build/llvm/llvm-*/lib)

add_custom_command(
        OUTPUT "${TARGET_PATH}"
        COMMAND ${CMAKE_COMMAND} -E remove_directory ${CSPROJ_BUILD_DIR} ${CSPROJ_OBJ_DIR} ${OUTPUT_DIR}
        COMMAND dotnet build
            ARGS
                -o ${CSPROJ_BUILD_DIR}
                -c $<IF:$<CONFIG:Debug>,Debug,Release>
        COMMAND ${CMAKE_COMMAND} -E copy ${CPPSHARP_BINARY_DIR}/ijwhost.dll ${CPPSHARP_BINARY_DIR}/CppSharp.CppParser.dll ${CSPROJ_BUILD_DIR}
        COMMAND ${CMAKE_COMMAND} -E copy_directory ${CLANG_LIB} ${CSPROJ_BUILD_DIR}/lib
        COMMAND ${CSPROJ_BUILD_DIR}/LevelGenerator.exe
        COMMAND ${CMAKE_COMMAND} -E remove_directory ${TARGET_DIR}/obj
        DEPENDS
            $<TARGET_FILE:level-gen-cpp>
            ${CPPSHARP_RUNTIME}
            ${CSPROJ_DIR}/LevelGenerator.csproj
            ${CSPROJ_DIR}/Program.cs
        WORKING_DIRECTORY
            ${CSPROJ_DIR}
        VERBATIM
)

add_custom_target(level-gen-csharp
    ALL
    VERBATIM
    DEPENDS "${TARGET_PATH}")
add_dependencies(level-gen-csharp level-gen-cpp CppSharp)

install(DIRECTORY ${TARGET_DIR}
    COMPONENT level-gen
    DESTINATION .  # "LevelGenerator" is already in the target dir path
    FILES_MATCHING REGEX "/LevelGenerator/([\.A-Za-z\-]+\.dll|[\.A-Za-z\-]+\.pdb)$")
