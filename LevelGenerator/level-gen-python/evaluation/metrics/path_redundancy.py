from .helpers import dijkstra_path
from .level import Level
from .metric import Metric


class PathRedundancy(Metric):
    """Measures amount of rooms that aren't used in the optimal path from start->finish

    After:

    Thomas Smith, Julian Padget, and Andrew Vidler. 2018. Graph-based Generation of Action-Adventure Dungeon Levels
    using Answer Set Programming. In Proceedings of FDG, Malmö, Sweden, August 7-10, 2018 (FDG’18), 10 pages.
    DOI: 10.1145/3235765.3235817
    """

    @classmethod
    def title(cls) -> str:
        return "Path Redundancy"

    def score(self, level: Level) -> float:
        path = dijkstra_path(level, level.start_room, level.finish_room)[level.finish_room]
        return (level.num_rooms - len(path)) / level.num_rooms
