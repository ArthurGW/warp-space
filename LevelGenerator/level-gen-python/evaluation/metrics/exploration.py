from .helpers import dijkstra_path, dijkstra_distance
from .level import Level
from .metric import Metric


class Exploration(Metric):
    """Measures average amount of off-path exploration in a level

    The value returned is based on the average minimum distance of each non-optimal-path room to a room on the path,
    relative to the maximum possible average.
    """

    @classmethod
    def title(cls) -> str:
        return "Exploration"

    def score(self, level: Level) -> float:
        path = set(dijkstra_path(level, level.start_room, level.finish_room)[level.finish_room])
        off_path_dists = {room: level.num_rooms for room in level.rooms if room not in path}
        if not off_path_dists:
            # No off-path rooms, so no exploration
            return 0.0

        # Find the closest path point to each off-path room
        for path_room in path:
            for room, dist in dijkstra_distance(level, path_room).items():
                if room in off_path_dists and dist < off_path_dists[room]:
                    off_path_dists[room] = dist

        # Return average distance from path, normalised by num rooms/2, which is the max possible average
        return sum(off_path_dists.values()) / len(off_path_dists) / (level.num_rooms / 2)
