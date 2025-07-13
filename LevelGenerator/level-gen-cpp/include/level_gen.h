#ifndef LEVEL_GEN_H
#define LEVEL_GEN_H

#ifdef LEVEL_GEN_EXPORT
  #define LEVEL_GEN_API __declspec(dllexport)
#else
  #define LEVEL_GEN_API  _declspec(dllimport)
#endif

#define CS_IGNORE

LEVEL_GEN_API void hello();

class LEVEL_GEN_API Test {
    public:
        Test() = default;
        Test(int x, const char* log);
        unsigned short x = 3;

        Test(Test&& other) = default;
        CS_IGNORE Test(const Test& other) = default;
        CS_IGNORE Test& operator=(const Test& other) = default;

};

LEVEL_GEN_API Test createTest(const char *txt);
LEVEL_GEN_API Test* createTest2();


#endif // LEVEL_GEN_H
