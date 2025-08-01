#include "level_gen.h"
#include "clingo.hh"

#include <memory>
#include <sstream>
#include <fstream>
#include <numeric>
#include <random>

class LevelGenerator::LevelGenImpl
{
    public:
        LevelGenImpl(unsigned num_levels, unsigned width, unsigned height, unsigned min_rooms, unsigned max_rooms,
                     size_t seed, const char* prog) : width(width), height(height), min_rooms(min_rooms), max_rooms(
                max_rooms), solver(std::make_unique<Clingo::Control>())
        {
            auto config = solver->configuration();
            config["solve.parallel_mode"] = "4";
            config["solver.rand_freq"] = "1.0";

            config["solve.models"] = std::to_string(num_levels).c_str();;
            if (seed == 0)
            {
                seed = std::random_device()();
            }
            config["solver.seed"] = std::to_string(seed).c_str();

            if (prog)
            {
                program = prog;  // Copy to std::string for safe storage
            }
        }

    private:
        std::unique_ptr<Clingo::Control> solver;
        std::vector<Level> levels;
        std::string solutions;

        const unsigned width;
        const unsigned height;
        const unsigned min_rooms;
        const unsigned max_rooms;
        std::string program;

        const char* solve()
        {
            if (program.empty())
            {
                // Load from file
                const char* error = nullptr;
                std::ifstream ship;
                auto success = false;
                try
                {
                    ship.open("programs/ship.lp");
                    std::stringstream buffer;
                    if (!(buffer << ship.rdbuf()))
                    {
                        throw std::exception("failed to read ship.lp");
                    }
                    solver->add("base", {}, buffer.str().c_str());
                    success = true;
                }
                catch (const std::exception& e)
                {
                    std::cout << e.what();
                    error = e.what();
                }
                if (ship.is_open())
                {
                    ship.close();
                }
                if (!success)
                {
                    throw std::exception(
                            (std::string("error creating logic program: ") + (error ? error : "")).c_str());
                }
            }
            else
            {
                // Set externally
                solver->add("base", {}, program.c_str());
            }

            // Add inputs
            std::stringstream inputs;
            inputs
                    << "#const width = "
                    << Clingo::Number(static_cast<int>(width))
                    << "."
                    << std::endl
                    << "#const height = "
                    << Clingo::Number(static_cast<int>(height))
                    << "."
                    << std::endl
                    << "#const min_rooms = "
                    << Clingo::Number(static_cast<int>(min_rooms))
                    << "."
                    << std::endl
                    << "#const max_rooms = "
                    << Clingo::Number(static_cast<int>(max_rooms))
                    << "."
                    << std::endl;
            solver->add("base", {}, inputs.str().c_str());

            solver->ground({{"base", {}}});

            std::ostringstream out;
            for (auto& m : solver->solve())
            {
                const auto costs = m.cost();
                const auto total_cost = std::accumulate(costs.cbegin(), costs.cend(), (int64_t) 0);

                const auto model_symbols = m.symbols();
                std::vector<uint64_t> transformed_symbols(model_symbols.size(), 0);
                std::transform(model_symbols.cbegin(), model_symbols.cend(), transformed_symbols.begin(),
                               [](const auto& sym) { return sym.to_c(); });
                levels.emplace_back(total_cost, transformed_symbols);
                out << "Model: ";
                for (auto& atom : m.symbols())
                {
                    out << " " << atom;
                }
                out << "\n";
            }
            solutions = out.str();
            return solutions.c_str();
        }

        Level* best_level()
        {
            if (levels.empty())
            {
                return nullptr;
            }

            return &(*std::min_element(levels.begin(), levels.end(), [&](const auto& left, const auto& right)
            {
                return left.get_cost() <= right.get_cost();
            }));
        }

        friend class LevelGenerator;
};

LevelGenerator::LevelGenerator(unsigned num_levels, unsigned width, unsigned height, unsigned min_rooms,
                               unsigned max_rooms, size_t seed, const char* program) : impl(
        std::make_unique<LevelGenImpl>(num_levels, width, height, min_rooms, max_rooms, seed, program))
{}

LevelGenerator& LevelGenerator::operator=(LevelGenerator&& other) noexcept = default;

LevelGenerator::LevelGenerator(LevelGenerator&& other) noexcept = default;

LevelGenerator::~LevelGenerator() = default;

const char* LevelGenerator::solve()
{
    return impl->solve();
}

const char* LevelGenerator::solve_safe()
{
    try
    {
        return impl->solve();
    }
    catch (const std::exception& e)
    {
        return e.what();
    }
}

Level* LevelGenerator::best_level() const
{
    return impl->best_level();
}
