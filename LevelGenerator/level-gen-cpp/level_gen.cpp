#include "level_gen.h"
#include "program.h"
#include "clingo.hh"

#include <memory>
#include <sstream>
#include <fstream>
#include <numeric>
#include <random>
#include <utility>

namespace {
    class CancelableSolveHandler : public Clingo::SolveEventHandler
    {
        public:
            explicit CancelableSolveHandler(std::function<bool(void)> check_cancel) : check_cancel(std::move(check_cancel)), Clingo::SolveEventHandler() {}

            bool on_model(Clingo::Model& model) override
            {
                if (check_cancel()) return false;

                return SolveEventHandler::on_model(model);
            }

        private:
            std::function<bool(void)> check_cancel;
    };
}


class LevelGenerator::LevelGenImpl
{
    public:
        LevelGenImpl(unsigned max_num_levels, unsigned width, unsigned height, unsigned min_rooms, unsigned max_rooms,
                unsigned num_breaches, unsigned num_portals, size_t seed, bool load_prog_from_file, unsigned num_threads)
                 : width(width), height(height), min_rooms(min_rooms), max_rooms(max_rooms), num_breaches(num_breaches),
                 num_portals(num_portals), solver(std::make_unique<Clingo::Control>())
        {
            auto config = solver->configuration();
            if (num_threads >= 1)
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

            if (!load_prog_from_file)
            {
                std::ostringstream stream;
                stream << ship_prog << std::endl << portal_prog;
                program = stream.str();
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
        const unsigned num_breaches;
        const unsigned num_portals;
        std::string program;

        void add_program_from_file(const char *path)
        {
            // Load from file
            const char* error = nullptr;
            std::ifstream prog;
            auto success = false;
            try
            {
                prog.open(path);
                std::stringstream buffer;
                if (!(buffer << prog.rdbuf()))
                {

                    throw std::exception(
                    (std::string("failed to read program: ") + path).c_str()
                    );
                }
                solver->add("base", {}, buffer.str().c_str());
                success = true;
            }
            catch (const std::exception& e)
            {
                std::cout << e.what();
                error = e.what();
            }
            if (prog.is_open())
            {
                prog.close();
            }
            if (!success)
            {
                throw std::exception(
                    (std::string("error creating logic program: ") + (error ? error : "")).c_str()
                );
            }
        }

        const char* solve(std::function<bool(void)> check_cancel)
        {
            if (program.empty())
            {
                add_program_from_file("programs/ship.lp");
                add_program_from_file("programs/portal.lp");
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
                    << std::endl
                    << "#const num_breaches = "
                    << Clingo::Number(static_cast<int>(num_breaches))
                    << "."
                    << std::endl
                    << "#const num_portals = "
                    << Clingo::Number(static_cast<int>(num_portals))
                    << "."
                    << std::endl;
            solver->add("base", {}, inputs.str().c_str());

            solver->ground({{"base", {}}});

            std::ostringstream out;

            std::unique_ptr<CancelableSolveHandler> event_handler = std::make_unique<CancelableSolveHandler>(
                    [&](){ return check_cancel && check_cancel(); });
            for (const auto& m : solver->solve(Clingo::LiteralSpan{}, event_handler.get()))
            {
                const auto costs = m.cost();
                const auto total_cost = std::accumulate(costs.cbegin(), costs.cend(), (decltype(costs)::value_type) 0);

                const auto model_symbols = m.symbols();
                std::vector<clingo_symbol_t> transformed_symbols(model_symbols.size(), (clingo_symbol_t) 0);
                std::transform(model_symbols.cbegin(), model_symbols.cend(), transformed_symbols.begin(),
                               [](const auto& sym) { return sym.to_c(); });
                levels.emplace_back(width, height, total_cost, transformed_symbols);
                out << "Model: ";
                for (auto& atom : m.symbols())
                {
                    out << " " << atom;
                }
                out << std::endl;

                if (check_cancel && check_cancel()) break;
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

            return &(*std::min_element(levels.begin(), levels.end(),
                                       [&](const auto& left, const auto& right)
                                       {
                                           return left.get_cost() <= right.get_cost();
                                       }
            ));
        }

        size_t num_levels() const
        {
            return levels.size();
        }

        void interrupt()
        {
            solver->interrupt();
        }

        friend class LevelGenerator;
};

LevelGenerator::LevelGenerator(unsigned max_num_levels, unsigned width, unsigned height, unsigned min_rooms,
                               unsigned max_rooms, unsigned num_breaches, unsigned num_portals,
                               size_t seed, bool load_prog_from_file, unsigned num_threads) : impl(
        std::make_unique<LevelGenImpl>(max_num_levels, width, height, min_rooms, max_rooms, num_breaches, num_portals,
                                       seed, load_prog_from_file, num_threads))
{}

LevelGenerator& LevelGenerator::operator=(LevelGenerator&& other) noexcept = default;

LevelGenerator::LevelGenerator(LevelGenerator&& other) noexcept = default;

LevelGenerator::~LevelGenerator() = default;

const char* LevelGenerator::solve(cancel_cb check_cancel)
{
    return impl->solve(check_cancel);
}

const char* LevelGenerator::solve_safe(cancel_cb check_cancel)
{
    try
    {
        return impl->solve(check_cancel);
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

size_t LevelGenerator::get_num_levels() const
{
    return impl->num_levels();
}

void LevelGenerator::interrupt()
{
    impl->interrupt();
}
