#include "level_gen.h"
#include "clingo.hh"

#include <memory>
#include <sstream>
#include <fstream>

namespace {

} // unnamed namespace

class LevelGenerator::LevelGenImpl {
    private:
        std::unique_ptr<Clingo::Control> solver;

        uint8_t width;
        uint8_t height;

        std::string solve() {
            solver = std::make_unique<Clingo::Control>();

            std::ifstream ship;
            try {
                ship.open("programs/ship.lp");
                std::cout << "OPEN: " <<ship.is_open() << std::endl;
                std::string p{};
                if (!(ship >> p)) {
                    throw std::exception("failed to read ship.lp");
                }
                solver->add("ship", {"w", "h"}, p.c_str());
            } catch(const std::exception& e) {
                std::cout << e.what();
            }
            if (ship.is_open()) {
                ship.close();
            }

            Clingo::Part base_part{"base", {}};
            Clingo::Part ship_part{"ship", {Clingo::Number(width), Clingo::Number(height)}};

            std::ostringstream out;
            solver->ground({base_part, ship_part});
            for (auto &m : solver->solve()) {
                out << "Model:";
                for (auto &atom : m.symbols()) {
                    out << " " << atom;
                }
                out << "\n";
            }

            return out.str();
        }

    public:
        LevelGenImpl(uint8_t width, uint8_t height) : solver(nullptr), width(width), height(height) {}

        friend class LevelGenerator;
};


LevelGenerator::LevelGenerator(uint8_t width, uint8_t height) : impl(std::make_unique<LevelGenImpl>(width, height)){

}

LevelGenerator& LevelGenerator::operator=(LevelGenerator&& other) noexcept = default;

LevelGenerator::LevelGenerator(LevelGenerator&& other) noexcept = default;

LevelGenerator::~LevelGenerator() = default;

std::string LevelGenerator::solve()
{
    return impl->solve();
}
