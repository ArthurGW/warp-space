#include "level_gen.h"
#include "clingo.hh"

#include <memory>
#include <sstream>
#include <fstream>
#include <numeric>

class LevelGenerator::LevelGenImpl {
    public:
        LevelGenImpl() = default;

    private:
        std::unique_ptr<Clingo::Control> solver = nullptr;
        std::vector<Level> levels;

        unsigned width = 8;
        unsigned height = 7;
        unsigned min_rooms = 2;
        unsigned max_rooms = 16;
        unsigned seed = 0;
        bool seed_set = false;
        const char *program = nullptr;

        std::string solve() {
            solver = std::make_unique<Clingo::Control>();
            auto config = solver->configuration();
            config["solve.models"] = "1";
            config["solve.parallel_mode"] = "4";
            if (!seed_set) {
                seed = rand();
                seed_set = true;
            }
            config["solver.seed"] = std::to_string(seed).c_str();
            config["solver.rand_freq"] = "1.0";

            if (!program) {
                const char *error = nullptr;
                std::ifstream ship;
                auto success = false;
                try {
                    ship.open("programs/ship.lp");
                    std::stringstream buffer;
                    if (!(buffer << ship.rdbuf())) {
                        throw std::exception("failed to read ship.lp");
                    }
                    solver->add("base", {}, buffer.str().c_str());
                    success = true;
                } catch(const std::exception& e) {
                    std::cout << e.what();
                    error = e.what();
                }
                if (ship.is_open()) {
                    ship.close();
                }
                if(!success) {
                    throw std::exception((std::string("error creating logic program: ") + (error ? error : "")).c_str());
                }
            } else {
                solver->add("base", {}, program);
            }

            // Add inputs
            std::stringstream inputs;
            inputs
                << "#const width = " << Clingo::Number(static_cast<int>(width)) << "." << std::endl
                << "#const height = " << Clingo::Number(static_cast<int>(height)) << "." << std::endl
                << "#const min_rooms = " << Clingo::Number(static_cast<int>(min_rooms)) << "." << std::endl
                << "#const max_rooms = " << Clingo::Number(static_cast<int>(max_rooms)) << "." << std::endl;
            solver->add("base", {}, inputs.str().c_str());

            solver->ground({{"base", {}}});

            std::ostringstream out;
            for (auto &m : solver->solve()) {
                const auto costs = m.cost();
                const auto total_cost = std::accumulate(
                    costs.cbegin(), costs.cend(), (int64_t)0
                );

                const auto model_symbols = m.symbols();
                std::vector<uint64_t> transformed_symbols(model_symbols.size(), 0);
                std::transform(
                    model_symbols.cbegin(),
                    model_symbols.cend(),
                    transformed_symbols.begin(),
                    [](const auto& sym) { return sym.to_c(); }
                );
                levels.emplace_back(total_cost, transformed_symbols);
                out << "Model: ";
                for (auto &atom : m.symbols()) {
                    out << " " << atom;
                }
                out << "\n";
            }

            return out.str();
        }

        Level* best_level() {
            return &(*std::min_element(levels.begin(), levels.end(), [&](const auto& left, const auto& right) {
                return left.get_cost() < right.get_cost();
            }));
        }

        void set_width(unsigned new_width) { width = new_width; }
        void set_height(unsigned new_height) { height = new_height; }
        void set_min_rooms(unsigned new_min_rooms) { min_rooms = new_min_rooms; }
        void set_max_rooms(unsigned new_max_rooms) { max_rooms = new_max_rooms; }
        void set_seed(unsigned new_seed) { seed = new_seed; seed_set = true; }
        void set_program(const char *new_program) { program = new_program; }

        friend class LevelGenerator;
};


LevelGenerator::LevelGenerator() : impl(std::make_unique<LevelGenImpl>()){

}

LevelGenerator& LevelGenerator::operator=(LevelGenerator&& other) noexcept = default;

LevelGenerator::LevelGenerator(LevelGenerator&& other) noexcept = default;

LevelGenerator::~LevelGenerator() = default;

std::string LevelGenerator::solve()
{
    return impl->solve();
}

LevelGenerator& LevelGenerator::set_min_rooms(unsigned new_min_rooms)
{
    impl->set_min_rooms(new_min_rooms);
    return *this;
}

LevelGenerator& LevelGenerator::set_max_rooms(unsigned new_max_rooms)
{
    impl->set_max_rooms(new_max_rooms);
    return *this;
}

LevelGenerator& LevelGenerator::set_seed(unsigned new_seed) {
    impl->set_seed(new_seed);
    return *this;
}

LevelGenerator& LevelGenerator::set_width(unsigned new_width)
{
    impl->set_width(new_width);
    return *this;
}

LevelGenerator& LevelGenerator::set_height(unsigned new_height)
{
    impl->set_height(new_height);
    return *this;
}

std::string LevelGenerator::solve_safe()
{
    try {
        return impl->solve();
    } catch (const std::exception& e) {
        return std::string("NAH");
    }
}

LevelGenerator& LevelGenerator::set_program(const char* program)
{
    impl->set_program(program);
    return *this;
}

Level* LevelGenerator::best_level() const
{
    return impl->best_level();
}

unsigned LevelGenerator::get_width() const
{
    return impl->width;
}

unsigned LevelGenerator::get_height() const
{
    return impl->height;
}

unsigned LevelGenerator::get_min_rooms() const
{
    return impl->min_rooms;
}

unsigned LevelGenerator::get_max_rooms() const
{
    return impl->max_rooms;
}

unsigned LevelGenerator::get_seed() const
{
    return impl->seed;
}

const char* LevelGenerator::get_program() const
{
    return impl->program;
}
