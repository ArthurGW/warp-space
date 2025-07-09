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
# Build the bindings C# library using CppSharp
# ===================================================
cmake_path(APPEND CMAKE_CURRENT_BINARY_DIR $<CONFIG> OUTPUT_VARIABLE CSPROJ_DIR)
cmake_path(APPEND CSPROJ_DIR "build" OUTPUT_VARIABLE CSPROJ_BUILD_DIR)
cmake_path(APPEND CSPROJ_DIR "obj" OUTPUT_VARIABLE CSPROJ_OBJ_DIR)
cmake_path(APPEND CMAKE_CURRENT_BINARY_DIR $<CONFIG> "output" OUTPUT_VARIABLE OUTPUT_DIR)
cmake_path(APPEND OUTPUT_DIR "Bindings" OUTPUT_VARIABLE TARGET_DIR)
cmake_path(APPEND TARGET_DIR "Bindings.dll" OUTPUT_VARIABLE TARGET_PATH)

configure_file(Program.cs.in Program.cs.in NEWLINE_STYLE WIN32)
configure_file(Bindings.csproj.in Bindings.csproj.in NEWLINE_STYLE WIN32)

file(GENERATE
    OUTPUT ${CSPROJ_DIR}/Program.cs
    INPUT ${CMAKE_CURRENT_BINARY_DIR}/Program.cs.in
    NEWLINE_STYLE WIN32)
file(GENERATE
    OUTPUT ${CSPROJ_DIR}/Bindings.csproj
    INPUT ${CMAKE_CURRENT_BINARY_DIR}/Bindings.csproj.in
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
        COMMAND ${CSPROJ_BUILD_DIR}/Bindings.exe
        COMMAND ${CMAKE_COMMAND} -E remove_directory ${TARGET_DIR}/obj
        DEPENDS
            $<TARGET_FILE:bindings-cpp>
            ${CPPSHARP_RUNTIME}
            ${CSPROJ_DIR}/Bindings.csproj
        WORKING_DIRECTORY
            ${CSPROJ_DIR}
        VERBATIM
)

add_custom_target(bindings-cs
    ALL
    VERBATIM
    DEPENDS "${TARGET_PATH}")
add_dependencies(bindings-cs bindings-cpp CppSharp)

install(DIRECTORY ${TARGET_DIR}
    COMPONENT bindings
    DESTINATION .  # "Bindings" is already in the target dir path
    FILES_MATCHING REGEX "/Bindings/([^/]*\.dll|[^/]*\.pdb)$")
