#ifndef LEVEL_IMPL_H
#define LEVEL_IMPL_H

#include "level_gen.h"
#include "clingo.hh"

class Level::LevelImpl {
    public:
        explicit LevelImpl(const Clingo::SymbolVector& symbols);
        LevelImpl();

    private:
        Clingo::SymbolVector symbols;

        friend class Level;
};


#endif //LEVEL_IMPL_H
