from .helpers import dijkstra_path, dijkstra_distance
from .level import Level
from .metric import Metric


class ProximityToDanger(Metric):
    """Measures average distance to sources of "danger" on the optimal path from start->finish

    The value returned is proportional to the distance, i.e. higher when further from danger.

    Loosely based on the metric "proximity-to-enemies" in:

    Colan Biemer and Seth Cooper. 2024. Solution Path Heuristics for Predicting Difficulty and Enjoyment Ratings of
    Roguelike Level Segments. In Proceedings of the 19th International Conference on the Foundations of Digital Games
    (FDG 2024), May 21–24, 2024, Worcester, MA, USA. ACM, New York, NY, USA, 8 pages.
    https://doi.org/10.1145/3649921.3659846
    """

    @classmethod
    def title(cls) -> str:
        return "Proximity to Danger"

    def score(self, level: Level) -> float:
        path = dijkstra_path(level, level.start_room, level.finish_room)[level.finish_room]
        danger = level.dangerous_rooms
        if not danger:
            return 1.0  # Maximally-safe

        # Find the closest danger point to each room on the path
        path_danger = {room: level.num_rooms for room in path}
        for danger_src in danger:
            for room, dist in dijkstra_distance(level, danger_src).items():
                if room in path_danger and dist < path_danger[room]:
                    path_danger[room] = dist

        # Return average danger over path, normalised by num rooms, which is the max possible distance
        return sum(path_danger.values()) / (len(path_danger) * level.num_rooms)
