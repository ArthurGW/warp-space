#include "bindings.h"
#include "clingo.hh"

#include <iostream>
#include <memory>

void BINDINGS_API hello()
{
    std::cout << "Hello, World!" << std::endl;
}

Test createTest(const char *txt)
{
    return {2 ,txt};
}

Test* createTest2()
{ return std::make_unique<Test>(33, "asdsads").release(); }

Test::Test(int x, const char* log)  : x(x) {
    std::cout << log << std::endl;
}
