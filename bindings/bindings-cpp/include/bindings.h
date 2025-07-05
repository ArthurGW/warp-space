#ifndef BINDINGS_CSCLINGO_H
#define BINDINGS_CSCLINGO_H

#ifdef BINDINGS_EXPORT
  #define BINDINGS_API __declspec(dllexport)
#else
  #define BINDINGS_API  _declspec(dllimport)
#endif

BINDINGS_API void hello();

class BINDINGS_API Test {
    public:
    Test() = default;
    unsigned short x = 0;

};


#endif //BINDINGS_CSCLINGO_H
