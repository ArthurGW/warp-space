from .level import Level
from .metric import Metric


class Density(Metric):
    """Measures average density of level, i.e. amount of used tiles over total available spaces

    From:

    Colan Biemer and Seth Cooper. 2024. Solution Path Heuristics for Predicting Difficulty and Enjoyment Ratings of
    Roguelike Level Segments. In Proceedings of the 19th International Conference on the Foundations of Digital Games
    (FDG 2024), May 21–24, 2024, Worcester, MA, USA. ACM, New York, NY, USA, 8 pages.
    https://doi.org/10.1145/3649921.3659846
    """

    @classmethod
    def title(cls) -> str:
        return "Density"

    def score(self, level: Level) -> float:
        return sum(rm.w * rm.h for rm in level.rooms) / level.total_size
