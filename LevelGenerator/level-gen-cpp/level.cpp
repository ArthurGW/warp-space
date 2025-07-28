#include "level_gen.h"
#include "level_impl.h"
#include "clingo.hh"

#include <memory>

Level::LevelImpl::LevelImpl() = default;

Level::LevelImpl::LevelImpl(const Clingo::SymbolVector& symbols) : symbols(symbols)
{

}


const MapSquare* Level::next_square()
{
    return nullptr;
}

const Room* Level::next_room()
{
    return nullptr;
}

const Adjacency* Level::next_adjacency()
{
    return nullptr;
}

int Level::get_cost()
{
    return 0;
}

Level::Level(std::unique_ptr<LevelImpl> impl) : impl(impl.release())
{

}

Level::~Level() = default;

Level& Level::operator=(Level&& other) noexcept = default;

Level::Level(Level&& other) noexcept = default;
