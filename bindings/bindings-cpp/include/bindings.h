#ifndef BINDINGS_H
#define BINDINGS_H

#ifdef BINDINGS_EXPORT
  #define BINDINGS_API __declspec(dllexport)
#else
  #define BINDINGS_API  _declspec(dllimport)
#endif

#define CS_IGNORE

BINDINGS_API void hello();

class BINDINGS_API Test {
    public:
        Test() = default;
        Test(int x, const char* log);
        unsigned short x = 3;

        Test(Test&& other) = default;
        CS_IGNORE Test(const Test& other) = default;
        CS_IGNORE Test& operator=(const Test& other) = default;

};

BINDINGS_API Test createTest(const char *txt);
BINDINGS_API Test* createTest2();


#endif //BINDINGS_H
