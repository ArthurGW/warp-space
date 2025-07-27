#include "level_gen.h"
#include "level_impl.h"
#include "clingo.hh"

#include <memory>
#include <sstream>
#include <fstream>

class LevelGenerator::LevelGenImpl {
    private:
        std::unique_ptr<Clingo::Control> solver = nullptr;

        unsigned width = 8;
        unsigned height = 7;
        unsigned min_rooms = 2;
        unsigned max_rooms = 16;
        unsigned seed = 0;
        bool seed_set = false;

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
            }
            if (ship.is_open()) {
                ship.close();
            }
            if(!success) {
                throw std::exception("error creating logic program");
            }

            // Add inputs
            std::stringstream inputs;
            inputs
                << "#const width = " << Clingo::Number(width) << "." << std::endl
                << "#const height = " << Clingo::Number(height) << "." << std::endl
                << "#const min_rooms = " << Clingo::Number(min_rooms) << "." << std::endl
                << "#const max_rooms = " << Clingo::Number(max_rooms) << "." << std::endl;
            solver->add("base", {}, inputs.str().c_str());

            solver->ground({{"base", {}}});

            std::ostringstream out;
            for (auto &m : solver->solve()) {
                out << "Model: ";
                for (auto &atom : m.symbols()) {
                    out << " " << atom;
                }
                out << "\n";
            }

            return out.str();
        }

        LevelGenImpl() = default;

        void set_width(unsigned new_width) { width = new_width; }
        void set_height(unsigned new_height) { height = new_height; }
        void set_min_rooms(unsigned new_min_rooms) { min_rooms = new_min_rooms; }
        void set_max_rooms(unsigned new_max_rooms) { max_rooms = new_max_rooms; }
        void set_seed(unsigned new_seed) { seed = new_seed; seed_set = true; }

        friend class LevelGenerator;
        friend std::unique_ptr<LevelGenImpl> std::make_unique<LevelGenImpl>();
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

Level LevelGenerator::best_level() const
{
    return Level(std::make_unique<Level::LevelImpl>());
}
