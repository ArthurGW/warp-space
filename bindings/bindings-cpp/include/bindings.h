#ifndef BINDINGS_H
#define BINDINGS_H

#ifdef BINDINGS_EXPORT
  #define BINDINGS_API __declspec(dllexport)
#else
  #define BINDINGS_API  _declspec(dllimport)
#endif

BINDINGS_API void hello();

class BINDINGS_API Test {
    public:
        Test() = default;
        Test(int x, const char* log);
        unsigned short x = 2;

        Test(Test&&) = default;
        Test(const Test&) = default;
        Test& operator=(const Test&) = default;

};

BINDINGS_API Test createTest(const char *txt);
BINDINGS_API Test* createTest2();


#endif //BINDINGS_H
