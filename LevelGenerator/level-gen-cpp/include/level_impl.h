#ifndef LEVEL_IMPL_H
#define LEVEL_IMPL_H

#include "level_gen.h"
#include "clingo.hh"

class Level::LevelImpl {
    private:
        explicit LevelImpl(const Clingo::Model& model);
        LevelImpl();

        friend class LevelGenerator;
        friend std::unique_ptr<LevelImpl> std::make_unique<LevelImpl>();
};


#endif //LEVEL_IMPL_H
