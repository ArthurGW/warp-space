from .level import Level
from .metric import Metric


class MapLinearity(Metric):
    """Measures average amount of exits/branching from each room on the map, where dead ends do not contribute

    After:

    Thomas Smith, Julian Padget, and Andrew Vidler. 2018. Graph-based Generation of Action-Adventure Dungeon Levels
    using Answer Set Programming. In Proceedings of FDG, Malmö, Sweden, August 7-10, 2018 (FDG’18), 10 pages.
    DOI: 10.1145/3235765.3235817
    """

    @classmethod
    def title(cls) -> str:
        return "Map Linearity"

    def score(self, level: Level) -> float:
        total = 0
        count = 0
        for room in level.rooms:
            connections = level.get_connections_for_room(room)
            if not connections or len(connections) == 1:
                continue
            count += 1
            total += 1 / (len(connections) - 1)
        return total / count
