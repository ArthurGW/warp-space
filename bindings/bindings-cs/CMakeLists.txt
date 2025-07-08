# ===================================================
# Set up CppSharp build
# ===================================================
set(CPPSHARP_SOURCE_DIR "../../external/CppSharp")
cmake_path(ABSOLUTE_PATH CPPSHARP_SOURCE_DIR NORMALIZE)
cmake_path(APPEND CPPSHARP_SOURCE_DIR "bin" "Release" "CppSharp.CLI.exe" OUTPUT_VARIABLE CPPSHARP_EXE)

add_custom_command(
    OUTPUT "${CPPSHARP_EXE}"
#    COMMAND build.sh ARGS clone_llvm
#    COMMAND build.sh ARGS build_llvm
#    COMMAND build.sh ARGS package_llvm
    COMMAND build.sh ARGS generate -configuration Release -platform x64 -target-framework .netstandard2.1
    COMMAND build.sh ARGS -configuration Release -platform x64
    VERBATIM
    DEPENDS "${CPPSHARP_SOURCE_DIR}/version.json"
    WORKING_DIRECTORY "${CPPSHARP_SOURCE_DIR}/build")

add_custom_target(CppSharp
    COMMAND echo CppSharp target finished
    VERBATIM
    DEPENDS "${CPPSHARP_EXE}")

# ===================================================
# Build the bindings C# library using CppSharp
# ===================================================
cmake_path(APPEND CMAKE_CURRENT_BINARY_DIR "Bindings" OUTPUT_VARIABLE TARGET_DIR)
cmake_path(APPEND TARGET_DIR "Bindings.dll" OUTPUT_VARIABLE TARGET_PATH)

# LIST generator: https://cmake.org/cmake/help/v3.31/manual/cmake-generator-expressions.7.html#whitespace-and-quoting
add_custom_command(
    OUTPUT "${TARGET_PATH}"
    COMMAND "${CPPSHARP_EXE}"
        ARGS
            --iln=bindings-cpp
            "$<LIST:TRANSFORM,$<SHELL_PATH:$<TARGET_PROPERTY:bindings-cpp,INCLUDE_DIRECTORIES>>,PREPEND,-I=>"
            -L=$<SHELL_PATH:$<TARGET_FILE_DIR:bindings-cpp>>
            --output=$<SHELL_PATH:${TARGET_DIR}>
            $<$<CONFIG:Debug,RelWithDebInfo>:--debug>
#            -D=_ALLOW_COMPILER_AND_STL_VERSION_MISMATCH
            --compile
            --module=Bindings
            --outputnamespace=Bindings
            --arch=x64
            --platform=win
            --generator=csharp
#            --checksymbols
            --unity
            -A=-std=c++14
            -v
            $<SHELL_PATH:$<TARGET_PROPERTY:bindings-cpp,SOURCE_DIR>/include/bindings.h>
    DEPENDS
        $<TARGET_FILE:bindings-cpp>
    COMMAND_EXPAND_LISTS
    VERBATIM
)

add_custom_target(bindings-cs
    ALL
    COMMAND echo Bindings target finished
    VERBATIM
    DEPENDS "${TARGET_PATH}")
add_dependencies(bindings-cs bindings-cpp CppSharp)

install(DIRECTORY ${TARGET_DIR}
    COMPONENT bindings
    DESTINATION "."  # "Bindings" is already in the target dir path
    FILES_MATCHING REGEX "/Bindings/([^/]*\.dll|[^/]*\.pdb)$")
