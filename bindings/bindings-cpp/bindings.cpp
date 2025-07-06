#include "bindings.h"
#include "clingo.hh"

#include <iostream>

void BINDINGS_API hello()
{
    std::cout << "Hello, World!" << std::endl;
}

Test createTest(const char *txt)
{
    return {2 ,txt};
}

Test::Test(int x, const char* log)  : x(x) {
    std::cout << log << std::endl;
}
