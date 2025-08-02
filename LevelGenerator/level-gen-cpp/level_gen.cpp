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
        LevelGenImpl(unsigned max_num_levels, unsigned width, unsigned height, unsigned min_rooms, unsigned max_rooms,
                     size_t seed, const char* prog, unsigned num_threads) : width(width), height(height), min_rooms(min_rooms), max_rooms(
                max_rooms), solver(std::make_unique<Clingo::Control>())
        {
            auto config = solver->configuration();
            if (num_threads > 1)
            {
                config["solve.parallel_mode"] = std::to_string(num_threads).c_str();;
            }

            // Note - this is the upper limit, the solver may stop if an optimum is found
            config["solve.models"] = std::to_string(max_num_levels).c_str();
            if (seed == 0)
            {
                seed = std::random_device()();
            }
            config["solver.seed"] = std::to_string(seed).c_str();
            config["solver.rand_freq"] = "1.0";  // Always choose randomly where possible

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
            for (const auto& m : solver->solve())
            {
                const auto costs = m.cost();
                const auto total_cost = std::accumulate(costs.cbegin(), costs.cend(), (decltype(costs)::value_type)0);

                const auto model_symbols = m.symbols();
                std::vector<clingo_symbol_t> transformed_symbols(model_symbols.size(), (clingo_symbol_t)0);
                std::transform(model_symbols.cbegin(), model_symbols.cend(), transformed_symbols.begin(),
                               [](const auto& sym) { return sym.to_c(); });
                levels.emplace_back(total_cost, transformed_symbols);
                out << "Model: ";
                for (auto& atom : m.symbols())
                {
                    out << " " << atom;
                }
                out << std::endl;
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

        size_t num_levels() const {
            return levels.size();
        }

        friend class LevelGenerator;
};

LevelGenerator::LevelGenerator(unsigned max_num_levels, unsigned width, unsigned height, unsigned min_rooms,
                               unsigned max_rooms, size_t seed, const char* program, unsigned num_threads) : impl(
        std::make_unique<LevelGenImpl>(max_num_levels, width, height, min_rooms, max_rooms, seed, program, num_threads))
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

Level* LevelGenerator::best_level()
{
    return impl->best_level();
}

size_t LevelGenerator::num_levels() const
{
    return impl->num_levels();
}
