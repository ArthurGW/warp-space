#include "level_gen.h"
#include "clingo.hh"

#include <memory>

namespace {

} // unnamed namespace

class LevelGenerator::LevelGenImpl {
    std::unique_ptr<Clingo::SolveControl> solver;

    LevelGenImpl() : solver(nullptr) {};
};


LevelGenerator::LevelGenerator() = default; //: impl(std::make_unique<LevelGenImpl>())

LevelGenerator& LevelGenerator::operator=(LevelGenerator&& other) noexcept = default;

LevelGenerator::LevelGenerator(LevelGenerator&& other) noexcept = default;

LevelGenerator::~LevelGenerator() = default;
