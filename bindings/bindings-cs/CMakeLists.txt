enable_language(CSharp)
#set(CMAKE_DOTNET_TARGET_FRAMEWORK "netstandard2.0")

# ===================================================
# Set up CppSharp build
# ===================================================
set(CPPSHARP_SOURCE_DIR "../../external/CppSharp")
cmake_path(ABSOLUTE_PATH CPPSHARP_SOURCE_DIR NORMALIZE)

add_custom_command(
        OUTPUT "${CPPSHARP_SOURCE_DIR}/bin/Release/CppSharp.Runtime.dll"
        COMMAND build.sh ARGS generate -configuration Release -platform x64 -target-framework .netstandard2.1
        COMMAND build.sh ARGS -configuration Release -platform x64
        VERBATIM
        DEPENDS "${CPPSHARP_SOURCE_DIR}/version.json"
        WORKING_DIRECTORY "${CPPSHARP_SOURCE_DIR}/build")

add_custom_target(CppSharp
        COMMAND echo CppSharp target finished
        VERBATIM
        DEPENDS "${CPPSHARP_SOURCE_DIR}/bin/Release/CppSharp.Runtime.dll")

# ===================================================
# Build the bindings C# library
# ===================================================
file(GENERATE
        OUTPUT "Program-$<CONFIG>.cs"
        INPUT Program.cs.in
        NEWLINE_STYLE WIN32)

add_library(bindings-cs SHARED "Program-$<CONFIG>.cs")
add_dependencies(bindings-cs bindings-cpp CppSharp)
